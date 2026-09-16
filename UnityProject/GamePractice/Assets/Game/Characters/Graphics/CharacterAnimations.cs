using UnityEngine;

namespace Sayne
{
    public static class CharacterAnimations
    {
        public const int BaseLayer = 0;

        public const string SignatureName = "Skill";

        public const string UltimateName = "Ultimate";

        /// <summary>평타 콤보 모션 이름. 순서대로 1 → 2 → 3 타다.</summary>
        public static readonly string[] ComboNames = { "Attack1", "Attack2", "Attack3" };

        public static readonly int Idle = Animator.StringToHash("Idle");
        public static readonly int Walk = Animator.StringToHash("Walk");
        public static readonly int Hit = Animator.StringToHash("Hit");
        public static readonly int Death = Animator.StringToHash("Death");
    }
}
