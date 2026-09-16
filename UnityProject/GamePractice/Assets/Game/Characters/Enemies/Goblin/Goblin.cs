using System.Collections.Generic;

namespace Sayne
{
    /// <summary>Goblin 본체. 전투 행동은 GoblinCombat 이 맡는다.</summary>
    public class Goblin : Enemy
    {
        public override string ID => EnemyID.Goblin;

        protected override CharacterCombat CreateCombat(CharacterEquipment equipment,
            IReadOnlyList<BasicAttack> combo, CharacterSkill signature, CharacterSkill ultimate)
        {
            return new GoblinCombat(this, equipment, combo, signature, ultimate);
        }
    }
}
