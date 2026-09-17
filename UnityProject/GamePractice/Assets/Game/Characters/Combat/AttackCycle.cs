using System.Collections.Generic;
using UnityEngine;

namespace Sayne
{
    /// <summary>평타 콤보를 순서대로 돌린다. 콤보가 끊기면 1타로 돌아간다.</summary>
    public class AttackCycle
    {
        private const float ComboWindow = 1.2f;

        private readonly IReadOnlyList<BasicAttack> _attacks;

        private int _step;
        private float _expireTime;

        /// <summary>다음 타가 콤보의 마지막인가. 마지막 타 뒤에는 다음 묶음까지 길게 쉰다.</summary>
        public bool IsAtLast => CurrentStep == _attacks.Count - 1;

        public AttackCycle(IReadOnlyList<BasicAttack> combo)
        {
            _attacks = combo;
        }

        /// <summary>다음에 나갈 타격을 꺼내고 한 칸 넘긴다.</summary>
        public BasicAttack Draw()
        {
            var attack = _attacks[CurrentStep];

            _step = (CurrentStep + 1) % _attacks.Count;
            _expireTime = Time.time + ComboWindow;

            return attack;
        }

        private int CurrentStep => Time.time <= _expireTime ? _step : 0;
    }
}
