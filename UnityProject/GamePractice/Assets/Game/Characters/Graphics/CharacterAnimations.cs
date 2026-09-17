using UnityEngine;

namespace Sayne
{
    public static class CharacterAnimations
    {
        public const int BaseLayer = 0;
        public const float ActionPlaybackSpeed = 1.6f;
        public const float SkillDuration = 1.05f;
        public const float UltimateDuration = 1.80f;
        public const float SkillImpact = .29f;
        public const float UltimateImpact = .52f;

        public const string SkillName = "Skill";

        public const string UltimateName = "Ultimate";

        /// <summary>평타 콤보 모션 이름. 순서대로 1 → 4 타이며 각각 독립된 모션이다.</summary>
        public static readonly string[] ComboNames = { "Attack1", "Attack2", "Attack3", "Attack4" };

        public static readonly int Idle = Animator.StringToHash("Idle");
        public static readonly int Walk = Animator.StringToHash("Walk");
        public static readonly int Hit = Animator.StringToHash("Hit");
        public static readonly int Death = Animator.StringToHash("Death");
    }
}
