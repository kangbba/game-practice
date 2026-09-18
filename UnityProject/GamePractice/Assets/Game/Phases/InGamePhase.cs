using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        private readonly UltimateDirector _ultimateDirector;

        public override string Key => PhaseID.InGame;

        public InGamePhase(MapManager mapManager, HeroManager heroManager, EnemyManager enemyManager,
            WaveManager waveManager, UltimateDirector ultimateDirector)
        {
            _mapManager = mapManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _waveManager = waveManager;
            _ultimateDirector = ultimateDirector;
        }

        public override void Enter(CancellationToken token)
        {
            _mapManager.CreateMainMap();
            _heroManager.SpawnHero(HeroID.Aldric, Vector3.zero);
        }

        /// <summary>끝이 없다. 웨이브는 스테이지를 넘어가며 계속 돈다.</summary>
        public override async UniTask<PhaseBase> MainLogicAsync(CancellationToken token)
        {
            var initialStage = 1;
            var initialWave = 1;

            var stage = initialStage;
            var wave = initialWave;

            while (true)
            {
                var stagePlan = _waveManager.GetStagePlan(stage);

                while (wave <= stagePlan.Waves.Count)
                {
                    _waveManager.SetWave(stage, wave);
                    await FightAsync(stagePlan.GetEnemies(wave), token);

                    wave++;
                }

                Debug.Log($"{stage}스테이지 클리어");

                stage++;
                wave = 1;
            }
        }

        public override void Exit()
        {
            _enemyManager.DestroyAllEnemies();
        }

        /// <summary>
        /// 적을 내보내고, 다 죽을 때까지 기다렸다가 판을 비운다.
        /// 궁극기 연출이 도는 중이면 그게 끝날 때까지 판을 안 치운다 — 쓰러지는 몸과 어두운 배경이 끝까지 보여야 한다.
        /// </summary>
        private async UniTask FightAsync(IReadOnlyDictionary<string, int> enemies, CancellationToken token)
        {
            _enemyManager.SpawnEnemies(enemies);

            await UniTask.WaitUntil(() => _enemyManager.IsAliveEnemyNone() && !_ultimateDirector.IsPlaying.CurrentValue,
                cancellationToken: token);

            _enemyManager.DestroyAllEnemies();
        }

    }
}
