using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sayne
{
    /// <summary>
    /// 월드에 떨어진 구슬의 주인. 뿌리고, 가까이 온 히어로에게 끌어다 붙이고, 닿으면 약속된 일을 실행한 뒤 치운다.
    /// 끌어당기는 힘은 히어로 쪽의 것이다 — 구슬은 자기가 어디로 가는지 모르고, 여기가 옮긴다.
    /// 구슬이 무엇인지도 모른다: 그림도, 먹었을 때 할 일도 스폰을 부탁하는 쪽이 같이 넣어 준다.
    /// </summary>
    public class DropManager : ManagerBase
    {
        /// <summary>히어로가 이 거리 안의 구슬을 끌어당긴다.</summary>
        private const float MagnetRadius = 2.5f;

        /// <summary>같은 자리에 겹쳐 떨어지지 않게 흩는 반경.</summary>
        private const float ScatterRadius = 0.8f;

        /// <summary>끌려오는 속도. 가까울수록 빨라져 착 달라붙는 느낌이 난다.</summary>
        private const float PullSpeed = 9f;
        private const float PullAcceleration = 24f;
        private const float PullReachDistance = 0.35f;

        private readonly HeroManager _heroManager;
        private readonly ItemAssetManager _itemAssets;

        /// <summary>월드에 남아 있는 구슬과 닿았을 때 할 일, 그리고 지금 그걸 끌고 있는 히어로와 그 속도.</summary>
        private readonly Dictionary<DropItem, (Action<Hero> OnCollected, Hero Puller, float Speed)> _drops =
            new Dictionary<DropItem, (Action<Hero>, Hero, float)>();

        /// <summary>도는 동안 표를 고치므로 키를 따로 떠 둔다.</summary>
        private readonly List<DropItem> _cursor = new List<DropItem>();

        public DropManager(HeroManager heroManager, ItemAssetManager itemAssets)
        {
            _heroManager = heroManager;
            _itemAssets = itemAssets;
        }

        protected override void OnInit()
        {
            Observable.EveryUpdate(UnityFrameProvider.Update)
                .Subscribe(this, (_, self) => self.UpdatePull())
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
        }

        /// <summary>
        /// 구슬 하나를 떨어뜨린다. 히어로가 주우면 onCollected 가 그 히어로를 받아 돈다 —
        /// 무엇이 들어오는지는 부탁한 쪽만 안다.
        /// </summary>
        /// <param name="label">하이어라키에 보일 이름. 뭐가 떨어졌는지 눈으로 구분하려고 붙인다.</param>
        public void Spawn(Vector3 position, Sprite icon, string label, Action<Hero> onCollected)
        {
            var drop = Object.Instantiate(_itemAssets.DropOrbPrefab);
            drop.name = label;
            drop.transform.position = position;
            drop.SetPortrait(icon);

            var scatter = UnityEngine.Random.insideUnitCircle * ScatterRadius;
            drop.Drop(position + new Vector3(scatter.x, 0f, scatter.y));

            _drops[drop] = (onCollected, null, 0f);
        }

        private void UpdatePull()
        {
            _cursor.Clear();
            _cursor.AddRange(_drops.Keys);

            foreach (var drop in _cursor)
            {
                if (drop == null)
                {
                    _drops.Remove(drop);
                    continue;
                }

                var entry = _drops[drop];

                if (entry.Puller == null || !entry.Puller.IsAlive)
                {
                    TryGrab(drop, entry);
                    continue;
                }

                Pull(drop, entry);
            }
        }

        /// <summary>
        /// 아직 아무도 끌지 않는 구슬을, 반경 안에 든 히어로가 집어 끈다.
        /// 끌던 히어로가 죽어 놓친 구슬도 이 길로 새 주인을 찾는다 — 그동안은 다시 둥실거린다.
        /// </summary>
        private void TryGrab(DropItem drop, (Action<Hero> OnCollected, Hero Puller, float Speed) entry)
        {
            if (!drop.IsSettled)
            {
                return;
            }

            // 끌던 주인을 방금 잃었으면 손을 놓은 티를 낸다.
            if (entry.Puller != null)
            {
                drop.StartBobbing();
                _drops[drop] = (entry.OnCollected, null, 0f);
                return;
            }

            var hero = FindHeroInRange(drop.transform.position);
            if (hero == null)
            {
                return;
            }

            drop.StopBobbing();
            _drops[drop] = (entry.OnCollected, hero, PullSpeed);
        }

        /// <summary>끄는 쪽이 구슬을 자기에게 옮긴다. 닿으면 약속된 일을 하고 치운다.</summary>
        private void Pull(DropItem drop, (Action<Hero> OnCollected, Hero Puller, float Speed) entry)
        {
            var speed = entry.Speed + PullAcceleration * Time.deltaTime;
            _drops[drop] = (entry.OnCollected, entry.Puller, speed);

            var target = entry.Puller.transform.position;
            drop.transform.position = Vector3.MoveTowards(drop.transform.position, target, speed * Time.deltaTime);

            if (Vector3.Distance(drop.transform.position, target) > PullReachDistance)
            {
                return;
            }

            _drops.Remove(drop);
            entry.OnCollected(entry.Puller);
            Object.Destroy(drop.gameObject);
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
    }
}
