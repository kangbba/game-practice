using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace Sayne
{
    /// <summary>
    /// 근접 무기 — 검·낫·지팡이·몽둥이. 궁극기는 무기가 거대해지는 안무를 한 번 추고,
    /// 클립이 알리는 타격마다 무대 위 적 전부를 벤다. 지금은 모션이 하나뿐이라 근접 무기가 모두 이걸 같이 쓴다 —
    /// 모양은 든 무기 그림이 알아서 달라진다. 제 궁극기가 필요한 무기는 이걸 상속해 PlayUltimateAsync 를 덮는다.
    /// </summary>
    public class MeleeWeapon : Weapon
    {
        protected override string UltimateAnimation => CharacterAnimations.UltimateName;

        public override async UniTask PlayUltimateAsync(UltimateStage stage, CancellationToken token)
        {
            var combat = stage.Hero.Combat;

            using (combat.HitMoment
                       .Where(combat, (attack, owner) => attack == owner.Ultimate)
                       .Subscribe(stage, (attack, owner) => owner.HitAll(attack)))
            {
                await combat.PerformUltimateAsync(token);
            }
        }
    }
}
