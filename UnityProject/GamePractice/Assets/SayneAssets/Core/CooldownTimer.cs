using System;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 재사용 쿨타임. 남은 초와 남은 비율을 리액티브로 내보낸다.
    /// 도는 동안만 프레임을 구독하고, 다 돌면 스스로 끊는다.
    /// </summary>
    public class CooldownTimer : IDisposable
    {
        private readonly ReactiveProperty<float> _remainSeconds = new ReactiveProperty<float>();
        private readonly ReactiveProperty<float> _remainRatio = new ReactiveProperty<float>();

        private IDisposable _tick;
        private float _duration;
        private float _endTime;

        /// <summary>남은 초. 0 이면 바로 쓸 수 있다.</summary>
        public ReadOnlyReactiveProperty<float> RemainSeconds => _remainSeconds;

        /// <summary>남은 비율 0~1. fillAmount 에 그대로 꽂는 값이다.</summary>
        public ReadOnlyReactiveProperty<float> RemainRatio => _remainRatio;

        public bool IsReady => _remainSeconds.Value <= 0f;

        public void Begin(float duration)
        {
            _tick?.Dispose();

            if (duration <= 0f)
            {
                Finish();
                return;
            }

            _duration = duration;
            _endTime = Time.time + duration;
            _remainSeconds.Value = duration;
            _remainRatio.Value = 1f;

            _tick = Observable.EveryUpdate().Subscribe(this, (_, self) => self.Tick());
        }

        private void Tick()
        {
            var remain = _endTime - Time.time;

            if (remain <= 0f)
            {
                Finish();
                return;
            }

            _remainSeconds.Value = remain;
            _remainRatio.Value = remain / _duration;
        }

        private void Finish()
        {
            _tick?.Dispose();
            _tick = null;

            _remainSeconds.Value = 0f;
            _remainRatio.Value = 0f;
        }

        public void Dispose()
        {
            _tick?.Dispose();
            _remainSeconds.Dispose();
            _remainRatio.Dispose();
        }
    }
}
