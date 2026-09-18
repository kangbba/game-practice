namespace Sayne
{
    /// <summary>
    /// 캐릭터 한 명이 싸우는 방식: 스킬. 평타와 궁극기는 낀 무기가 정한다 — 타수·간격·위력·궁극기가 전부 무기에 있다.
    /// 스킬이 없는 캐릭터(적)는 그 자리가 null 이다.
    /// 값의 주인은 설계값 에셋(HeroPlan·EnemyPlan)이고, 이건 거기서 굳어 나온 런타임 형태다.
    /// </summary>
    public class CombatPlan
    {
        /// <summary>평타에 맞은 쪽을 움찔하게 할지. 무기가 아니라 때리는 쪽의 격이 정한다.</summary>
        public bool ComboStaggers { get; }

        public CharacterSkill Skill { get; }

        public CombatPlan(bool comboStaggers, CharacterSkill skill = null)
        {
            ComboStaggers = comboStaggers;
            Skill = skill;
        }
    }
}
