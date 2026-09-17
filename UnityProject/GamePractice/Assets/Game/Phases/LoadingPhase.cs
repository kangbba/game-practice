using System.Threading;
using Cysharp.Threading.Tasks;

namespace Sayne
{
    /// <summary>아직 빈 껍데기. 에셋 로딩은 GameManager 가 매니저 조립 전에 한다.</summary>
    public class LoadingPhase : PhaseBase
    {
        public override string Key => PhaseID.Loading;

        public override void Enter(CancellationToken token)
        {
        }

        public override UniTask MainLogicAsync(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        public override void Exit()
        {
        }
    }
}
