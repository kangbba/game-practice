using UnityEngine;

namespace Sayne
{
    public static class CharacterAnimations
    {
        public const int BaseLayer = 0;

        // 모션을 새로 구울 때의 기본 길이. 런타임은 이 값이 아니라 클립의 실제 길이를 읽는다(CharacterMotion.GetClipSeconds).
        public const float SkillDuration = 1.05f;
        public const float UltimateDuration = 7f;
        public const float SkillImpact = .29f;

        /// <summary>
        /// 클립 안 애니메이션 이벤트 이름. 궁극기는 타격 시점을 클립 키프레임에 이벤트로 박아 두고,
        /// 그 이벤트가 이 이름의 메서드(CharacterMotion.OnHitFrame)를 부른다 — 무기가 뻗는 프레임에 맞는다.
        /// </summary>
        public const string HitFrameEvent = "OnHitFrame";

        public const string SkillName = "Skill";

        public const string UltimateName = "Ultimate";

        /// <summary>원거리 계열 궁극기: 겨눈 채 멈춰 선 자세 하나. 클립 길이만큼 버틴다.</summary>
        public const string UltimateRangedName = "UltimateRanged";

        /// <summary>평타 콤보 모션 이름. 순서대로 1 → 4 타이며 각각 독립된 모션이다.</summary>
        public static readonly string[] ComboNames = { "Attack1", "Attack2", "Attack3", "Attack4" };

        /// <summary>그 해시가 콤보 몇 번째 모션인가. 콤보 모션이 아니면 -1.</summary>
        public static int ComboIndexOf(int stateHash)
        {
            for (var i = 0; i < ComboNames.Length; i++)
            {
                if (Animator.StringToHash(ComboNames[i]) == stateHash)
                {
                    return i;
                }
            }

            return -1;
        }

        public static readonly int Idle = Animator.StringToHash("Idle");
        public static readonly int Walk = Animator.StringToHash("Walk");
        public static readonly int Hit = Animator.StringToHash("Hit");
        public static readonly int Death = Animator.StringToHash("Death");
    }
}
