using System.Collections.Generic;

namespace Sayne
{
    /// <summary>궁극기 매니저. 사이클 밖에서 자기 쿨타임으로 도는 기술을 들고 있다.</summary>
    public class UltimateManager : ManagerBase
    {
        private static readonly Dictionary<string, CharacterSkill> Ultimates = new Dictionary<string, CharacterSkill>
        {
            [HeroID.Kage] = new CharacterSkill("그림자 참", CharacterAnimations.UltimateName, powerMultiplier: 3f, cooldown: 10f),
            [HeroID.Aldric] = new CharacterSkill("성검 강타", CharacterAnimations.UltimateName, powerMultiplier: 3.5f, cooldown: 12f),
            [HeroID.Nyx] = new CharacterSkill("어둠 폭발", CharacterAnimations.UltimateName, powerMultiplier: 3f, cooldown: 9f),
        };

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
        }

        /// <summary>궁극기가 없는 캐릭터면 null — 적이 그렇다.</summary>
        public CharacterSkill Of(string characterID)
        {
            return Ultimates.TryGetValue(characterID, out var ultimate) ? ultimate : null;
        }
    }
}
