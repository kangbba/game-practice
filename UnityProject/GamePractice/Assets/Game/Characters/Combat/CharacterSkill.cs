namespace Sayne
{
    /// <summary>
    /// 쿨타임이 붙은 캐릭터 기술. 평타 콤보 밖에서 자기 쿨로 돌고, 평타 진행을 끊고 즉발한다.
    /// 스킬과 궁극기가 같은 꼴이다 — 궁극기가 쿨이 더 길고 셀 뿐이다.
    /// </summary>
    public class CharacterSkill : BasicAttack
    {
        public float Cooldown { get; }

        public float MotionSeconds => (Animation == CharacterAnimations.UltimateName
            ? CharacterAnimations.UltimateDuration : CharacterAnimations.SkillDuration) / CharacterAnimations.ActionPlaybackSpeed;

        public CharacterSkill(string name, string animation, float powerMultiplier, float cooldown,
            float hitTime = -1f, float staggerSeconds = 0.4f)
            : base(name, animation, powerMultiplier, hitTime >= 0f ? hitTime :
                (animation == CharacterAnimations.UltimateName
                    ? CharacterAnimations.UltimateDuration * CharacterAnimations.UltimateImpact
                    : CharacterAnimations.SkillDuration * CharacterAnimations.SkillImpact) / CharacterAnimations.ActionPlaybackSpeed,
                staggerSeconds)
        {
            Cooldown = cooldown;
        }
    }
}
