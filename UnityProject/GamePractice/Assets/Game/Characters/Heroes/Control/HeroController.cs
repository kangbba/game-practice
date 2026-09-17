using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 히어로 반자동 컨트롤러. 입력원이 활성이면 수동 이동, 아니면 자동사냥 이동.
    /// 평타는 항상 사거리 내 자동 4콤보. 순수 자동 = 입력원이 조용한 상태일 뿐이라 모드 전환 개념이 없다.
    /// 자동일 때 스킬·궁극기는 쿨이 돌아오면 그 자리에서 나간다 — 평타 콤보 도중이어도 끊고 즉발. 버튼을 누른 것과 같다.
    /// </summary>
    public class HeroController
    {
        private const float StopDistanceFactor = 0.8f;

        /// <summary>타격이 퍼지는 반경. 타겟 주위에 있는 적도 같이 맞는다.</summary>
        private const float SplashRadius = 1.2f;

        /// <summary>투사체가 떠나는 높이. 발밑이 아니라 가슴께에서 나간다.</summary>
        private const float MuzzleHeight = 0.9f;

        private readonly EnemyManager _enemyManager;
        private readonly IMoveInputSource _moveSource;

        public Hero Hero { get; }

        public HeroController(Hero hero, EnemyManager enemyManager, IMoveInputSource moveSource)
        {
            Hero = hero;
            _enemyManager = enemyManager;
            _moveSource = moveSource;

            // ② 맞는 순간에 대상을 다시 찾는다 — 그 사이 도망갔거나 죽었으면 헛친다.
            hero.Combat.HitMoment
                .Subscribe(this, (attack, self) => self.ApplyHit(attack))
                .RegisterTo(hero.destroyCancellationToken);
        }

        /// <summary>
        /// ③ 타겟이 아직 사거리에 있으면 때린다. 벗어났으면 헛친다.
        /// 근접이면 여기서 바로 판정하고, 원거리면 투사체를 쏘고 판정을 도착 시점으로 미룬다 —
        /// 미루는 것뿐이라 판정 규칙은 근접과 한 글자도 다르지 않다.
        /// </summary>
        private void ApplyHit(BasicAttack attack)
        {
            var target = Hero.Target as Enemy;
            if (target == null || !target.IsAlive || !Hero.Combat.IsInRange(ToTarget(target)))
            {
                return;
            }

            var weapon = Hero.Combat.Weapon;
            if (weapon.IsRanged)
            {
                Launch(weapon.Projectile, target, attack);
                return;
            }

            AlignDepth(target);
            Splash(target, attack);
        }

        /// <summary>
        /// 가슴께에서 투사체를 쏜다. 날아가는 동안 타겟이 죽었으면 도착해도 때릴 게 없어 헛맞는다.
        /// 도착할 때까지 산 놈들 중 터진 자리 근처에 있는 놈이 맞는다 — 근접의 스플래시와 같은 규칙이다.
        /// </summary>
        private void Launch(Projectile prefab, Enemy target, BasicAttack attack)
        {
            var muzzle = Hero.transform.position + new Vector3(0f, MuzzleHeight, 0f);

            Object.Instantiate(prefab)
                .Launch(muzzle, target.transform, () => Splash(target, attack));
        }

        /// <summary>터진 자리 근처의 산 적을 전부 때린다.</summary>
        private void Splash(Enemy center, BasicAttack attack)
        {
            // 투사체가 날아가는 사이에 터진 자리의 주인이 죽어 사라졌을 수 있다. 그러면 허공에서 터진다.
            if (center == null)
            {
                return;
            }

            // 맞고 죽은 적은 그 자리에서 목록을 빠져나간다. 뒤에서부터 훑어야 순번이 밀리지 않는다.
            var enemies = _enemyManager.CurrentEnemies;
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (SplashDistance(center, enemy) <= SplashRadius)
                {
                    Hero.Combat.Hit(enemy, attack);
                }
            }
        }

        /// <summary>
        /// 맞는 순간 타겟을 히어로와 같은 줄(Z)에 세운다. 사거리 판정이 Z 를 빡세게 보니까
        /// 조금 어긋난 채로 맞으면 허공을 치는 그림이 된다 — 때리는 순간 줄을 맞춰준다.
        /// 어긋난 양은 어차피 Z 허용폭 안이라 눈에 띄는 순간이동은 안 생긴다.
        /// 스플래시로 같이 맞는 주변 적은 안 건드린다. 한 줄로 뭉쳐버린다.
        /// </summary>
        private void AlignDepth(Enemy target)
        {
            var position = target.transform.position;
            position.z = Hero.transform.position.z;
            target.transform.position = position;
        }

        /// <summary>타겟에서 얼마나 떨어져 있나. 깊이는 사거리와 같은 기준으로 좁게 본다.</summary>
        private static float SplashDistance(Enemy target, Enemy other)
        {
            var offset = other.transform.position - target.transform.position;
            offset.y = 0f;

            return CharacterCombat.DistanceOf(offset);
        }

        public void UpdateControl()
        {
            if (Hero == null || !Hero.IsAlive)
            {
                return;
            }

            // 기술을 쓰는 동안은 어떤 커맨드도 받지 않는다 — 이동·타겟 변경·평타 전부 모션이 끝난 뒤다.
            if (Hero.Combat.IsCasting)
            {
                return;
            }

            // 수동 이동이 들어오면 교전을 끊는다. 손을 떼면 타겟 찾기부터 다시 시작한다.
            if (_moveSource.IsActive)
            {
                Hero.SetTarget(null);
                Hero.Move(_moveSource.Direction);
                return;
            }

            // ⓪ 싸울 상대를 정한다. 죽었거나 사라졌으면 다시 고른다.
            AcquireTarget();

            AutoMove();
            TryAutoCast();
            TryAutoAttack();
        }

        /// <summary>타겟이 없거나 유실됐으면 가장 가까운 적을 새로 잡는다. 살아 있으면 그놈을 계속 쫓는다.</summary>
        private void AcquireTarget()
        {
            if (Hero.Target is Enemy current && current != null && current.IsAlive)
            {
                return;
            }

            Hero.SetTarget(FindNearestEnemy(float.MaxValue));
        }

        private void AutoMove()
        {
            if (Hero.Target is not Enemy target)
            {
                Hero.StopMove();
                return;
            }

            var offset = ToTarget(target);
            if (CharacterCombat.DistanceOf(offset) <= Hero.Combat.AttackRange * StopDistanceFactor)
            {
                Hero.StopMove();
                return;
            }

            Hero.Move(offset.normalized);
        }

        /// <summary>스킬 버튼이 부르는 곳. 자동전투는 쿨이 돌아오면 같은 함수를 대신 불러준다.</summary>
        public void UseSkill()
        {
            if (Hero == null)
            {
                return;
            }

            Hero.Combat.UseSkill();
        }

        /// <summary>궁극기 버튼이 부르는 곳. 자동전투는 쿨이 돌아오면 같은 함수를 대신 불러준다.</summary>
        public void UseUltimate()
        {
            if (Hero == null)
            {
                return;
            }

            Hero.Combat.UseUltimate();
        }

        /// <summary>자동전투의 스킬·궁극기. 사거리에 타겟이 있고 쿨이 돌아왔으면 그 자리에서 쓴다. 궁극기가 먼저다.</summary>
        private void TryAutoCast()
        {
            if (Hero.Target is not Enemy target || !Hero.Combat.IsInRange(ToTarget(target)))
            {
                return;
            }

            if (!Hero.Combat.CanUseUltimate && !Hero.Combat.CanUseSkill)
            {
                return;
            }

            Hero.Look(ToTarget(target));

            if (Hero.Combat.CanUseUltimate)
            {
                Hero.Combat.UseUltimate();
            }
            else
            {
                Hero.Combat.UseSkill();
            }
        }

        private void TryAutoAttack()
        {
            if (!Hero.Combat.CanAttack)
            {
                return;
            }

            // ① 타겟이 사거리에 없으면 휘두르지 않는다.
            if (Hero.Target is not Enemy target || !Hero.Combat.IsInRange(ToTarget(target)))
            {
                return;
            }

            Hero.Look(ToTarget(target));
            Hero.Combat.Attack();
        }

        private Enemy FindNearestEnemy(float maxDistance)
        {
            Enemy nearest = null;
            var nearestDistance = maxDistance;

            foreach (var enemy in _enemyManager.CurrentEnemies)
            {
                var distance = CharacterCombat.DistanceOf(ToTarget(enemy));
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private Vector3 ToTarget(Enemy enemy)
        {
            var offset = enemy.transform.position - Hero.transform.position;
            offset.y = 0f;
            return offset;
        }
    }
}
