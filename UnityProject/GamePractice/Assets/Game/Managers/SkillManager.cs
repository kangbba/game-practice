using System.Collections.Generic;

namespace Sayne
{
    /// <summary>고유스킬 매니저. 평타를 마친 뒤 사이클의 마지막으로 나가는 기술을 들고 있다.</summary>
    public class SkillManager : ManagerBase
    {
        private static readonly Dictionary<string, CharacterSkill> Skills = new Dictionary<string, CharacterSkill>
        {
            [HeroID.Kage] = new CharacterSkill("그림자 가르기", CharacterAnimations.SignatureName, powerMultiplier: 1.8f),
            [HeroID.Aldric] = new CharacterSkill("성검 내려베기", CharacterAnimations.SignatureName, powerMultiplier: 2f),
            [HeroID.Nyx] = new CharacterSkill("어둠 갈퀴", CharacterAnimations.SignatureName, powerMultiplier: 1.6f),
        };

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
        }

        /// <summary>고유스킬이 없는 캐릭터면 null — 적이 그렇다.</summary>
        public CharacterSkill Of(string characterID)
        {
            return Skills.TryGetValue(characterID, out var skill) ? skill : null;
        }
    }
}
