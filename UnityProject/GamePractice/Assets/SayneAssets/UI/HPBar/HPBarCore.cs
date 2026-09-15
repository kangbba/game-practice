using System;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    public sealed class HPBarCore : IDisposable
    {
        private readonly Image _frontFill;
        private readonly Image _backFill;

        private HPBarStyle _style;
        private IDisposable _subscription;

        private float _currentRatio = 1f;
        private float _chaseRatio = 1f;
        private float _chaseHoldTime;

        public HPBarCore(Image frontFill, Image backFill, HPBarStyle style)
        {
            _frontFill = frontFill;
            _backFill = backFill;
            _style = style;
        }

        public float Ratio => _currentRatio;

        public void SetStyle(HPBarStyle style)
        {
            _style = style;
        }

        public void Bind(ReadOnlyReactiveProperty<float> currentHP, float maxHP)
        {
            Unbind();

            _subscription = currentHP.Subscribe(current => SetHP(current, maxHP));
        }

        private void Unbind()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void SetHP(float current, float max)
        {
            SetRatio(max > 0f ? current / max : 0f);
        }

        public void SetRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);

            if (ratio < _currentRatio) _chaseHoldTime = _style.ChaseDelay;
            else _chaseRatio = ratio;

            _currentRatio = ratio;
            if (_frontFill != null) _frontFill.fillAmount = _currentRatio;
            if (_backFill != null) _backFill.fillAmount = _chaseRatio;
        }

        public void Tick(float deltaTime)
        {
            if (_backFill == null || Mathf.Approximately(_chaseRatio, _currentRatio)) return;

            if (_chaseHoldTime > 0f)
            {
                _chaseHoldTime -= deltaTime;
                return;
            }

            _chaseRatio = Mathf.Lerp(_chaseRatio, _currentRatio, 1f - Mathf.Exp(-_style.ChaseSpeed * deltaTime));
            if (Mathf.Abs(_chaseRatio - _currentRatio) < 0.001f) _chaseRatio = _currentRatio;

            _backFill.fillAmount = _chaseRatio;
        }

        public void Dispose()
        {
            Unbind();
        }
    }
}
