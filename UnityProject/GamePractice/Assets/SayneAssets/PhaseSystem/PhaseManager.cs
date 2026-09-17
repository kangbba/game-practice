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

        /// <summary>페이즈가 돌려준 다음 페이즈를 따라 끝까지 흐른다. null 이 나오면 거기서 끝이다.</summary>
        public async UniTask RunAsync(PhaseBase phase)
        {
            while (phase != null)
            {
                SetPhase(phase);

                var result = await phase.MainLogicAsync(_phaseCts.Token).SuppressCancellationThrow();
                if (result.IsCanceled)
                {
                    return;
                }

                phase = result.Result;
            }

            ClearPhase();
        }

        private void SetPhase(PhaseBase phase)
        {
            Debug.Log($"{_name}: {_currentPhase.Value?.Key ?? "(none)"} -> {phase.Key}");

            ClearPhase();

            _phaseCts = CancellationTokenSource.CreateLinkedTokenSource(_ownerToken);
            _currentPhase.Value = phase;

            phase.Enter(_phaseCts.Token);
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
