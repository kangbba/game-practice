using System.Collections.Generic;
using UnityEngine;

namespace Sayne
{
    public class HeroManager : ManagerBase
    {
        private readonly HeroAssetManager _heroAssetManager;

        private readonly List<Hero> _currentHeroes = new List<Hero>();

        public IReadOnlyList<Hero> CurrentHeroes => _currentHeroes;

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
            _currentHeroes.Add(hero);
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
