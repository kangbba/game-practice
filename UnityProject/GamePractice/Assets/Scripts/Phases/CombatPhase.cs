using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class CombatPhase : PhaseBase
    {
        private const int TestEnemyCount = 8;
        private const float SpawnRadiusMin = 5f;
        private const float SpawnRadiusMax = 12f;

        private readonly EnemyManager _enemyManager;

        public override string Key => PhaseID.Combat;

        public CombatPhase(EnemyManager enemyManager)
        {
            _enemyManager = enemyManager;
        }

        public override void Enter(CancellationToken token)
        {
            for (var i = 0; i < TestEnemyCount; i++)
            {
                _enemyManager.SpawnRandomEnemy(RandomPosition());
            }
        }

        public override UniTask MainLogicAsync(CancellationToken token)
        {
            return UniTask.Never(token);
        }

        public override void Exit()
        {
            _enemyManager.DespawnAll();
        }

        private static Vector3 RandomPosition()
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var distance = Random.Range(SpawnRadiusMin, SpawnRadiusMax);
            return new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        }
    }
}
