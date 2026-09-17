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
        private readonly Dictionary<DebuffType, ReactiveProperty<bool>> _effects =
            new Dictionary<DebuffType, ReactiveProperty<bool>>();

        private readonly Dictionary<DebuffType, IDisposable> _timers =
            new Dictionary<DebuffType, IDisposable>();

        public CharacterDebuffs()
        {
            foreach (var effect in DebuffTypes.All)
            {
                _effects[effect] = new ReactiveProperty<bool>();
            }
        }

        public ReadOnlyReactiveProperty<bool> Observe(DebuffType effect) => _effects[effect];

        public bool Has(DebuffType effect) => _effects[effect].Value;

        /// <summary>
        /// 상태이상 한 건을 건다. 같은 종류를 다시 걸면 지속 시간이 새로 시작된다.
        /// 지속 시간이 0 이하면 타이머를 안 건다 — Clear 로 벗기기 전까지 계속 걸려 있다.
        /// </summary>
        public void Apply(Debuff debuff)
        {
            var type = debuff.Type;
            Stop(type);

            _effects[type].Value = true;

            if (debuff.Seconds <= 0f)
            {
                return;
            }

            _timers[type] = Observable.Timer(TimeSpan.FromSeconds(debuff.Seconds))
                .Subscribe((self: this, type), (_, state) => state.self.Clear(state.type));
        }

        public void Clear(DebuffType effect)
        {
            Stop(effect);
            _effects[effect].Value = false;
        }

        public void ClearAll()
        {
            foreach (var effect in DebuffTypes.All)
            {
                Clear(effect);
            }
        }

        private void Stop(DebuffType effect)
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
