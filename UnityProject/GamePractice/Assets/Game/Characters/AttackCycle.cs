using System.Collections.Generic;
using UnityEngine;

namespace Sayne
{
    /// <summary>평타와 고유스킬을 한 사이클로 담아 순서대로 돌린다. 콤보가 끊기면 1타로 돌아간다.</summary>
    public class AttackCycle
    {
        private const float ComboWindow = 1.2f;

        private readonly BasicAttack[] _attacks;

        private int _step;
        private float _expireTime;

        /// <summary>다음 타가 사이클의 마지막(고유스킬)인가. 자동전투가 궁극기를 끼워 넣는 지점이다.</summary>
        public bool IsAtLast => CurrentStep == _attacks.Length - 1;

        /// <summary>사이클을 마무리하는 타. 고유스킬이 없는 캐릭터면 마지막 평타다.</summary>
        public BasicAttack Last => _attacks[_attacks.Length - 1];

        public AttackCycle(IReadOnlyList<BasicAttack> combo, CharacterSkill signature)
        {
            _attacks = new BasicAttack[combo.Count + (signature != null ? 1 : 0)];

            for (var i = 0; i < combo.Count; i++)
            {
                _attacks[i] = combo[i];
            }

            if (signature != null)
            {
                _attacks[combo.Count] = signature;
            }
        }

        /// <summary>다음에 나갈 타격을 꺼내고 한 칸 넘긴다.</summary>
        public BasicAttack Draw()
        {
            var attack = _attacks[CurrentStep];

            _step = (CurrentStep + 1) % _attacks.Length;
            _expireTime = Time.time + ComboWindow;

            return attack;
        }

        private int CurrentStep => Time.time <= _expireTime ? _step : 0;
    }
}
