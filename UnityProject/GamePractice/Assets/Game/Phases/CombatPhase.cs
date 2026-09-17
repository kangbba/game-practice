using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class CombatPhase : PhaseBase
    {
        private const float SpawnRadiusMin = 5f;
        private const float SpawnRadiusMax = 12f;

        private readonly EnemyManager _enemyManager;
        private readonly WaveNumber _wave;
        private readonly List<Enemy> _spawned = new List<Enemy>();

        public override string Key => PhaseID.Combat;

        public CombatPhase(EnemyManager enemyManager, WaveNumber wave)
        {
            _enemyManager = enemyManager;
            _wave = wave;
        }

        public override void Enter(CancellationToken token)
        {
            foreach (var entry in WavePlans.Get(_wave))
            {
                for (var i = 0; i < entry.Count; i++)
                {
                    var enemy = _enemyManager.SpawnEnemy(entry.EnemyID, RandomPosition());
                    if (enemy != null)
                    {
                        _spawned.Add(enemy);
                    }
                }
            }
        }

        public override UniTask MainLogicAsync(CancellationToken token)
        {
            return UniTask.WaitUntil(AllEnemiesDead, cancellationToken: token);
        }

        public override void Exit()
        {
            _spawned.Clear();
            _enemyManager.DespawnAll();
        }

        private bool AllEnemiesDead()
        {
            foreach (var enemy in _spawned)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector3 RandomPosition()
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var distance = Random.Range(SpawnRadiusMin, SpawnRadiusMax);
            return new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        }
    }
}
