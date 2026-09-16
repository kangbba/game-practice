using System.Collections.Generic;

namespace Sayne
{
    /// <summary>어둠 마법사의 전투. 궁극기에 전용 연출이 붙는다.</summary>
    public class NyxCombat : CharacterCombat
    {
        public NyxCombat(Character owner, CharacterEquipment equipment, IReadOnlyList<BasicAttack> combo,
            CharacterSkill signature, CharacterSkill ultimate)
            : base(owner, equipment, combo, signature, ultimate)
        {
        }

        public override string UltimateParticleID => ParticleID.Hero.NyxUltimate;
    }
}
