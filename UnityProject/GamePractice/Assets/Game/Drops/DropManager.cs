using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 전리품의 주인. 적이 죽으면 그 적의 설계값을 굴려 월드에 구슬을 남기고,
    /// 히어로가 가까이 오면 그쪽으로 빨려가게 해 가방이나 지갑에 넣는다.
    /// 구슬은 자기가 무엇인지 모른다 — 그 대응은 여기서만 들고 있다.
    /// </summary>
    public class DropManager : ManagerBase
    {
        /// <summary>이 거리 안에 들어오면 빨려 들어가기 시작한다.</summary>
        private const float MagnetRadius = 2.5f;

        /// <summary>같은 자리에 겹쳐 떨어지지 않게 흩는 반경.</summary>
        private const float ScatterRadius = 0.8f;

        private readonly EnemyManager _enemyManager;
        private readonly HeroManager _heroManager;
        private readonly CurrencyManager _currencyManager;
        private readonly EquipmentManager _equipmentManager;
        private readonly IAssets<Sprite> _dropPortraits;
        private readonly DropItem _dropItemPrefab;

        /// <summary>월드에 남아 있는 구슬과 그 안에 든 전리품, 그리고 지금 그걸 빨아들이는 히어로.</summary>
        private readonly Dictionary<DropItem, (DropReward Reward, Hero Taker)> _drops =
            new Dictionary<DropItem, (DropReward, Hero)>();

        private readonly List<DropItem> _magnetTargets = new List<DropItem>();

        private readonly Subject<DropReward> _collected = new Subject<DropReward>();

        /// <summary>구슬 하나를 실제로 주웠다. 무엇이 들어왔는지 기록하는 쪽(정산 화면)이 이걸 센다.</summary>
        public Observable<DropReward> Collected => _collected;

        public DropManager(EnemyManager enemyManager, HeroManager heroManager, CurrencyManager currencyManager,
            EquipmentManager equipmentManager, IAssets<Sprite> dropPortraits, DropItem dropItemPrefab)
        {
            _enemyManager = enemyManager;
            _heroManager = heroManager;
            _currencyManager = currencyManager;
            _equipmentManager = equipmentManager;
            _dropPortraits = dropPortraits;
            _dropItemPrefab = dropItemPrefab;
        }

        protected override void OnInit()
        {
            _enemyManager.Died
                .Subscribe(this, (enemy, self) => self.DropFrom(enemy))
                .RegisterTo(LifeToken);

            Observable.EveryUpdate(UnityFrameProvider.Update)
                .Subscribe(this, (_, self) => self.UpdateMagnet())
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            foreach (var drop in _drops.Keys)
            {
                if (drop != null)
                {
                    Object.Destroy(drop.gameObject);
                }
            }

            _drops.Clear();
            _collected.Dispose();
        }

        private void DropFrom(Enemy enemy)
        {
            foreach (var reward in enemy.Plan.Roll())
            {
                Spawn(reward, enemy.transform.position);
            }
        }

        private void Spawn(DropReward reward, Vector3 position)
        {
            var drop = Object.Instantiate(_dropItemPrefab);
            drop.name = reward.Type == DropType.Gold ? $"Gold {reward.GoldAmount}" : reward.EquipmentID;
            drop.transform.position = position;
            drop.SetPortrait(PortraitFor(reward));

            var scatter = Random.insideUnitCircle * ScatterRadius;
            drop.Drop(position + new Vector3(scatter.x, 0f, scatter.y));

            _drops[drop] = (reward, null);

            drop.Absorbed
                .Subscribe(this, (absorbed, self) => self.Collect(absorbed))
                .RegisterTo(drop.destroyCancellationToken);
        }

        private void UpdateMagnet()
        {
            // 흡수를 걸면 표에 주인을 적어야 해서 키 목록을 따로 뜬 뒤에 고친다.
            _magnetTargets.Clear();
            _magnetTargets.AddRange(_drops.Keys);

            foreach (var drop in _magnetTargets)
            {
                if (drop == null || !drop.CanBeAbsorbed)
                {
                    continue;
                }

                var hero = FindHeroInRange(drop.transform.position);
                if (hero != null)
                {
                    drop.Absorb(hero.transform);
                    _drops[drop] = (_drops[drop].Reward, hero);
                }
            }
        }

        private Hero FindHeroInRange(Vector3 position)
        {
            foreach (var hero in _heroManager.CurrentHeroes)
            {
                if (hero != null && hero.IsAlive
                    && (hero.transform.position - position).sqrMagnitude <= MagnetRadius * MagnetRadius)
                {
                    return hero;
                }
            }

            return null;
        }

        /// <summary>구슬 속 그림. 장비는 실제로 걸치는 그림을, 골드는 동전 그림을 쓴다.</summary>
        private Sprite PortraitFor(DropReward reward)
        {
            if (reward.Type == DropType.Gold)
            {
                return _dropPortraits.Get(AssetAddresses.GoldPortrait);
            }

            return _equipmentManager.GetIcon(reward.EquipmentID);
        }

        /// <summary>구슬이 닿았다. 받는 건 흡수를 건 그 히어로다 — 도중에 죽었으면 아무도 못 받는다.</summary>
        private void Collect(DropItem drop)
        {
            if (!_drops.TryGetValue(drop, out var entry))
            {
                return;
            }

            _drops.Remove(drop);

            if (entry.Taker == null)
            {
                return;
            }

            if (entry.Reward.Type == DropType.Gold)
            {
                _currencyManager.AddGold(entry.Reward.GoldAmount);
            }
            else
            {
                _heroManager.Inventory.Add(entry.Reward.EquipmentID);
            }

            _collected.OnNext(entry.Reward);
        }
    }
}
