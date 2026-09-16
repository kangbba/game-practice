using System.Collections.Generic;

namespace Sayne
{
    /// <summary>Ogre 본체. 전투 행동은 OgreCombat 이 맡는다.</summary>
    public class Ogre : Enemy
    {
        public override string ID => EnemyID.Ogre;

        protected override CharacterCombat CreateCombat(CharacterEquipment equipment,
            IReadOnlyList<BasicAttack> combo, CharacterSkill signature, CharacterSkill ultimate)
        {
            return new OgreCombat(this, equipment, combo, signature, ultimate);
        }
    }
}
