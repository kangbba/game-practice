using System.Collections.Generic;
using UnityEngine;

namespace Sayne
{
    public class ParticleAssetManager : ManagerBase
    {
        private const string ParticlePrefabRoot = "Particles";

        private readonly Dictionary<string, GameObject> _particlePrefabs = new Dictionary<string, GameObject>();

        protected override void OnInit()
        {
            _particlePrefabs.Clear();
            foreach (var go in Resources.LoadAll<GameObject>(ParticlePrefabRoot))
            {
                _particlePrefabs[go.name] = go;
            }
        }

        protected override void OnRelease()
        {
            _particlePrefabs.Clear();
        }

        public GameObject GetParticlePrefab(string particleID)
        {
            if (!_particlePrefabs.TryGetValue(particleID, out var prefab))
            {
                Debug.LogError($"ParticleAssetManager: {particleID} 프리팹이 등록되지 않았다");
                return null;
            }

            return prefab;
        }
    }
}
