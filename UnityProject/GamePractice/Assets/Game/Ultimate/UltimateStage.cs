using System.Collections.Generic;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 궁극기 한 판의 무대. 연출기가 주인공과 무대에 올린 적을 모아 든 무기의 궁극기에 넘긴다.
    /// 무대 위 적은 죽음 보류 중이라 HP 가 0 이어도 죽음처리 전이면 서서 맞는다.
    /// </summary>
    public class UltimateStage
    {
        public Hero Hero { get; }
        public IReadOnlyList<Enemy> Targets { get; }

        public UltimateStage(Hero hero, IReadOnlyList<Enemy> targets)
        {
            Hero = hero;
            Targets = targets;
        }

        /// <summary>무대 위 적 전부를 한 대씩 때린다. 모션이 크게 돌아다니므로 때릴 때마다 가장 가까운 적 쪽으로 다시 선다.</summary>
        public void HitAll(BasicAttack attack)
        {
            FaceNearest();

            foreach (var target in Targets)
            {
                Hero.Combat.Hit(target, attack);
            }
        }

        public void Hit(Enemy target, BasicAttack attack)
        {
            Hero.Combat.Hit(target, attack);
        }

        /// <summary>아직 서 있는 적 하나를 아무나 고른다. 다 쓰러졌으면 null.</summary>
        public Enemy PickTarget()
        {
            var standing = new List<Enemy>();

            foreach (var target in Targets)
            {
                if (!target.IsDead)
                {
                    standing.Add(target);
                }
            }

            return standing.Count > 0 ? standing[Random.Range(0, standing.Count)] : null;
        }

        /// <summary>
        /// 무대 위 적 중 가장 가까운 쪽을 바라본다. HP 0 이라도 죽음처리 전이면 서서 맞고 있으니 센다.
        /// 좌우 반전만 바뀌므로 이미 그쪽을 보고 있으면 아무 변화도 없다.
        /// </summary>
        public void FaceNearest()
        {
            Enemy nearest = null;
            var nearestDistance = float.MaxValue;

            foreach (var target in Targets)
            {
                var distance = (target.transform.position - Hero.transform.position).sqrMagnitude;

                if (!target.IsDead && distance < nearestDistance)
                {
                    nearest = target;
                    nearestDistance = distance;
                }
            }

            if (nearest != null)
            {
                Hero.Look(nearest.transform.position - Hero.transform.position);
            }
        }
    }
}
