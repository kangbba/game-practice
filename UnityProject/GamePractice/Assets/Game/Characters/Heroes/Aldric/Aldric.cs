using System.Collections.Generic;

namespace Sayne
{
    /// <summary>Aldric 본체. 전투 행동은 AldricCombat 이 맡는다.</summary>
    public class Aldric : Hero
    {
        public override string ID => HeroID.Aldric;

        protected override CharacterCombat CreateCombat(CharacterEquipment equipment,
            IReadOnlyList<BasicAttack> combo, CharacterSkill signature, CharacterSkill ultimate)
        {
            return new AldricCombat(this, equipment, combo, signature, ultimate);
        }
    }
}
