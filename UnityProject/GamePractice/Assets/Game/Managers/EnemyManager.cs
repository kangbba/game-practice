using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    public class EnemyManager : ManagerBase
    {
        private readonly IAssets<Enemy> _enemyAssets;
        private readonly IAssets<EnemyPlan> _enemyPlans;
        private readonly EquipmentManager _equipmentManager;

        private readonly List<Enemy> _currentEnemies = new List<Enemy>();
        private readonly Subject<Character> _spawned = new Subject<Character>();
        private readonly Subject<Enemy> _died = new Subject<Enemy>();

        public IReadOnlyList<Enemy> CurrentEnemies => _currentEnemies;
        public Observable<Character> Spawned => _spawned;

        /// <summary>적이 죽었다. 재화·경험치·처치 수가 이걸 본다.</summary>
        public Observable<Enemy> Died => _died;

        public EnemyManager(IAssets<Enemy> enemyAssets, IAssets<EnemyPlan> enemyPlans,
            EquipmentManager equipmentManager)
        {
            _enemyAssets = enemyAssets;
            _enemyPlans = enemyPlans;
            _equipmentManager = equipmentManager;
        }

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            DespawnAll();
            _spawned.Dispose();
            _died.Dispose();
        }

        public Enemy SpawnEnemy(string enemyID, Vector3 position)
        {
            var plan = _enemyPlans.Get(enemyID);
            var enemy = Object.Instantiate(_enemyAssets.Get(plan.PrefabID));

            // 이름을 ID 로 고정해 둔다. 같은 프리팹을 쓰는 변종끼리 하이어라키에서 구분이 되어야 한다.
            enemy.name = enemyID;
            enemy.transform.position = position;

            // 몸을 만들기 전에 자기 설계값부터 쥐여 준다 — 죽을 때 무엇을 흘릴지는 남이 아니라 자기가 안다.
            enemy.SetPlan(plan);

            enemy.Init(plan.Body, plan.Combat, _equipmentManager.CreateSet(plan.Outfit));

            _currentEnemies.Add(enemy);

            enemy.Died
                .Subscribe((self: this, enemy), (_, state) => state.self._died.OnNext(state.enemy))
                .RegisterTo(enemy.destroyCancellationToken);

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
