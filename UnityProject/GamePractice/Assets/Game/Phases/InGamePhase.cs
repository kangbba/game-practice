using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>전투 흐름의 주인. 맵과 히어로를 세우고 웨이브 루프를 스스로 돌린다.</summary>
    public class InGamePhase : PhaseBase
    {
        private readonly MapManager _mapManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly WaveManager _waveManager;

        public override string Key => PhaseID.InGame;

        public InGamePhase(MapManager mapManager, HeroManager heroManager, EnemyManager enemyManager,
            WaveManager waveManager)
        {
            _mapManager = mapManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _waveManager = waveManager;
        }

        public override void Enter(CancellationToken token)
        {
            _mapManager.CreateMap(MapID.World);
            _heroManager.SpawnHero(HeroID.Aldric, Vector3.zero);
        }

        public override async UniTask MainLogicAsync(CancellationToken token)
        {
            var stage = 1;
            while (true)
            {
                var stagePlan = _waveManager.GetStagePlan(stage);

                for (var i = 0; i < stagePlan.Waves.Count; i++)
                {
                    var number = new WaveNumber(stage, i + 1);
                    var spawned = _enemyManager.SpawnEnemies(stagePlan.Waves[i]);

                    Debug.Log($"{number.Label} 시작 — {spawned}마리");
                    _waveManager.ResetWave(number, spawned);

                    await WaitUntilClearedAsync(token);
                }

                var bossNumber = WaveNumber.Boss(stage);
                _enemyManager.SpawnEnemy(stagePlan.BossEnemyID);

                Debug.Log($"{bossNumber.Label} 시작");
                _waveManager.ResetWave(bossNumber, 1);

                await WaitUntilClearedAsync(token);

                stage++;
            }
        }

        public override void Exit()
        {
            _enemyManager.DestroyAllEnemies();
        }

        /// <summary>내보낸 적이 다 죽을 때까지 기다렸다가 시체를 치운다.</summary>
        private async UniTask WaitUntilClearedAsync(CancellationToken token)
        {
            await _enemyManager.AliveCount.Where(count => count == 0).FirstAsync(cancellationToken: token);

            _enemyManager.DestroyAllEnemies();
        }
    }
}
