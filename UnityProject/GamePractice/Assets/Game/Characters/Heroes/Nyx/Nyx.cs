using System.Collections.Generic;

namespace Sayne
{
    /// <summary>Nyx 본체. 전투 행동은 NyxCombat 이 맡는다.</summary>
    public class Nyx : Hero
    {
        public override string ID => HeroID.Nyx;

        protected override CharacterCombat CreateCombat(CharacterEquipment equipment,
            IReadOnlyList<BasicAttack> combo, CharacterSkill signature, CharacterSkill ultimate)
        {
            return new NyxCombat(this, equipment, combo, signature, ultimate);
        }
    }
}
