using R3;
using UnityEngine;

namespace Sayne
{
    public class PhaseManager : ManagerBase
    {
        private readonly ReactiveProperty<PhaseBase> _currentPhase = new ReactiveProperty<PhaseBase>();

        public ReadOnlyReactiveProperty<PhaseBase> CurrentPhase => _currentPhase;

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            if (_currentPhase.Value != null)
            {
                _currentPhase.Value.OnExit();
            }

            _currentPhase.Value = null;
            _currentPhase.Dispose();
        }

        public void ChangePhase(PhaseBase phase)
        {
            Debug.Log($"PhaseManager: {_currentPhase.Value?.Key ?? "(none)"} -> {phase.Key}");

            if (_currentPhase.Value != null)
            {
                _currentPhase.Value.OnExit();
            }

            _currentPhase.Value = phase;
            phase.OnEnter();
        }
    }
}
