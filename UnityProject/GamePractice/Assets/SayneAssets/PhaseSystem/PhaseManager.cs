using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Sayne
{
    public class PhaseManager : ManagerBase
    {
        private readonly ReactiveProperty<PhaseBase> _currentPhase = new ReactiveProperty<PhaseBase>();
        private readonly CancellationToken _ownerToken;
        private readonly string _name;

        private CancellationTokenSource _phaseCts;

        public ReadOnlyReactiveProperty<PhaseBase> CurrentPhase => _currentPhase;

        public PhaseManager(string name, CancellationToken ownerToken)
        {
            _name = name;
            _ownerToken = ownerToken;
        }

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            ClearPhase();
            _currentPhase.Dispose();
        }

        public void SetPhase(PhaseBase phase)
        {
            Debug.Log($"{_name}: {_currentPhase.Value?.Key ?? "(none)"} -> {phase.Key}");

            ClearPhase();

            _phaseCts = CancellationTokenSource.CreateLinkedTokenSource(_ownerToken);
            _currentPhase.Value = phase;

            phase.Enter(_phaseCts.Token);
        }

        public async UniTask RunAsync(PhaseBase phase)
        {
            SetPhase(phase);

            await phase.MainLogicAsync(_phaseCts.Token).SuppressCancellationThrow();

            if (_currentPhase.Value == phase)
            {
                ClearPhase();
            }
        }

        private void ClearPhase()
        {
            if (_currentPhase.Value == null)
            {
                return;
            }

            _phaseCts.Cancel();
            _phaseCts.Dispose();
            _phaseCts = null;

            _currentPhase.Value.Exit();
            _currentPhase.Value = null;
        }
    }
}
