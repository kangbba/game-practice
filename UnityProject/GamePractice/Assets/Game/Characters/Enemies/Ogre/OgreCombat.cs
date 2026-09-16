using System.Collections.Generic;

namespace Sayne
{
    /// <summary>덩치 큰 적의 전투. 평타 1타만 친다.</summary>
    public class OgreCombat : CharacterCombat
    {
        public OgreCombat(Character owner, CharacterEquipment equipment, IReadOnlyList<BasicAttack> combo,
            CharacterSkill signature, CharacterSkill ultimate)
            : base(owner, equipment, combo, signature, ultimate)
        {
        }
    }
}
