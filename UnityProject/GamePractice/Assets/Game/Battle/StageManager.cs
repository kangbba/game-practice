using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 스테이지·웨이브 진행 상태의 주인. 들고 있는 건 "몇 스테이지 몇 웨이브" 와 처치 수뿐이다.
    /// 목표 마릿수·보스 여부·표기는 전부 그 번호로 설계값을 찾아 그때그때 뽑는다 — 따로 적어두지 않는다.
    /// 번호는 SetWave 로만 바뀌므로 구독거리가 아니다. 바뀐 걸 알고 싶으면 WaveStarted 를 본다.
    /// </summary>
    public class StageManager : ManagerBase
    {
        private readonly EnemyManager _enemyManager;

        private readonly ReactiveProperty<int> _stageKills = new ReactiveProperty<int>();
        private readonly ReactiveProperty<int> _waveKills = new ReactiveProperty<int>();
        private readonly Subject<(int stage, int wave)> _waveStarted = new Subject<(int stage, int wave)>();

        /// <summary>마지막으로 찾아본 스테이지. 같은 스테이지를 웨이브마다 다시 찾지 않는다.</summary>
        private int _loadedStage;
        private StagePlan _loadedStagePlan;

        public int CurrentStageNumber { get; private set; } = 1;
        public int CurrentWaveNumber { get; private set; } = 1;

        /// <summary>웨이브가 시작됐다. 번호를 다 적은 뒤에 울린다.</summary>
        public Observable<(int stage, int wave)> WaveStarted => _waveStarted;

        /// <summary>이번 스테이지에서 잡은 수. 웨이브가 넘어가도 이어서 센다.</summary>
        public ReadOnlyReactiveProperty<int> StageKills => _stageKills;

        /// <summary>이번 웨이브에서 잡은 수.</summary>
        public ReadOnlyReactiveProperty<int> WaveKills => _waveKills;

        /// <summary>이번 웨이브에 잡아야 할 수.</summary>
        public int WaveGoal => CountEnemies(CurrentStageNumber, CurrentWaveNumber);

        /// <summary>지금이 보스 판인가. 보스 판은 그 스테이지의 마지막 웨이브다.</summary>
        public bool IsBossWave => GetStagePlan(CurrentStageNumber).IsBossWave(CurrentWaveNumber);

        /// <summary>지금 번호의 표기. "STAGE 1-3" 또는 "STAGE 1 BOSS".</summary>
        public string Label => GetLabel(CurrentStageNumber, CurrentWaveNumber);

        public StageManager(EnemyManager enemyManager)
        {
            _enemyManager = enemyManager;
        }

        protected override void OnInit()
        {
            _enemyManager.Died
                .Subscribe(this, (_, self) =>
                {
                    self._stageKills.Value++;
                    self._waveKills.Value++;
                })
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            _stageKills.Dispose();
            _waveKills.Dispose();
            _waveStarted.Dispose();
        }

        /// <summary>그 스테이지의 설계값. 전투가 이걸 받아 웨이브를 순서대로 돈다.</summary>
        public StagePlan GetStagePlan(int stage)
        {
            if (stage != _loadedStage)
            {
                _loadedStage = stage;
                _loadedStagePlan = StagePlans.Get(stage);
            }

            return _loadedStagePlan;
        }

        /// <summary>지금은 몇 스테이지 몇 웨이브다. 기록만 한다 — 적을 내보내는 건 적 매니저 일이다.</summary>
        public void SetWave(int stage, int wave)
        {
            Debug.Log($"{GetLabel(stage, wave)} 시작");

            // 새 스테이지거나 1웨이브부터 다시 도는 거면 스테이지 처치 수도 새로 센다.
            var isStageStart = stage != CurrentStageNumber || wave == 1;

            // 번호를 먼저 적는다. 처치 수 구독자가 새 번호를 읽게.
            CurrentStageNumber = stage;
            CurrentWaveNumber = wave;

            if (isStageStart)
            {
                _stageKills.Value = 0;
            }

            _waveKills.Value = 0;

            _waveStarted.OnNext((stage, wave));
        }

        private int CountEnemies(int stage, int wave)
        {
            var goal = 0;

            foreach (var count in GetStagePlan(stage).GetEnemies(wave).Values)
            {
                goal += count;
            }

            return goal;
        }

        /// <summary>
        /// 그 번호의 표기. HUD 명패도 시작 알림도 로그도 전부 이 한 곳에서 나온 글자를 쓴다.
        /// 평소엔 "STAGE 1-2", 보스 판은 번호 대신 "STAGE 1 BOSS" — 명패 폭 안에 들어가게 짧게 쓴다.
        /// </summary>
        public string GetLabel(int stage, int wave)
        {
            return GetStagePlan(stage).IsBossWave(wave) ? $"STAGE {stage} BOSS" : $"STAGE {stage}-{wave}";
        }
    }
}
