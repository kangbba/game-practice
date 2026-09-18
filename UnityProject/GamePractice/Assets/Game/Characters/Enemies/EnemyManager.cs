using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Sayne
{
    /// <summary>
    /// 적의 수명을 쥔 유일한 문. 만들고, 죽이고, 파괴하는 건 전부 여기를 거친다 —
    /// 밖에서 Destroy 하거나 직접 죽이지 않는다. 문이 하나라 목록이 곧 진실이다.
    /// 상태가 셋이라 동사도 셋이다 — 스폰(SpawnEnemy) · 죽임(KillEnemy) · 파괴(DestroyEnemy).
    /// 상태는 목록으로 구분한다: 죽음처리 전인 적은 CurrentEnemies, 죽어서 아직 화면에 남은 몸은 시체 목록.
    ///
    /// HP 가 0 이 된 적을 언제 죽음처리할지도 여기서 정한다. 평소엔 그 자리에서 죽이고,
    /// 죽음 보류(HoldDeaths)가 걸려 있으면 쓰러진 채로 세워 뒀다가 보류가 풀리는 순간 한꺼번에 죽인다.
    ///
    /// 시체도 여기서 치운다: 죽고 잠깐 누워 있다가 흐려지며 사라진다. 시체 보류(HoldCorpses)가 걸려 있으면
    /// 풀릴 때까지 누운 채로 둔다 — 궁극기처럼 무대에 올린 적을 끝까지 쥐고 있는 쪽이 이걸로 몸을 지킨다.
    /// </summary>
    public class EnemyManager : ManagerBase
    {
        /// <summary>적은 히어로를 둘러싼 고리 위에서 태어난다.</summary>
        private const float SpawnRadiusMin = 5f;
        private const float SpawnRadiusMax = 12f;

        /// <summary>죽고 나서 흐려지기 전까지 누워 있는 시간. 쓰러지는 모션(0.75초)이 끝난 뒤다.</summary>
        private const float CorpseLingerSeconds = 1f;

        private const float CorpseFadeSeconds = 0.5f;

        private readonly IAssets<Enemy> _enemyAssets;
        private readonly IAssets<EnemyPlan> _enemyPlans;
        private readonly EquipmentManager _equipmentManager;

        /// <summary>죽음처리 전인 적. 죽는 순간 여기서 빠져 시체로 넘어간다.</summary>
        private readonly List<Enemy> _currentEnemies = new List<Enemy>();

        /// <summary>보류 중에 HP 가 0 이 된 적. 보류가 풀리면 죽음처리된다. CurrentEnemies 에도 그대로 들어 있다.</summary>
        private readonly List<Enemy> _fallen = new List<Enemy>();

        /// <summary>지금 죽음 보류를 잡고 있는 수. 0 이 되어야 풀린다.</summary>
        private int _deathHolds;

        /// <summary>죽었지만 아직 화면에 누워 있는 몸. 흐려지고 나면 DestroyEnemy 로 빠진다.</summary>
        private readonly List<Enemy> _corpses = new List<Enemy>();

        /// <summary>지금 시체 보류를 잡고 있는 수. 0 이 되어야 시체가 치워지기 시작한다.</summary>
        private readonly ReactiveProperty<int> _corpseHolds = new ReactiveProperty<int>(0);

        private readonly Subject<Character> _spawned = new Subject<Character>();
        private readonly Subject<Enemy> _died = new Subject<Enemy>();

        /// <summary>
        /// 죽음처리 전인 적. 시체는 안 들어 있다. 죽음 보류 중엔 HP 0 으로 서 있는 적(IsAlive 거짓)도 들어 있다 —
        /// 그래서 웨이브는 보류가 풀릴 때까지 끝나지 않는다.
        /// </summary>
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
            DestroyAllEnemies();
            _spawned.Dispose();
            _died.Dispose();
            _corpseHolds.Dispose();
        }

        /// <summary>웨이브 구성 한 장을 그대로 받아 그만큼 내보낸다.</summary>
        public void SpawnEnemies(IReadOnlyDictionary<string, int> plan)
        {
            foreach (var (enemyID, count) in plan)
            {
                for (var i = 0; i < count; i++)
                {
                    SpawnEnemy(enemyID);
                }
            }
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

            enemy.Fell
                .Subscribe((self: this, enemy), (_, state) => state.self.OnFell(state.enemy))
                .RegisterTo(enemy.destroyCancellationToken);

            enemy.Died
                .Subscribe((self: this, enemy), (_, state) => state.self.MoveToCorpse(state.enemy))
                .RegisterTo(enemy.destroyCancellationToken);

            _spawned.OnNext(enemy);
            return enemy;
        }

        /// <summary>죽음처리 전인 적이 하나도 없다. 웨이브가 끝났는지 묻는 쪽이 이걸 본다.</summary>
        public bool IsAliveEnemyNone()
        {
            return _currentEnemies.Count == 0;
        }

        /// <summary>즉사시킨다. 체력을 깎아 죽이므로 죽음의 경로는 맞아 죽는 것과 똑같다 — 보류 중이면 쓰러져 기다린다.</summary>
        public void KillEnemy(Enemy enemy)
        {
            enemy.TakeDamage(enemy.CurrentHP.CurrentValue);
        }

        /// <summary>
        /// 죽음을 보류한다. 돌려받은 걸 Dispose 할 때까지 HP 가 0 이 된 적은 쓰러진 채 서서 계속 맞는다.
        /// 보류가 전부 풀리는 순간 쓰러진 적을 한꺼번에 죽음처리한다. 궁극기가 이걸로 적을 마네킹으로 세운다.
        /// </summary>
        public IDisposable HoldDeaths()
        {
            _deathHolds++;
            return Disposable.Create(this, self => self.ReleaseDeathHold());
        }

        /// <summary>
        /// 시체를 치우지 않고 둔다. 돌려받은 걸 Dispose 하면 그동안 쌓인 시체가 그때부터 누웠다가 사라진다.
        /// 적을 목록째 쥐고 있다가 나중에 다시 만지는 쪽(궁극기)이 그 사이 몸이 파괴되지 않게 건다.
        /// </summary>
        public IDisposable HoldCorpses()
        {
            _corpseHolds.Value++;
            return Disposable.Create(this, self => self._corpseHolds.Value--);
        }

        private void ReleaseDeathHold()
        {
            _deathHolds--;
            if (_deathHolds > 0)
            {
                return;
            }

            // 죽음처리는 목록을 옮긴다. 복사본을 돌아야 도는 중에 흔들리지 않는다.
            var fallen = _fallen.ToArray();
            _fallen.Clear();

            foreach (var enemy in fallen)
            {
                enemy.Die();
            }
        }

        /// <summary>HP 가 0 이 됐다. 보류 중이 아니면 그 자리에서 죽음처리한다.</summary>
        private void OnFell(Enemy enemy)
        {
            if (_deathHolds > 0)
            {
                _fallen.Add(enemy);
                return;
            }

            enemy.Die();
        }

        /// <summary>한 마리를 파괴한다. 죽이는 게 아니라 없던 일로 만드는 것이라 Died 는 울리지 않는다.</summary>
        public void DestroyEnemy(Enemy enemy)
        {
            _currentEnemies.Remove(enemy);
            _fallen.Remove(enemy);
            _corpses.Remove(enemy);

            Object.Destroy(enemy.gameObject);
        }

        /// <summary>살아있든 시체든 전부 파괴한다. 웨이브가 끝나면 이걸로 판을 비운다.</summary>
        public void DestroyAllEnemies()
        {
            _fallen.Clear();
            DestroyEach(_currentEnemies);
            DestroyEach(_corpses);
        }

        /// <summary>죽는 순간 목록을 옮긴다 — "살아있느냐" 는 곧 CurrentEnemies 에 있느냐다.</summary>
        private void MoveToCorpse(Enemy enemy)
        {
            _currentEnemies.Remove(enemy);
            _corpses.Add(enemy);

            _died.OnNext(enemy);

            ClearCorpseAsync(enemy).Forget();
        }

        /// <summary>
        /// 시체 보류가 풀리길 기다렸다가, 잠깐 누워 있고, 흐려진 뒤 파괴한다.
        /// 그 전에 판이 비워져 몸이 먼저 파괴되면 몸의 수명 토큰이 이 흐름도 같이 끊는다.
        /// </summary>
        private async UniTaskVoid ClearCorpseAsync(Enemy enemy)
        {
            var token = enemy.destroyCancellationToken;

            await _corpseHolds.FirstAsync(holds => holds == 0, token);
            await UniTask.Delay(TimeSpan.FromSeconds(CorpseLingerSeconds), cancellationToken: token);

            var faded = new UniTaskCompletionSource();
            enemy.FadeOut(CorpseFadeSeconds).OnComplete(() => faded.TrySetResult());
            await faded.Task.AttachExternalCancellation(token);

            DestroyEnemy(enemy);
        }

        private static void DestroyEach(List<Enemy> enemies)
        {
            foreach (var enemy in enemies)
            {
                // 게임이 끝나 씬이 내려갈 때는 유니티가 먼저 몸을 치운다. 그때는 이미 없는 것을 또 치우지 않는다.
                if (enemy != null)
                {
                    Object.Destroy(enemy.gameObject);
                }
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
