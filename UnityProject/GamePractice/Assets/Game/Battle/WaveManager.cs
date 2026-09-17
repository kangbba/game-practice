using R3;

namespace Sayne
{
    /// <summary>웨이브 진행 상태의 주인. 몇 번째 웨이브이고 몇 마리 중 몇 마리를 잡았는지 들고 있다.</summary>
    public class WaveManager : ManagerBase
    {
        private readonly EnemyManager _enemyManager;

        private readonly ReactiveProperty<WaveNumber> _currentWave = new ReactiveProperty<WaveNumber>();
        private readonly ReactiveProperty<int> _kills = new ReactiveProperty<int>();
        private readonly ReactiveProperty<int> _goal = new ReactiveProperty<int>();

        public ReadOnlyReactiveProperty<WaveNumber> CurrentWave => _currentWave;

        /// <summary>이번 웨이브에서 잡은 수와 잡아야 할 수.</summary>
        public ReadOnlyReactiveProperty<int> Kills => _kills;
        public ReadOnlyReactiveProperty<int> Goal => _goal;

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
            _currentWave.Dispose();
            _kills.Dispose();
            _goal.Dispose();
        }

        /// <summary>그 스테이지의 설계값. 전투가 이걸 받아 웨이브를 순서대로 돈다.</summary>
        public StagePlan GetStagePlan(int stage)
        {
            return StagePlans.Get(stage);
        }

        /// <summary>이번 웨이브 몫을 셀 계량기를 0 으로 돌린다. 목표는 실제로 내보낸 마릿수다.</summary>
        public void ResetWave(WaveNumber wave, int goal)
        {
            _currentWave.Value = wave;
            _goal.Value = goal;
            _kills.Value = 0;
        }
    }
}
