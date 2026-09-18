using System.Threading;
using Cysharp.Threading.Tasks;

namespace Sayne
{
    /// <summary>
    /// 로딩 화면 위에서 시작 탭을 기다린다. 에셋 로딩 자체는 GameManager 가 매니저 조립 전에 끝내고,
    /// 그동안의 진행도는 같은 로딩 화면이 보여 준다. 여기 들어올 즈음이면 게이지가 마저 차오르는 중이다.
    /// </summary>
    public class LoadingPhase : PhaseBase
    {
        private readonly LoadingScreenManager _loadingScreenManager;
        private readonly PhaseBase _nextPhase;

        public override string Key => PhaseID.Loading;

        public LoadingPhase(LoadingScreenManager loadingScreenManager, PhaseBase nextPhase)
        {
            _loadingScreenManager = loadingScreenManager;
            _nextPhase = nextPhase;
        }

        public override void Enter(CancellationToken token)
        {
        }

        public override async UniTask<PhaseBase> MainLogicAsync(CancellationToken token)
        {
            await _loadingScreenManager.WaitStartAsync(token);

            return _nextPhase;
        }

        /// <summary>화면이 옅어지는 사이 다음 페이즈가 판을 세운다 — 걷히면서 전투가 비쳐 보인다.</summary>
        public override void Exit()
        {
            _loadingScreenManager.Hide();
        }
    }
}
