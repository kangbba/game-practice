using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 웨이브 진행 상태의 주인. 들고 있는 건 "몇 스테이지 몇 웨이브" 와 처치 수뿐이다.
    /// 목표 마릿수·보스 여부·표기는 전부 그 번호로 설계값을 찾아 그때그때 뽑는다 — 따로 적어두지 않는다.
    /// </summary>
    public class WaveManager : ManagerBase
    {
        private readonly EnemyManager _enemyManager;

        private readonly ReactiveProperty<int> _currentStageNumber = new ReactiveProperty<int>(1);
        private readonly ReactiveProperty<int> _currentWaveNumber = new ReactiveProperty<int>(1);
        private readonly ReactiveProperty<int> _kills = new ReactiveProperty<int>();
        private readonly Subject<(int stage, int wave)> _waveStarted = new Subject<(int stage, int wave)>();

        /// <summary>마지막으로 찾아본 스테이지. 같은 스테이지를 웨이브마다 다시 찾지 않는다.</summary>
        private int _loadedStage;
        private StagePlan _loadedStagePlan;

        public ReadOnlyReactiveProperty<int> CurrentStageNumber => _currentStageNumber;
        public ReadOnlyReactiveProperty<int> CurrentWaveNumber => _currentWaveNumber;

        /// <summary>웨이브가 시작됐다. 번호 둘을 다 적은 뒤에 딱 한 번 울린다 — 시작 알림 같은 건 번호 말고 이걸 본다.</summary>
        public Observable<(int stage, int wave)> WaveStarted => _waveStarted;

        /// <summary>이번 웨이브에서 잡은 수.</summary>
        public ReadOnlyReactiveProperty<int> Kills => _kills;

        /// <summary>이번 웨이브에 잡아야 할 수. 번호가 바뀌면 설계값에서 다시 센다.</summary>
        public Observable<int> Goal => _currentStageNumber
            .CombineLatest(_currentWaveNumber, (stage, wave) => (stage, wave))
            .Select(this, (number, self) => self.CountEnemies(number.stage, number.wave));

        /// <summary>지금이 보스 판인가. 보스 판은 그 스테이지의 마지막 웨이브다.</summary>
        public Observable<bool> IsBossWave => _currentStageNumber
            .CombineLatest(_currentWaveNumber, (stage, wave) => (stage, wave))
            .Select(this, (number, self) => self.GetStagePlan(number.stage).IsBossWave(number.wave));

        /// <summary>화면에 그대로 쓰는 표기. "STAGE 1 - 3" 또는 "STAGE 1 - BOSS".</summary>
        public Observable<string> Label => _currentStageNumber
            .CombineLatest(_currentWaveNumber, (stage, wave) => (stage, wave))
            .Select(this, (number, self) => self.GetLabel(number.stage, number.wave));

        public WaveManager(EnemyManager enemyManager)
        {
            _enemyManager = enemyManager;
        }

        protected override void OnInit()
        {
            _enemyManager.Died
                .Subscribe(this, (_, self) => self._kills.Value++)
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            _currentStageNumber.Dispose();
            _currentWaveNumber.Dispose();
            _kills.Dispose();
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

            // 스테이지를 옮길 땐 1웨이브를 거쳐 간다. 1웨이브는 어느 스테이지에나 있어서,
            // 번호 둘을 차례로 적는 사이의 중간값도 늘 설계값에 있는 번호다.
            if (stage != _currentStageNumber.Value)
            {
                _currentWaveNumber.Value = 1;
                _currentStageNumber.Value = stage;
            }

            _currentWaveNumber.Value = wave;
            _kills.Value = 0;

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
