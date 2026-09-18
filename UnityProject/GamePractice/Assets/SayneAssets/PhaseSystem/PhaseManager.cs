using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Sayne
{
    public class PhaseManager : ManagerBase
    {
        private readonly ReactiveProperty<PhaseBase> _currentPhase = new ReactiveProperty<PhaseBase>();

        private CancellationTokenSource _phaseCts;

        public ReadOnlyReactiveProperty<PhaseBase> CurrentPhase => _currentPhase;

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
            // 나가는 길은 하나다 — 취소든 예외든 정상 종료든 페이즈를 접고 링크 등록을 떼어낸다.
            try
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
            }
            finally
            {
                ClearPhase();
            }
        }

        private void SetPhase(PhaseBase phase)
        {
            Debug.Log($"Phase: {_currentPhase.Value?.Key ?? "(none)"} -> {phase.Key}");

            ClearPhase();

            _phaseCts = CancellationTokenSource.CreateLinkedTokenSource(LifeToken);
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
