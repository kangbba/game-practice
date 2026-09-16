using System.Collections.Generic;

namespace Sayne
{
    /// <summary>Kage 본체. 전투 행동은 KageCombat 이 맡는다.</summary>
    public class Kage : Hero
    {
        public override string ID => HeroID.Kage;

        protected override CharacterCombat CreateCombat(CharacterEquipment equipment,
            IReadOnlyList<BasicAttack> combo, CharacterSkill signature, CharacterSkill ultimate)
        {
            return new KageCombat(this, equipment, combo, signature, ultimate);
        }
    }
}
