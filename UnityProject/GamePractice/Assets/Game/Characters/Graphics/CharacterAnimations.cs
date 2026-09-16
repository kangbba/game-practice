using UnityEngine;

namespace Sayne
{
    public static class CharacterAnimations
    {
        public static readonly int Idle = Animator.StringToHash("Idle");
        public static readonly int Walk = Animator.StringToHash("Walk");
        public static readonly int Attack = Animator.StringToHash("Attack");
        public static readonly int Hit = Animator.StringToHash("Hit");
        public static readonly int Death = Animator.StringToHash("Death");
        public static readonly int Empty = Animator.StringToHash("Empty");

        public const int BaseLayer = 0;
        public const int UpperBodyLayer = 1;

        public static class Hero
        {
            public static readonly int Ultimate = Animator.StringToHash("Ultimate");
        }
    }
}
