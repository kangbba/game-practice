using System.Threading;
using Cysharp.Threading.Tasks;

namespace Sayne
{
    public abstract class PhaseBase
    {
        public abstract string Key { get; }

        public abstract void Enter(CancellationToken token);
        public abstract UniTask MainLogicAsync(CancellationToken token);
        public abstract void Exit();
    }
}
