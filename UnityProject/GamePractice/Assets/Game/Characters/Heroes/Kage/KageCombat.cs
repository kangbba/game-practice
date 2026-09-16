using System.Collections.Generic;

namespace Sayne
{
    /// <summary>그림자 검사의 전투. 고유 행동이 생기면 OnUltimate 을 재정의한다.</summary>
    public class KageCombat : CharacterCombat
    {
        public KageCombat(Character owner, CharacterEquipment equipment, IReadOnlyList<BasicAttack> combo,
            CharacterSkill signature, CharacterSkill ultimate)
            : base(owner, equipment, combo, signature, ultimate)
        {
        }
    }
}
