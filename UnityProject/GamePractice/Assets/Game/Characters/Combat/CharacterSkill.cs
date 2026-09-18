namespace Sayne
{
    /// <summary>
    /// 쿨타임이 붙은 캐릭터 기술. 평타 콤보 밖에서 자기 쿨로 돌고, 평타 진행을 끊고 즉발한다.
    /// 스킬과 궁극기가 같은 꼴이다 — 궁극기가 쿨이 더 길고 셀 뿐이다.
    /// </summary>
    /// <remarks>
    /// 기술은 몇 초짜리인지 스스로 모른다 — 캐릭터마다 모션 길이가 다르고 클립을 고치면 바뀌기 때문이다.
    /// 길이는 쓰는 순간 그 몸의 클립에서 읽고(Character.GetMotionSeconds), 스킬 타격은 그 길이의 ImpactRatio 지점에 나간다.
    /// 궁극기는 한 모션에 여러 번 때리므로 타격 시점을 클립 키프레임의 이벤트(OnHitFrame)가 정한다.
    /// 그래서 HitTime 은 쓰지 않는다.
    /// </remarks>
    public class CharacterSkill : BasicAttack
    {
        public float Cooldown { get; }

        /// <summary>타격 시점이 클립 안 이벤트에 있다. 참이면 ImpactRatio 는 안 쓴다.</summary>
        public bool HitsFromClip { get; }

        /// <summary>모션의 몇 할 지점에서 맞나(0~1). 클립 길이가 바뀌어도 같은 프레임에 맞는다.</summary>
        public float ImpactRatio { get; }

        public CharacterSkill(string name, string animation, float powerMultiplier, float cooldown,
            float staggerSeconds = 0.4f)
            : base(name, animation, powerMultiplier, 0f, staggerSeconds)
        {
            Cooldown = cooldown;
            HitsFromClip = animation == CharacterAnimations.UltimateMeleeName;
            ImpactRatio = CharacterAnimations.SkillImpact;
        }
    }
}
