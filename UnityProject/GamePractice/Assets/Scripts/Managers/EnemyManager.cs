using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    public class EnemyManager : ManagerBase
    {
        private static readonly CharacterStats TestEnemyStats = new CharacterStats(
            maxHP: 50, moveSpeed: 1.5f, attackPower: 5, attackRange: 2.5f, attackInterval: 1f);

        private readonly EnemyAssetManager _enemyAssetManager;

        private readonly List<Enemy> _currentEnemies = new List<Enemy>();
        private readonly Subject<Character> _spawned = new Subject<Character>();

        public IReadOnlyList<Enemy> CurrentEnemies => _currentEnemies;
        public Observable<Character> Spawned => _spawned;

        public EnemyManager(EnemyAssetManager enemyAssetManager)
        {
            _enemyAssetManager = enemyAssetManager;
        }

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            DespawnAll();
            _spawned.Dispose();
        }

        public Enemy SpawnRandomEnemy(Vector3 position)
        {
            var enemyIDs = _enemyAssetManager.EnemyIDs;
            return SpawnEnemy(enemyIDs[Random.Range(0, enemyIDs.Count)], position);
        }

        public Enemy SpawnEnemy(string enemyID, Vector3 position)
        {
            var prefab = _enemyAssetManager.GetEnemyPrefab(enemyID);
            if (prefab == null)
            {
                return null;
            }

            var enemy = Object.Instantiate(prefab);
            enemy.transform.position = position;
            enemy.Init(TestEnemyStats);
            _currentEnemies.Add(enemy);
            _spawned.OnNext(enemy);
            return enemy;
        }

        public void DespawnAll()
        {
            foreach (var enemy in _currentEnemies)
            {
                if (enemy != null)
                {
                    Object.Destroy(enemy.gameObject);
                }
            }

            _currentEnemies.Clear();
        }
    }
}
