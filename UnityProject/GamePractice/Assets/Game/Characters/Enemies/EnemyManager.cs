using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 적의 수명을 쥔 유일한 문. 만들고, 죽이고, 파괴하는 건 전부 여기를 거친다 —
    /// 밖에서 Destroy 하거나 직접 죽이지 않는다. 문이 하나라 목록이 곧 진실이다.
    /// 상태가 셋이라 동사도 셋이다 — 스폰(SpawnEnemy) · 죽임(KillEnemy) · 파괴(DestroyEnemy).
    /// 상태는 목록으로 구분한다: 살아있는 적은 CurrentEnemies, 죽어서 아직 화면에 남은 몸은 시체 목록.
    /// </summary>
    public class EnemyManager : ManagerBase
    {
        /// <summary>적은 히어로를 둘러싼 고리 위에서 태어난다.</summary>
        private const float SpawnRadiusMin = 5f;
        private const float SpawnRadiusMax = 12f;

        private readonly IAssets<Enemy> _enemyAssets;
        private readonly IAssets<EnemyPlan> _enemyPlans;
        private readonly EquipmentManager _equipmentManager;

        /// <summary>살아있는 적만. 죽는 순간 여기서 빠져 시체로 넘어간다.</summary>
        private readonly List<Enemy> _currentEnemies = new List<Enemy>();

        /// <summary>죽었지만 아직 화면에 누워 있는 몸. 파괴하는 건 DestroyAllEnemies 가 한다.</summary>
        private readonly List<Enemy> _corpses = new List<Enemy>();

        private readonly Subject<Character> _spawned = new Subject<Character>();
        private readonly Subject<Enemy> _died = new Subject<Enemy>();
        private readonly ReactiveProperty<int> _aliveCount = new ReactiveProperty<int>();

        /// <summary>지금 살아있는 적. 시체는 안 들어 있으니 거를 것 없이 그대로 쓰면 된다.</summary>
        public IReadOnlyList<Enemy> CurrentEnemies => _currentEnemies;

        /// <summary>살아있는 적 수. 0 이 되는 순간이 웨이브가 끝난 순간이다.</summary>
        public ReadOnlyReactiveProperty<int> AliveCount => _aliveCount;

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
            DestroyAllEnemies();
            _spawned.Dispose();
            _died.Dispose();
            _aliveCount.Dispose();
        }

        /// <summary>웨이브 구성 한 장을 그대로 받아 그만큼 내보낸다. 실제로 내보낸 마릿수를 돌려준다.</summary>
        public int SpawnEnemies(IReadOnlyDictionary<string, int> plan)
        {
            var spawned = 0;

            foreach (var (enemyID, count) in plan)
            {
                for (var i = 0; i < count; i++)
                {
                    SpawnEnemy(enemyID);
                    spawned++;
                }
            }

            return spawned;
        }

        public Enemy SpawnEnemy(string enemyID)
        {
            var plan = _enemyPlans.Get(enemyID);
            var enemy = Object.Instantiate(_enemyAssets.Get(plan.PrefabID));

            // 이름을 ID 로 고정해 둔다. 같은 프리팹을 쓰는 변종끼리 하이어라키에서 구분이 되어야 한다.
            enemy.name = enemyID;
            enemy.transform.position = RandomSpawnPosition();

            // 몸을 만들기 전에 자기 설계값부터 쥐여 준다 — 죽을 때 무엇을 흘릴지는 남이 아니라 자기가 안다.
            enemy.SetPlan(plan);

            enemy.Init(plan.Body, plan.Combat, _equipmentManager.CreateSet(plan.Outfit));

            _currentEnemies.Add(enemy);
            _aliveCount.Value = _currentEnemies.Count;

            enemy.Died
                .Subscribe((self: this, enemy), (_, state) => state.self.MoveToCorpse(state.enemy))
                .RegisterTo(enemy.destroyCancellationToken);

            _spawned.OnNext(enemy);
            return enemy;
        }

        /// <summary>즉사시킨다. 체력을 깎아 죽이므로 죽음의 경로는 맞아 죽는 것과 똑같다.</summary>
        public void KillEnemy(Enemy enemy)
        {
            enemy.TakeDamage(enemy.CurrentHP.CurrentValue);
        }

        /// <summary>한 마리를 파괴한다. 죽이는 게 아니라 없던 일로 만드는 것이라 Died 는 울리지 않는다.</summary>
        public void DestroyEnemy(Enemy enemy)
        {
            _currentEnemies.Remove(enemy);
            _corpses.Remove(enemy);
            _aliveCount.Value = _currentEnemies.Count;

            Object.Destroy(enemy.gameObject);
        }

        /// <summary>살아있든 시체든 전부 파괴한다. 웨이브가 끝나면 이걸로 판을 비운다.</summary>
        public void DestroyAllEnemies()
        {
            DestroyEach(_currentEnemies);
            DestroyEach(_corpses);

            _aliveCount.Value = 0;
        }

        /// <summary>죽는 순간 목록을 옮긴다 — "살아있느냐" 는 곧 CurrentEnemies 에 있느냐다.</summary>
        private void MoveToCorpse(Enemy enemy)
        {
            _currentEnemies.Remove(enemy);
            _corpses.Add(enemy);
            _aliveCount.Value = _currentEnemies.Count;

            _died.OnNext(enemy);
        }

        private static void DestroyEach(List<Enemy> enemies)
        {
            foreach (var enemy in enemies)
            {
                Object.Destroy(enemy.gameObject);
            }

            enemies.Clear();
        }

        private static Vector3 RandomSpawnPosition()
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var distance = Random.Range(SpawnRadiusMin, SpawnRadiusMax);
            return new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        }
    }
}
