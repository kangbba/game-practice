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
            OnRelease();

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        protected abstract void OnInit();
        protected abstract void OnRelease();
    }
}
