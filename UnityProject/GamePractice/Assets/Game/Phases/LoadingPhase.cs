using System.Threading;
using Cysharp.Threading.Tasks;

namespace Sayne
{
    /// <summary>아직 빈 껍데기. 에셋 로딩은 GameManager 가 매니저 조립 전에 한다.</summary>
    public class LoadingPhase : PhaseBase
    {
        private readonly PhaseBase _nextPhase;

        public override string Key => PhaseID.Loading;

        public LoadingPhase(PhaseBase nextPhase)
        {
            _nextPhase = nextPhase;
        }

        public override void Enter(CancellationToken token)
        {
        }

        public override UniTask<PhaseBase> MainLogicAsync(CancellationToken token)
        {
            return UniTask.FromResult(_nextPhase);
        }

        public override void Exit()
        {
        }
    }
}
