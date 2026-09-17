using System;
using R3;
using TMPro;
using UnityEngine;

namespace Sayne
{
    public sealed class HPBarCore : IDisposable
    {
        private readonly SlicedFillBar _frontFill;
        private readonly SlicedFillBar _backFill;

        /// <summary>절대수치 라벨. 없는 바(라벨 미배선)면 비율만 그린다.</summary>
        private readonly TMP_Text _label;

        private HPBarStyle _style;
        private IDisposable _subscription;

        private float _currentRatio = 1f;
        private float _chaseRatio = 1f;
        private float _chaseHoldTime;

        public HPBarCore(SlicedFillBar frontFill, SlicedFillBar backFill, TMP_Text label, HPBarStyle style)
        {
            _frontFill = frontFill;
            _backFill = backFill;
            _label = label;
            _style = style;
        }

        public float Ratio => _currentRatio;

        public void SetStyle(HPBarStyle style)
        {
            _style = style;
        }

        /// <summary>최대치도 구독한다 — 성장으로 MaxHP 가 변하면 비율과 라벨이 같이 따라온다.</summary>
        public void Bind(ReadOnlyReactiveProperty<float> currentHP, ReadOnlyReactiveProperty<float> maxHP)
        {
            Unbind();

            _subscription = currentHP
                .CombineLatest(maxHP, (current, max) => (current, max))
                .Subscribe(this, (hp, self) => self.SetHP(hp.current, hp.max));
        }

        private void Unbind()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void SetHP(float current, float max)
        {
            SetRatio(max > 0f ? current / max : 0f);

            if (_label != null)
            {
                _label.text = $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
            }
        }

        public void SetRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);

            if (ratio < _currentRatio) _chaseHoldTime = _style.ChaseDelay;
            else _chaseRatio = ratio;

            _currentRatio = ratio;
            if (_frontFill != null) _frontFill.FillAmount = _currentRatio;
            if (_backFill != null) _backFill.FillAmount = _chaseRatio;
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

            _backFill.FillAmount = _chaseRatio;
        }

        public void Dispose()
        {
            Unbind();
        }
    }
}
