using System.Collections.Generic;

namespace Sayne
{
    /// <summary>평타 데이터 매니저. 캐릭터별 평타 목록을 들고 있다. 평타는 고유 이름이 없어 순번으로 부른다.</summary>
    public class AttackManager : ManagerBase
    {
        private static readonly Dictionary<string, BasicAttack[]> Combos = new Dictionary<string, BasicAttack[]>
        {
            [HeroID.Kage] = Combo(1f, 1f, 1.2f),
            [HeroID.Aldric] = Combo(1f, 1f, 1.2f),
            [HeroID.Nyx] = Combo(1f, 1f, 1.2f),

            // 적은 1타만 친다.
            // 적의 평타는 히어로를 움찔시키지 않는다. 계속 얻어맞으면 조작이 막히기 때문이다.
            [EnemyID.Goblin] = EnemyHit(1f),
            [EnemyID.Ogre] = EnemyHit(1f),
        };

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
        }

        public IReadOnlyList<BasicAttack> ComboOf(string characterID)
        {
            return Combos[characterID];
        }

        private static BasicAttack[] EnemyHit(float powerMultiplier)
        {
            return new[]
            {
                new BasicAttack("평타", CharacterAnimations.ComboNames[0], powerMultiplier, staggerSeconds: 0f),
            };
        }

        private static BasicAttack[] Combo(params float[] powerMultipliers)
        {
            var combo = new BasicAttack[powerMultipliers.Length];

            for (var i = 0; i < combo.Length; i++)
            {
                // 마지막 타가 조금 더 오래 움찔하게 해서 묶음의 맺음을 준다.
                var stagger = i == powerMultipliers.Length - 1 ? 0.28f : 0.16f;

                combo[i] = new BasicAttack($"평타{i + 1}", CharacterAnimations.ComboNames[i], powerMultipliers[i],
                    staggerSeconds: stagger);
            }

            return combo;
        }
    }
}
