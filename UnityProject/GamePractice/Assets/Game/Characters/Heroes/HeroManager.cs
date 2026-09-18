using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sayne
{
    public class HeroManager : ManagerBase
    {
        private const float ReviveDuration = 5f;

        /// <summary>마지막으로 맞은 뒤 이만큼 지나야 회복이 시작된다.</summary>
        private const float RegenDelaySeconds = 5f;

        /// <summary>회복이 시작되면 1초마다 최대 체력의 이 비율만큼 찬다.</summary>
        private const float RegenRatioPerSecond = 0.05f;

        private readonly IAssets<Hero> _heroAssets;
        private readonly IAssets<HeroData> _heroData;
        private readonly EquipmentManager _equipmentManager;
        private readonly GrowthManager _growthManager;

        private readonly List<Hero> _currentHeroes = new List<Hero>();
        private readonly Subject<Character> _spawned = new Subject<Character>();
        private readonly Subject<Character> _despawned = new Subject<Character>();
        private readonly ReactiveProperty<Hero> _currentHero = new ReactiveProperty<Hero>();
        private readonly ReactiveProperty<float> _reviveRemainTime = new ReactiveProperty<float>(0f);

        /// <summary>물러난 영웅이 마지막에 입고 있던 장비 ID. 다시 나올 때(부활·교체) 이걸 그대로 입고 나온다.</summary>
        private readonly Dictionary<string, List<string>> _wornEquipment = new Dictionary<string, List<string>>();

        /// <summary>영웅들이 함께 쓰는 가방. 주운 장비는 누가 주웠든 여기 쌓인다.</summary>
        public PartyInventory Inventory { get; } = new PartyInventory();

        public IReadOnlyList<Hero> CurrentHeroes => _currentHeroes;
        public Observable<Character> Spawned => _spawned;

        /// <summary>
        /// 몸이 판에서 치워지기 직전. 파괴는 프레임 끝으로 미뤄지므로, 몸에 딸린 것(HP 바 등)은 이걸 듣고 같은 자리에서 같이 치워야
        /// 몸 없이 한 프레임 더 도는 일이 없다. 교체처럼 죽지 않고 물러날 때는 Died 가 울리지 않는다.
        /// </summary>
        public Observable<Character> Despawned => _despawned;

        /// <summary>
        /// 지금 싸우는 영웅(편성 첫 칸). 부활·교체로 새로 서면 그 영웅으로 바뀐다. 쓰러져 부활을 기다리는 동안은 쓰러진 몸 그대로다.
        /// 첫 스폰 전에만 null 이다. "지금 영웅" 을 보여주는 UI 는 전부 이걸 구독한다.
        /// </summary>
        public ReadOnlyReactiveProperty<Hero> CurrentHero => _currentHero;

        /// <summary>부활까지 남은 시간. 살아있는 동안은 0.</summary>
        public ReadOnlyReactiveProperty<float> ReviveRemainTime => _reviveRemainTime;

        public HeroManager(IAssets<Hero> heroAssets, IAssets<HeroData> heroData,
            EquipmentManager equipmentManager, GrowthManager growthManager)
        {
            _heroAssets = heroAssets;
            _heroData = heroData;
            _equipmentManager = equipmentManager;
            _growthManager = growthManager;
        }

        protected override void OnInit()
        {
            // 지금은 있는 장비를 전부 들고 시작한다 — 세 영웅의 장비도, 활도 장비창에서 바로 바꿔 장착해 볼 수 있다.
            // 맨손만 뺀다. 맨손은 아이템이 아니라 "무기 없음" 이다.
            foreach (var equipmentID in _equipmentManager.IDs)
            {
                if (equipmentID != EquipmentID.Weapon.BareHands)
                {
                    Inventory.Add(equipmentID);
                }
            }

            // 스폰 이후에 성장을 사면 살아있는 히어로에게 그 자리에서 다시 얹는다.
            // 새로 스폰(부활 포함)되는 히어로는 SpawnHero 가 처음부터 성장분을 넣어 만든다.
            _growthManager.Bonus
                .Subscribe(this, (bonus, self) => self.ApplyGrowth(bonus))
                .RegisterTo(LifeToken);
        }

        /// <summary>성장 몫만 갈아끼운다. 최종 스탯은 캐릭터 안에서 기본 + 성장으로 합성된다.</summary>
        private void ApplyGrowth(StatGroup bonus)
        {
            foreach (var hero in _currentHeroes)
            {
                if (hero != null && hero.IsAlive)
                {
                    hero.SetGrowthBonus(bonus);
                }
            }
        }

        protected override void OnRelease()
        {
            DespawnAll();
            Inventory.Dispose();
            _spawned.Dispose();
            _despawned.Dispose();
            _currentHero.Dispose();
            _reviveRemainTime.Dispose();
        }

        public Hero SpawnHero(string heroID, Vector3 position)
        {
            var data = _heroData.Get(heroID);
            var hero = Object.Instantiate(_heroAssets.Get(heroID));
            hero.transform.position = position;

            // 처음 나오는 영웅은 설계값의 시작 장비 세트를, 한 번 나왔던 영웅은 물러날 때 입던 장비 세트를 입고 나온다.
            var equipment = _wornEquipment.TryGetValue(heroID, out var worn)
                ? _equipmentManager.CreateSet(worn)
                : _equipmentManager.CreateSet(data.EquipmentSet);

            // 몸은 기본 플랜 그대로 태어나고, 성장 몫은 그 위에 얹는다. 부활도 같은 길이라 성장분 풀피로 살아난다.
            hero.Init(data, equipment);
            hero.SetGrowthBonus(_growthManager.Bonus.CurrentValue);

            _currentHeroes.Add(hero);

            // 히어로는 죽음을 미루지 않는다. Dying 이 되는 그 자리에서 죽음처리한다.
            hero.StartedDying
                .Subscribe(hero, (_, owner) => owner.Die())
                .RegisterTo(hero.destroyCancellationToken);

            // 맞지 않고 한동안 지나면 조금씩 찬다. 맞으면 기다림을 처음부터 다시 센다.
            hero.Damaged
                .Select(_ => Unit.Default)
                .Prepend(Unit.Default)
                .Select(_ => Observable.Timer(TimeSpan.FromSeconds(RegenDelaySeconds), TimeSpan.FromSeconds(1f)))
                .Switch()
                .Subscribe(hero, (_, owner) => owner.Heal(Mathf.CeilToInt(owner.FinalMaxHP * RegenRatioPerSecond)))
                .RegisterTo(hero.destroyCancellationToken);

            hero.Died
                .Subscribe((self: this, heroID, hero), (_, state) => state.self.ReviveAsync(state.heroID, state.hero).Forget())
                .RegisterTo(hero.destroyCancellationToken);

            _currentHero.Value = hero;
            _spawned.OnNext(hero);
            return hero;
        }

        /// <summary>부활 = 시체 디스폰 후 재스폰. HP바·이펙트는 Spawned/Died 흐름이 알아서 재생성한다.</summary>
        private async UniTaskVoid ReviveAsync(string heroID, Hero hero)
        {
            _reviveRemainTime.Value = ReviveDuration;

            while (_reviveRemainTime.Value > 0f)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, LifeToken);

                if (hero == null)
                {
                    _reviveRemainTime.Value = 0f;
                    return;
                }

                _reviveRemainTime.Value = Mathf.Max(_reviveRemainTime.Value - Time.deltaTime, 0f);
            }

            Despawn(hero);
            SpawnHero(heroID, Vector3.zero);
        }

        /// <summary>
        /// 편성 첫 칸의 영웅을 바꾼다. 싸우던 영웅은 그 자리에서 물러나고 새 영웅이 같은 자리에 선다 —
        /// 웨이브·적·성장·가방은 그대로 이어지고, HUD·카메라·창은 Spawned 를 듣고 스스로 새 영웅으로 갈아 문다.
        /// 새 영웅은 부활처럼 풀피로 선다. 쓰러져 부활을 기다리는 동안은 부르지 않는다 — 편성창이 그동안 막는다.
        /// </summary>
        public void ChangeLeader(string heroID)
        {
            var leader = FindFirstAliveHero();

            if (leader.ID == heroID)
            {
                return;
            }

            var position = leader.transform.position;

            Despawn(leader);
            SpawnHero(heroID, position);
        }

        /// <summary>살아 있는 첫 영웅. 다 죽어 있으면 null.</summary>
        public Hero FindFirstAliveHero()
        {
            foreach (var hero in _currentHeroes)
            {
                if (hero != null && hero.IsAlive)
                {
                    return hero;
                }
            }

            return null;
        }

        /// <summary>그 자리에서 반경 안에 든 살아 있는 영웅 하나. 없으면 null.</summary>
        public Hero FindAliveHeroInRadius(Vector3 center, float radius)
        {
            foreach (var hero in _currentHeroes)
            {
                if (hero != null && hero.IsAlive && (hero.transform.position - center).sqrMagnitude <= radius * radius)
                {
                    return hero;
                }
            }

            return null;
        }

        /// <summary>그 자리에서 가장 가까운 살아 있는 영웅. 없으면 null.</summary>
        public Hero FindNearestAliveHero(Vector3 from)
        {
            Hero nearest = null;
            var nearestDistance = float.MaxValue;

            foreach (var hero in _currentHeroes)
            {
                if (!hero.IsAlive)
                {
                    continue;
                }

                var distance = (hero.transform.position - from).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = hero;
                }
            }

            return nearest;
        }

        public void Despawn(Hero hero)
        {
            if (hero == null || !_currentHeroes.Remove(hero))
            {
                return;
            }

            _wornEquipment[hero.ID] = hero.Equipment.WornIDs();

            _despawned.OnNext(hero);
            Object.Destroy(hero.gameObject);
        }

        public void DespawnAll()
        {
            foreach (var hero in _currentHeroes)
            {
                if (hero != null)
                {
                    Object.Destroy(hero.gameObject);
                }
            }

            _currentHeroes.Clear();
        }
    }
}
