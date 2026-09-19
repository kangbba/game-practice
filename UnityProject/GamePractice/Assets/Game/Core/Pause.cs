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
    ///
    /// static 이다. 감싸는 Time.timeScale 자체가 전역이라, 주입해 봐야 갈아 끼울 수 있는 게 없다.
    /// 상태는 걸린 요청 수 하나뿐이고 플레이를 시작할 때마다 새로 세운다(도메인 리로드를 꺼도 남지 않는다).
    /// </summary>
    public static class Pause
    {
        private static ReactiveProperty<int> _holds;
        private static ReadOnlyReactiveProperty<bool> _isPaused;

        public static ReadOnlyReactiveProperty<bool> IsPaused => _isPaused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            // 첫 플레이에는 아직 없다. 도메인 리로드를 끈 채 다시 플레이하면 앞 판의 것이 남아 있다.
            _isPaused?.Dispose();
            _holds?.Dispose();

            _holds = new ReactiveProperty<int>(0);
            _isPaused = _holds
                .Select(holds => holds > 0)
                .ToReadOnlyReactiveProperty();

            _isPaused.Subscribe(paused => Time.timeScale = paused ? 0f : 1f);
        }

        /// <summary>구간이 조건 하나로 안 떨어질 때(컷씬 시퀀스·창 열기 등) 직접 잡는 중단. 돌려받은 걸 Dispose 하면 놓는다.</summary>
        public static IDisposable Hold()
        {
            _holds.Value++;
            return Disposable.Create(() => _holds.Value--);
        }

        /// <summary>
        /// 조건이 참인 동안 중단한다. "이 연출이 도는 동안" 같은 선언은 전부 이걸로 건다.
        /// 돌려받은 걸 부른 쪽의 수명에 걸어 둔다 — 치우면 구독도 끊고 잡고 있던 중단도 놓는다.
        /// </summary>
        public static IDisposable While(Observable<bool> condition)
        {
            var hold = new SerialDisposable();

            var subscription = condition
                .DistinctUntilChanged()
                .Subscribe(hold, (on, state) => state.Disposable = on ? Hold() : null);

            return Disposable.Combine(subscription, hold);
        }
    }
}
