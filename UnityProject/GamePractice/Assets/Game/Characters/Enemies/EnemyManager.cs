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
    /// 목록은 둘이다: 죽음처리 전인 적은 CurrentEnemies, 죽어서 아직 화면에 남은 몸은 시체 목록.
    ///
    /// Dying(HP 0, 죽음처리 전)이 된 적을 언제 죽음처리할지도 여기서 정한다. 평소엔 그 자리에서 죽이고,
    /// 죽음 보류(HoldDeaths)가 걸려 있으면 Dying 으로 세워 뒀다가 보류가 풀리는 순간 한꺼번에 죽인다.
    ///
    /// 시체도 여기서 치운다: 죽고 잠깐 누워 있다가 흐려지며 사라진다. 시체 보류(HoldCorpses)가 걸려 있으면
    /// 풀릴 때까지 누운 채로 둔다 — 궁극기처럼 무대에 올린 적을 끝까지 쥐고 있는 쪽이 이걸로 몸을 지킨다.
    /// </summary>
    public class EnemyManager : ManagerBase
    {
        /// <summary>적은 히어로를 둘러싼 고리 위에서 태어난다.</summary>
        private const float SpawnRadiusMin = 5f;
        private const float SpawnRadiusMax = 12f;

        /// <summary>죽고 나서 흐려지기 전까지 누워 있는 시간. 죽는 모션(0.75초)이 끝난 뒤다.</summary>
        private const float CorpseLingerSeconds = 1f;

        private const float CorpseFadeSeconds = 0.5f;

        /// <summary>히어로와 위아래(Z)로 이만큼 안이면 같은 줄로 보고 몸이 부딪친다.</summary>
        private const float BodyDepth = 0.5f;

        private readonly IAssets<Enemy> _enemyAssets;
        private readonly IAssets<EnemyData> _enemyData;
        private readonly EquipmentManager _equipmentManager;

        /// <summary>죽음처리 전인 적. 죽는 순간 여기서 빠져 시체로 넘어간다.</summary>
        private readonly List<Enemy> _currentEnemies = new List<Enemy>();

        /// <summary>지금 죽음 보류를 잡고 있는 수. 0 이 되어야 풀린다.</summary>
        private int _deathHolds;

        /// <summary>죽었지만 아직 화면에 누워 있는 몸. 흐려지고 나면 DestroyEnemy 로 빠진다.</summary>
        private readonly List<Enemy> _corpses = new List<Enemy>();

        /// <summary>지금 시체 보류를 잡고 있는 수. 0 이 되어야 시체가 치워지기 시작한다.</summary>
        private readonly ReactiveProperty<int> _corpseHolds = new ReactiveProperty<int>(0);

        private readonly Subject<Character> _spawned = new Subject<Character>();
        private readonly Subject<Enemy> _died = new Subject<Enemy>();

        /// <summary>
        /// 죽음처리 전인 적. 시체는 안 들어 있다. 죽음 보류 중엔 Dying 인 적도 들어 있다 —
        /// 그래서 웨이브는 보류가 풀릴 때까지 끝나지 않는다.
        /// </summary>
        public IReadOnlyList<Enemy> CurrentEnemies => _currentEnemies;

        public Observable<Character> Spawned => _spawned;

        /// <summary>적이 죽었다. 재화·경험치·처치 수가 이걸 본다.</summary>
        public Observable<Enemy> Died => _died;

        public EnemyManager(IAssets<Enemy> enemyAssets, IAssets<EnemyData> enemyData,
            EquipmentManager equipmentManager)
        {
            _enemyAssets = enemyAssets;
            _enemyData = enemyData;
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
            var data = _enemyData.Get(enemyID);
            var enemy = Object.Instantiate(_enemyAssets.Get(data.PrefabID));

            // 이름을 ID 로 고정해 둔다. 같은 프리팹을 쓰는 변종끼리 하이어라키에서 구분이 되어야 한다.
            enemy.name = enemyID;
            enemy.transform.position = RandomSpawnPosition();

            // 설계값은 몸이 쥔다 — 죽을 때 무엇을 흘릴지는 남이 아니라 자기가 안다.
            enemy.Init(data, _equipmentManager.CreateSet(data.EquipmentSet));

            _currentEnemies.Add(enemy);

            enemy.StartedDying
                .Subscribe((self: this, enemy), (_, state) => state.self.OnStartedDying(state.enemy))
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

        /// <summary>그 자리에서 반경 안에 살아 있는 적이 하나라도 있나. 목록을 만들지 않아 매 프레임 물어도 된다.</summary>
        public bool HasAliveEnemyInRadius(Vector3 center, float radius)
        {
            foreach (var enemy in _currentEnemies)
            {
                var offset = enemy.transform.position - center;
                offset.y = 0f;

                if (enemy.IsAlive && offset.magnitude <= radius)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>그 자리에서 반경 안에 든 살아 있는 적. 높이는 무시하고 바닥 거리로 잰다. 새 목록이라 받은 쪽이 들고 있어도 된다.</summary>
        public List<Enemy> FindAliveEnemiesInRadius(Vector3 center, float radius)
        {
            var found = new List<Enemy>();

            foreach (var enemy in _currentEnemies)
            {
                var offset = enemy.transform.position - center;
                offset.y = 0f;

                if (enemy.IsAlive && offset.magnitude <= radius)
                {
                    found.Add(enemy);
                }
            }

            return found;
        }

        /// <summary>
        /// 바닥 거리(높이 무시)로 잰 가장 가까운 살아 있는 적. radius 안에 없으면 null.
        /// 때릴 거리와 달리 위아래로 떨어진 적도 똑같이 가깝다 — 달려가서 붙는 가젯이 쓴다.
        /// </summary>
        public Enemy FindNearestAliveEnemyInRadius(Vector3 center, float radius)
        {
            Enemy nearest = null;
            var nearestDistance = radius;

            foreach (var enemy in _currentEnemies)
            {
                var offset = enemy.transform.position - center;
                offset.y = 0f;

                var distance = offset.magnitude;
                if (enemy.IsAlive && distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        /// <summary>이 적들 중 죽음처리된 적의 가장 긴 죽는 모션 길이. 다 눕기까지 기다릴 시간이다. 아무도 안 죽었으면 0.</summary>
        public float GetLongestDeathSeconds(IReadOnlyList<Enemy> enemies)
        {
            var longest = 0f;

            foreach (var enemy in enemies)
            {
                if (enemy.IsDead)
                {
                    longest = Mathf.Max(longest, enemy.DeathSeconds);
                }
            }

            return longest;
        }

        /// <summary>
        /// 그 자리에서 때릴 거리(CharacterCombat.DistanceOf) 안에 든 살아 있는 적. 깊이는 좁게 본다.
        /// 새 목록이라 받은 쪽이 돌면서 때려 죽여도 흔들리지 않는다.
        /// </summary>
        public List<Enemy> FindAliveEnemiesInReach(Vector3 center, float reach)
        {
            var found = new List<Enemy>();

            foreach (var enemy in _currentEnemies)
            {
                var offset = enemy.transform.position - center;

                if (enemy.IsAlive && CharacterCombat.DistanceOf(offset) <= reach)
                {
                    found.Add(enemy);
                }
            }

            return found;
        }

        /// <summary>
        /// 몸 앞쪽 네모 안에 든 살아 있는 적. 바라보는 쪽으로 frontReach, 등 뒤로 backReach, 위아래 깊이로 depthReach 까지.
        /// 새 목록이라 받은 쪽이 돌면서 때려 죽여도 흔들리지 않는다.
        /// </summary>
        public List<Enemy> FindAliveEnemiesInFront(Vector3 origin, bool isFacingRight,
            float backReach, float frontReach, float depthReach)
        {
            var found = new List<Enemy>();
            var forward = isFacingRight ? 1f : -1f;

            foreach (var enemy in _currentEnemies)
            {
                var offset = enemy.transform.position - origin;
                var ahead = offset.x * forward;

                if (enemy.IsAlive && ahead >= -backReach && ahead <= frontReach && Mathf.Abs(offset.z) <= depthReach)
                {
                    found.Add(enemy);
                }
            }

            return found;
        }

        /// <summary>때릴 거리로 잰 가장 가까운 적. maxReach 안에 아무도 없으면 null.</summary>
        public Enemy FindNearestEnemy(Vector3 from, float maxReach)
        {
            Enemy nearest = null;
            var nearestDistance = maxReach;

            foreach (var enemy in _currentEnemies)
            {
                var distance = CharacterCombat.DistanceOf(enemy.transform.position - from);
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        /// <summary>이 적들 중 서 있는(죽음처리 전인) 적 하나를 아무나 고른다. Dying 도 서 있다. 다 죽었으면 null.</summary>
        public Enemy PickStandingEnemy(IReadOnlyList<Enemy> enemies)
        {
            var standing = new List<Enemy>();

            foreach (var enemy in enemies)
            {
                if (!enemy.IsDead)
                {
                    standing.Add(enemy);
                }
            }

            return standing.Count > 0 ? standing[Random.Range(0, standing.Count)] : null;
        }

        /// <summary>이 적들 중 서 있는(죽음처리 전인) 적 중 가장 가까운 적. Dying 도 센다. 없으면 null.</summary>
        public Enemy FindNearestStandingEnemy(IReadOnlyList<Enemy> enemies, Vector3 from)
        {
            Enemy nearest = null;
            var nearestDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                var distance = (enemy.transform.position - from).sqrMagnitude;

                if (!enemy.IsDead && distance < nearestDistance)
                {
                    nearest = enemy;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        /// <summary>적을 그 깊이(Z) 줄로 옮겨 세운다.</summary>
        public void AlignDepth(Enemy enemy, float depth)
        {
            var position = enemy.transform.position;
            position.z = depth;
            enemy.transform.position = position;
        }

        /// <summary>
        /// 히어로 몸에 파고든 산 적을 옆으로 밀어낸다. 히어로는 밀리지 않는다 — 한쪽만 밀어서 적이 히어로를 떠밀고 다니지 못한다.
        /// 좌우로만 민다. 위아래(Z)로 밀면 사거리에서 빠진 적을 히어로가 쫓아가며 계속 밀고 다닌다.
        /// </summary>
        public void PushAwayFrom(Vector3 body)
        {
            foreach (var enemy in CurrentEnemies)
            {
                if (!enemy.IsAlive)
                {
                    continue;
                }

                var position = enemy.transform.position;
                var dx = position.x - body.x;

                if (Mathf.Abs(position.z - body.z) >= BodyDepth || Mathf.Abs(dx) >= CharacterCombat.MinGap)
                {
                    continue;
                }

                position.x = body.x + (dx >= 0f ? 1f : -1f) * CharacterCombat.MinGap;
                enemy.transform.position = position;
            }
        }

        /// <summary>이 적들을 궁극기 무대에 올리거나 내린다.</summary>
        public void SetOnUltimateStage(IReadOnlyList<Enemy> enemies, bool isOn)
        {
            foreach (var enemy in enemies)
            {
                enemy.SetOnUltimateStage(isOn);
            }
        }

        /// <summary>즉사시킨다. 체력을 깎아 죽이므로 죽음의 경로는 맞아 죽는 것과 똑같다 — 보류 중이면 Dying 으로 기다린다.</summary>
        public void KillEnemy(Enemy enemy)
        {
            enemy.TakeDamage(enemy.CurrentHP.CurrentValue);
        }

        /// <summary>
        /// 죽음을 보류한다. 돌려받은 걸 Dispose 할 때까지 HP 가 0 이 된 적은 Dying 으로 서서 계속 맞는다.
        /// 보류가 전부 풀리는 순간 Dying 인 적을 한꺼번에 죽음처리한다. 궁극기가 이걸 건다.
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
            foreach (var enemy in _currentEnemies.ToArray())
            {
                if (enemy.State.CurrentValue == CharacterStateType.Dying)
                {
                    enemy.Die();
                }
            }
        }

        /// <summary>Dying 이 됐다. 보류 중이 아니면 그 자리에서 죽음처리한다. 보류 중이면 풀릴 때 한꺼번에 죽인다.</summary>
        private void OnStartedDying(Enemy enemy)
        {
            if (_deathHolds > 0)
            {
                return;
            }

            enemy.Die();
        }

        /// <summary>한 마리를 파괴한다. 죽이는 게 아니라 없던 일로 만드는 것이라 Died 는 울리지 않는다.</summary>
        public void DestroyEnemy(Enemy enemy)
        {
            _currentEnemies.Remove(enemy);
            _corpses.Remove(enemy);

            Object.Destroy(enemy.gameObject);
        }

        /// <summary>살아있든 시체든 전부 파괴한다. 웨이브가 끝나면 이걸로 판을 비운다.</summary>
        public void DestroyAllEnemies()
        {
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
