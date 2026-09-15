using System.Collections.Generic;
using UnityEngine;

namespace Sayne
{
    public class EnemyAssetManager : ManagerBase
    {
        private const string EnemyPrefabRoot = "Enemies";

        private readonly Dictionary<string, Enemy> _enemyPrefabs = new Dictionary<string, Enemy>();
        private readonly List<string> _enemyIDs = new List<string>();

        public IReadOnlyList<string> EnemyIDs => _enemyIDs;

        protected override void OnInit()
        {
            _enemyPrefabs.Clear();
            _enemyIDs.Clear();
            foreach (var go in Resources.LoadAll<GameObject>(EnemyPrefabRoot))
            {
                if (!go.TryGetComponent<Enemy>(out var enemy))
                {
                    Debug.LogError($"EnemyAssetManager: {go.name} 에 Enemy 컴포넌트가 없다");
                    continue;
                }

                _enemyPrefabs[go.name] = enemy;
                _enemyIDs.Add(go.name);
            }
        }

        protected override void OnRelease()
        {
            _enemyPrefabs.Clear();
            _enemyIDs.Clear();
        }

        public Enemy GetEnemyPrefab(string enemyID)
        {
            if (!_enemyPrefabs.TryGetValue(enemyID, out var prefab))
            {
                Debug.LogError($"EnemyAssetManager: {enemyID} 프리팹이 등록되지 않았다");
                return null;
            }

            return prefab;
        }
    }
}
