using System;
using System.Collections.Generic;
using R3;

namespace Sayne
{
    /// <summary>
    /// 캐릭터에 걸린 상태이상의 주인. 종류별로 걸렸는지를 리액티브로 노출한다.
    /// 경직처럼 무기가 걸어주는 것들이 여기 쌓인다.
    /// </summary>
    public class CharacterDebuffs : IDisposable
    {
        private readonly Dictionary<Debuff, ReactiveProperty<bool>> _effects =
            new Dictionary<Debuff, ReactiveProperty<bool>>();

        private readonly Dictionary<Debuff, IDisposable> _timers =
            new Dictionary<Debuff, IDisposable>();

        public CharacterDebuffs()
        {
            foreach (var effect in Debuffs.All)
            {
                _effects[effect] = new ReactiveProperty<bool>();
            }
        }

        public ReadOnlyReactiveProperty<bool> Observe(Debuff effect) => _effects[effect];

        public bool Has(Debuff effect) => _effects[effect].Value;

        /// <summary>같은 상태이상을 다시 걸면 지속 시간이 새로 시작된다.</summary>
        public void Apply(Debuff effect, float duration)
        {
            Stop(effect);

            _effects[effect].Value = true;
            _timers[effect] = Observable.Timer(TimeSpan.FromSeconds(duration))
                .Subscribe((self: this, effect), (_, state) => state.self.Clear(state.effect));
        }

        public void Clear(Debuff effect)
        {
            Stop(effect);
            _effects[effect].Value = false;
        }

        public void ClearAll()
        {
            foreach (var effect in Debuffs.All)
            {
                Clear(effect);
            }
        }

        private void Stop(Debuff effect)
        {
            if (_timers.TryGetValue(effect, out var timer))
            {
                timer.Dispose();
                _timers.Remove(effect);
            }
        }

        public void Dispose()
        {
            foreach (var timer in _timers.Values)
            {
                timer.Dispose();
            }

            _timers.Clear();

            foreach (var property in _effects.Values)
            {
                property.Dispose();
            }
        }
    }
}
