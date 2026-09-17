using System;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 게임 중단의 유일한 창구. Time.timeScale 은 여기서만 건드린다.
    /// 중단 = 타임스케일 0 — 이동·타이머·애니메이션·트윈·파티클이 전부 스케일 시간을 쓰므로 한 번에 같은 시점에서 멈춘다.
    /// 중단 중에도 움직여야 하는 것(팝업·컷씬 연출)은 그쪽이 unscaled 시간을 쓴다.
    ///
    /// 요청은 겹칠 수 있다 — 컷씬 도중 메뉴를 열었다 닫아도 컷씬이 끝나기 전엔 풀리지 않는다.
    /// </summary>
    public class PauseManager : ManagerBase
    {
        /// <summary>지금 중단을 잡고 있는 요청 수. 0 이 되어야 재개된다.</summary>
        private readonly ReactiveProperty<int> _holds = new ReactiveProperty<int>(0);

        public ReadOnlyReactiveProperty<bool> IsPaused { get; private set; }

        protected override void OnInit()
        {
            IsPaused = _holds
                .Select(holds => holds > 0)
                .ToReadOnlyReactiveProperty();

            IsPaused
                .Subscribe(paused => Time.timeScale = paused ? 0f : 1f)
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            IsPaused.Dispose();
            _holds.Dispose();

            Time.timeScale = 1f;
        }

        /// <summary>조건이 참인 동안 중단한다. "이 창이 열려 있는 동안" 같은 선언은 전부 이걸로 건다.</summary>
        public void PauseWhile(Observable<bool> condition)
        {
            var hold = new SerialDisposable();
            hold.RegisterTo(LifeToken);

            condition
                .DistinctUntilChanged()
                .Subscribe((self: this, hold), (on, state) =>
                    state.hold.Disposable = on ? state.self.Pause() : null)
                .RegisterTo(LifeToken);
        }

        /// <summary>구간이 조건 하나로 안 떨어질 때(컷씬 시퀀스 등) 직접 잡는 중단. 돌려받은 걸 Dispose 하면 놓는다.</summary>
        public IDisposable Pause()
        {
            _holds.Value++;
            return Disposable.Create(this, self => self._holds.Value--);
        }
    }
}
