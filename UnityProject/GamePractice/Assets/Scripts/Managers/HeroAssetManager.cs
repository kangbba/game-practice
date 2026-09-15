using System.Collections.Generic;
using UnityEngine;

namespace Sayne
{
    public class HeroAssetManager : ManagerBase
    {
        private const string HeroPrefabRoot = "Heroes";

        private readonly Dictionary<string, Hero> _heroPrefabs = new Dictionary<string, Hero>();

        protected override void OnInit()
        {
            _heroPrefabs.Clear();
            foreach (var go in Resources.LoadAll<GameObject>(HeroPrefabRoot))
            {
                if (!go.TryGetComponent<Hero>(out var hero))
                {
                    Debug.LogError($"HeroAssetManager: {go.name} 에 Hero 컴포넌트가 없다");
                    continue;
                }

                _heroPrefabs[go.name] = hero;
            }
        }

        protected override void OnRelease()
        {
            _heroPrefabs.Clear();
        }

        public Hero GetHeroPrefab(string heroID)
        {
            if (!_heroPrefabs.TryGetValue(heroID, out var prefab))
            {
                Debug.LogError($"HeroAssetManager: {heroID} 프리팹이 등록되지 않았다");
                return null;
            }

            return prefab;
        }
    }
}
