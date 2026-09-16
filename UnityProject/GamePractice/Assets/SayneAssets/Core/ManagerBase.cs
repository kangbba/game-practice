using System.Threading;

namespace Sayne
{
    public abstract class ManagerBase
    {
        private CancellationTokenSource _cts;

        protected CancellationToken LifeToken => _cts.Token;

        public void Init()
        {
            _cts = new CancellationTokenSource();
            OnInit();
        }

        public void Release()
        {
            // 먼저 취소해서 진행 중인 작업을 끊고, 그 다음에 하위 클래스가 자원을 반납한다.
            _cts.Cancel();

            OnRelease();

            _cts.Dispose();
            _cts = null;
        }

        protected abstract void OnInit();
        protected abstract void OnRelease();
    }
}
