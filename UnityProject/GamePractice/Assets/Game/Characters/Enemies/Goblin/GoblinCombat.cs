using System.Collections.Generic;

namespace Sayne
{
    /// <summary>잡몹의 전투. 평타 1타만 친다.</summary>
    public class GoblinCombat : CharacterCombat
    {
        public GoblinCombat(Character owner, CharacterEquipment equipment, IReadOnlyList<BasicAttack> combo,
            CharacterSkill signature, CharacterSkill ultimate)
            : base(owner, equipment, combo, signature, ultimate)
        {
        }
    }
}
