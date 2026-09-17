using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Sayne
{
    public class HeroManager : ManagerBase
    {
        private const float ReviveDuration = 5f;

        private readonly IAssets<Hero> _heroAssets;
        private readonly IAssets<HeroPlan> _heroPlans;
        private readonly EquipmentManager _equipmentManager;
        private readonly GrowthManager _growthManager;

        private readonly List<Hero> _currentHeroes = new List<Hero>();
        private readonly Subject<Character> _spawned = new Subject<Character>();
        private readonly ReactiveProperty<float> _reviveRemainTime = new ReactiveProperty<float>(0f);

        public IReadOnlyList<Hero> CurrentHeroes => _currentHeroes;
        public Observable<Character> Spawned => _spawned;

        /// <summary>부활까지 남은 시간. 살아있는 동안은 0.</summary>
        public ReadOnlyReactiveProperty<float> ReviveRemainTime => _reviveRemainTime;

        public HeroManager(IAssets<Hero> heroAssets, IAssets<HeroPlan> heroPlans,
            EquipmentManager equipmentManager, GrowthManager growthManager)
        {
            _heroAssets = heroAssets;
            _heroPlans = heroPlans;
            _equipmentManager = equipmentManager;
            _growthManager = growthManager;
        }

        protected override void OnInit()
        {
            // 스폰 이후에 성장을 사면 살아있는 히어로에게 그 자리에서 다시 얹는다.
            // 새로 스폰(부활 포함)되는 히어로는 SpawnHero 가 처음부터 성장분을 넣어 만든다.
            _growthManager.Bonus
                .Subscribe(this, (bonus, self) => self.ApplyGrowth(bonus))
                .RegisterTo(LifeToken);
        }

        /// <summary>성장 몫만 갈아끼운다. 최종 스탯은 캐릭터 안에서 기본 + 성장으로 합성된다.</summary>
        private void ApplyGrowth(CharacterStats bonus)
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
            _spawned.Dispose();
            _reviveRemainTime.Dispose();
        }

        public Hero SpawnHero(string heroID, Vector3 position)
        {
            var plan = _heroPlans.Get(heroID);
            var hero = Object.Instantiate(_heroAssets.Get(heroID));
            hero.transform.position = position;
            // 몸은 기본 플랜 그대로 태어나고, 성장 몫은 그 위에 얹는다. 부활도 같은 길이라 성장분 풀피로 살아난다.
            hero.Init(plan.Body, plan.Combat, _equipmentManager.CreateSet(plan.Outfit));
            hero.SetGrowthBonus(_growthManager.Bonus.CurrentValue);

            FillStartingBag(hero, plan.Outfit);

            _currentHeroes.Add(hero);

            hero.Died
                .Subscribe((self: this, heroID, hero), (_, state) => state.self.ReviveAsync(state.heroID, state.hero).Forget())
                .RegisterTo(LifeToken);

            _spawned.OnNext(hero);
            return hero;
        }

        /// <summary>
        /// 태어날 때 입고 있는 한 벌은 가방에도 들어 있다 — 입은 걸 장비창에서 다시 못 고르면 안 된다.
        /// 맨손은 무기의 바닥 상태라 늘 가지고 있다.
        /// </summary>
        private static void FillStartingBag(Hero hero, EquipmentIDs outfit)
        {
            hero.Inventory.Add(EquipmentID.Weapon.BareHands);

            foreach (var id in outfit.All())
            {
                if (!hero.Inventory.Contains(id))
                {
                    hero.Inventory.Add(id);
                }
            }
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

        public void Despawn(Hero hero)
        {
            if (hero == null || !_currentHeroes.Remove(hero))
            {
                return;
            }

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
