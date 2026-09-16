using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    public class EnemyManager : ManagerBase
    {
        private readonly IAssets<Enemy> _enemyAssets;
        private readonly WeaponManager _weaponManager;

        private readonly List<Enemy> _currentEnemies = new List<Enemy>();
        private readonly Subject<Character> _spawned = new Subject<Character>();

        public IReadOnlyList<Enemy> CurrentEnemies => _currentEnemies;
        public Observable<Character> Spawned => _spawned;

        public EnemyManager(IAssets<Enemy> enemyAssets, WeaponManager weaponManager)
        {
            _enemyAssets = enemyAssets;
            _weaponManager = weaponManager;
        }

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            DespawnAll();
            _spawned.Dispose();
        }

        public Enemy SpawnEnemy(string enemyID, Vector3 position, CharacterStats stats, AttackProfile bareHandsAttack,
            string weaponID = "")
        {
            var prefab = _enemyAssets.Get(enemyID);
            if (prefab == null)
            {
                return null;
            }

            var enemy = Object.Instantiate(prefab);
            enemy.transform.position = position;
            enemy.Init(stats, bareHandsAttack);

            if (!string.IsNullOrEmpty(weaponID))
            {
                _weaponManager.Equip(enemy, weaponID);
            }

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
