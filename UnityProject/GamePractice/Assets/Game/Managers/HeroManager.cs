using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Sayne
{
    public class HeroManager : ManagerBase
    {
        private readonly IHeroAssets _heroAssets;
        private readonly WeaponManager _weaponManager;

        private const float ReviveDuration = 5f;

        private readonly List<Hero> _currentHeroes = new List<Hero>();
        private readonly Subject<Character> _spawned = new Subject<Character>();
        private readonly ReactiveProperty<float> _reviveRemainTime = new ReactiveProperty<float>(0f);

        public IReadOnlyList<Hero> CurrentHeroes => _currentHeroes;
        public Observable<Character> Spawned => _spawned;

        /// <summary>부활까지 남은 시간. 살아있는 동안은 0.</summary>
        public ReadOnlyReactiveProperty<float> ReviveRemainTime => _reviveRemainTime;

        public HeroManager(IHeroAssets heroAssets, WeaponManager weaponManager)
        {
            _heroAssets = heroAssets;
            _weaponManager = weaponManager;
        }

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            DespawnAll();
            _spawned.Dispose();
            _reviveRemainTime.Dispose();
        }

        public Hero SpawnHero(string heroID, Vector3 position)
        {
            var prefab = _heroAssets.Get(heroID);
            var definition = _heroAssets.GetDefinition(heroID);
            if (prefab == null || definition == null)
            {
                return null;
            }

            var hero = Object.Instantiate(prefab);
            hero.transform.position = position;
            hero.Init(definition.ToStats(), definition.ToBareHandsAttack());

            if (!string.IsNullOrEmpty(definition.DefaultWeaponID))
            {
                _weaponManager.Equip(hero, definition.DefaultWeaponID);
            }

            _currentHeroes.Add(hero);

            hero.Died
                .Subscribe((self: this, heroID, hero), (_, state) => state.self.ReviveAsync(state.heroID, state.hero).Forget())
                .RegisterTo(LifeToken);

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
