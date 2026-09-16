using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>웨이브 진행 상태의 주인. 몇 번째 웨이브이고 몇 마리를 잡았는지 들고 있다.</summary>
    public class WaveManager : ManagerBase
    {
        private readonly EnemyManager _enemyManager;

        private float _startTime;

        private readonly ReactiveProperty<int> _currentWave = new ReactiveProperty<int>(1);
        private readonly ReactiveProperty<int> _kills = new ReactiveProperty<int>();
        private readonly ReactiveProperty<int> _goal = new ReactiveProperty<int>();
        private readonly ReactiveProperty<WaveResult> _lastResult = new ReactiveProperty<WaveResult>();

        public ReadOnlyReactiveProperty<int> CurrentWave => _currentWave;

        /// <summary>이번 웨이브에서 잡은 수와 잡아야 할 수.</summary>
        public ReadOnlyReactiveProperty<int> Kills => _kills;
        public ReadOnlyReactiveProperty<int> Goal => _goal;

        /// <summary>방금 끝난 웨이브의 결과. 정산 화면이 이걸 그린다.</summary>
        public ReadOnlyReactiveProperty<WaveResult> LastResult => _lastResult;

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
            _lastResult.Dispose();
        }

        /// <summary>웨이브가 시작될 때 전투가 알려준다. 목표는 그 웨이브 구성에서 센다.</summary>
        public void BeginWave(int wave)
        {
            var goal = 0;

            foreach (var entry in WavePlans.Get(wave))
            {
                goal += entry.Count;
            }

            _currentWave.Value = wave;
            _goal.Value = goal;
            _kills.Value = 0;

            _startTime = Time.time;
        }

        /// <summary>웨이브가 끝났을 때 전투가 알려준다.</summary>
        public void EndWave()
        {
            _lastResult.Value = new WaveResult(_currentWave.Value, _kills.Value, Time.time - _startTime);
        }
    }
}
