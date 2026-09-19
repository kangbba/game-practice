using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 전투 흐름의 주인. 한 번 도는 동안 페이즈용 로딩 → 인게임 → 정리를 스스로 다 한다.
    /// 페이즈용 로딩: 이 전투가 쓸 에셋을 로드하고 BattleScope 를 열어 판(맵·히어로)을 세운다.
    /// 지금은 따로 보여 줄 화면 없이 로직으로만 있다 — 메인 로딩 화면이 걷히는 사이에 끝난다.
    /// 나갈 때 스코프를 접고 에셋을 놓는다. 로드한 쪽이 놓으므로 전투 매니저와 전투 에셋의 수명이 곧 이 페이즈의 수명이다.
    /// </summary>
    public class InGamePhase : PhaseBase
    {
        /// <summary>웨이브를 다 잡고 다음 웨이브가 시작되기까지 숨 돌리는 시간.</summary>
        private const float WaveGapSeconds = 2f;

        private readonly GameContext _game;

        private BattleAssets _assets;
        private BattleScope _scope;

        public override string Key => PhaseID.InGame;

        public InGamePhase(GameContext game)
        {
            _game = game;
        }

        public override void Enter(CancellationToken token)
        {
        }

        /// <summary>끝이 없다. 웨이브는 스테이지를 넘어가며 계속 돈다.</summary>
        public override async UniTask<PhaseBase> MainLogicAsync(CancellationToken token)
        {
            await LoadAsync(token);

            var initialStage = 1;
            var initialWave = 1;

            var stage = initialStage;
            var wave = initialWave;

            while (true)
            {
                var stagePlan = _scope.Context.StageManager.GetStagePlan(stage);

                while (wave <= stagePlan.Waves.Count)
                {
                    _scope.Context.StageManager.SetWave(stage, wave);
                    await FightAsync(stagePlan.GetEnemies(wave), token);
                    await UniTask.Delay(TimeSpan.FromSeconds(WaveGapSeconds), cancellationToken: token);

                    wave++;
                }

                Debug.Log($"{stage}스테이지 클리어");

                stage++;
                wave = 1;
            }
        }

        /// <summary>
        /// 나갈 때 판을 접고 에셋을 놓는다. 로드 도중에 끊겼으면 아직 없다 — 로드는 취소되며 스스로 놓는다.
        /// 적·히어로·맵은 스코프가 해제되며 각자 매니저가 치우고, 에셋은 그걸 다 치운 뒤에 놓는다.
        /// </summary>
        public override void Exit()
        {
            _scope?.Release();
            _scope = null;

            _assets?.Release();
            _assets = null;
        }

        /// <summary>페이즈용 로딩. 진행도를 보여 줄 화면이 아직 없어서 받기만 하고 버린다.</summary>
        private async UniTask LoadAsync(CancellationToken token)
        {
            _assets = await BattleAssets.LoadAsync(_ => { }, token);
            _scope = new BattleScope(_game, _assets);

            _scope.Context.MapManager.CreateMainMap();
            _scope.Context.HeroManager.SpawnLeader(Vector3.zero);
        }

        /// <summary>
        /// 적을 내보내고, 다 죽을 때까지 기다렸다가 판을 비운다.
        /// 궁극기 연출이 도는 중이면 그게 끝날 때까지 판을 안 치운다 — 죽는 몸과 어두운 배경이 끝까지 보여야 한다.
        /// </summary>
        private async UniTask FightAsync(IReadOnlyDictionary<string, int> enemies, CancellationToken token)
        {
            _scope.Context.EnemyManager.SpawnEnemies(enemies);

            await UniTask.WaitUntil(() => _scope.Context.EnemyManager.IsAliveEnemyNone() && !_scope.Context.UltimateDirector.IsPlaying.CurrentValue,
                cancellationToken: token);

            _scope.Context.EnemyManager.DestroyAllEnemies();
        }

    }
}
