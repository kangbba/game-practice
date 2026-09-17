using System.Threading;
using Cysharp.Threading.Tasks;

namespace Sayne
{
    public abstract class PhaseBase
    {
        public abstract string Key { get; }

        public abstract void Enter(CancellationToken token);

        /// <summary>
        /// 이 페이즈가 할 일. 반환이 곧 "내 일은 끝났다" 이고, 돌려준 페이즈가 다음 차례다.
        /// null 을 돌려주면 거기서 흐름이 끝난다. 갈아끼우는 건 PhaseManager 가 한다 —
        /// 자기 안에서 직접 전환하면 아직 안 끝난 자기를 끝내는 꼴이 된다.
        /// </summary>
        public abstract UniTask<PhaseBase> MainLogicAsync(CancellationToken token);

        public abstract void Exit();
    }
}
