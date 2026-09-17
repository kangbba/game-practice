using System.Collections.Generic;

namespace Sayne
{
    /// <summary>
    /// 캐릭터 한 명이 싸우는 방식 전부: 평타 콤보 + 스킬 + 궁극기.
    /// 스킬·궁극기는 같은 꼴(쿨타임 기술)이고 궁극기가 쿨이 더 길 뿐이다. 없는 캐릭터(적)는 그 자리가 null 이다.
    /// 값의 주인은 설계값 에셋(HeroPlan·EnemyPlan)이고, 이건 거기서 굳어 나온 런타임 형태다.
    /// </summary>
    public class CombatPlan
    {
        public IReadOnlyList<BasicAttack> Combo { get; }
        public CharacterSkill Skill { get; }
        public CharacterSkill Ultimate { get; }

        /// <summary>궁극기 전용 연출. 없으면 평타와 같은 베기 연출을 쓴다.</summary>
        public string UltimateParticleID { get; }

        public CombatPlan(BasicAttack[] combo, CharacterSkill skill = null, CharacterSkill ultimate = null,
            string ultimateParticleID = null)
        {
            Combo = combo;
            Skill = skill;
            Ultimate = ultimate;
            UltimateParticleID = ultimateParticleID;
        }
    }
}
