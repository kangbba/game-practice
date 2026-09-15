using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    public class HeroManager : ManagerBase
    {
        private static readonly CharacterStats TestHeroStats = new CharacterStats(
            maxHP: 100, moveSpeed: 4.5f, attackPower: 10, attackRange: 4f, attackInterval: 0.2f);

        private readonly HeroAssetManager _heroAssetManager;

        private readonly List<Hero> _currentHeroes = new List<Hero>();
        private readonly Subject<Character> _spawned = new Subject<Character>();

        public IReadOnlyList<Hero> CurrentHeroes => _currentHeroes;
        public Observable<Character> Spawned => _spawned;

        public HeroManager(HeroAssetManager heroAssetManager)
        {
            _heroAssetManager = heroAssetManager;
        }

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            DespawnAll();
            _spawned.Dispose();
        }

        public Hero SpawnHero(string heroID, Vector3 position)
        {
            var prefab = _heroAssetManager.GetHeroPrefab(heroID);
            if (prefab == null)
            {
                return null;
            }

            var hero = Object.Instantiate(prefab);
            hero.transform.position = position;
            hero.Init(TestHeroStats);
            _currentHeroes.Add(hero);
            _spawned.OnNext(hero);
            return hero;
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
