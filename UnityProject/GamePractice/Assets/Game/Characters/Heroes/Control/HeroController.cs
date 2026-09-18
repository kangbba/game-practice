using Cysharp.Threading.Tasks;
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

        /// <summary>
        /// 상대로 삼는 거리는 사거리에서 이만큼 여유를 둔다. 딱 사거리로 자르면 경계에 선 적을
        /// 잡았다 놓았다 하며 깜빡인다.
        /// </summary>
        private const float AcquireRangeFactor = 1.1f;

        /// <summary>타격이 퍼지는 반경. 타겟 주위에 있는 적도 같이 맞는다.</summary>
        private const float SplashRadius = 1.2f;

        // 근접 스킬이 휩쓰는 범위. 스킬 모션은 무기를 키워 반경 7 로 크게 휘두르므로 판정도 그 끝까지 닿게 잡는다.
        // 몸 앞쪽으로 칼끝 너머까지, 머리 위로 넘어가는 호가 있어 등 뒤로도 조금, 화면 깊이로는 위아래로 넉넉히.
        private const float SkillFrontReach = 7.5f;
        private const float SkillBackReach = 2f;
        private const float SkillDepthReach = 2.5f;

        // 원거리 스킬: 평타 투사체를 두 배 크기로 키운 한 발. 평타 한 발의 세 배로 때리고, 터지는 반경도 두 배다.
        private const float SkillShotScale = 2f;
        private const float SkillShotPower = 3f;

        /// <summary>투사체가 떠나는 높이. 발밑이 아니라 가슴께에서 나간다.</summary>
        private const float MuzzleHeight = 0.9f;

        private readonly EnemyManager _enemyManager;
        private readonly UltimateDirector _ultimateDirector;
        private readonly IMoveInputSource _moveSource;

        public Hero Hero { get; }

        public HeroController(Hero hero, EnemyManager enemyManager, UltimateDirector ultimateDirector,
            IMoveInputSource moveSource)
        {
            Hero = hero;
            _enemyManager = enemyManager;
            _ultimateDirector = ultimateDirector;
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
        /// 궁극기는 여기서 안 때린다 — UltimateDirector 가 무대에 올린 적 전부에게 준다.
        /// </summary>
        private void ApplyHit(BasicAttack attack)
        {
            if (attack == Hero.Combat.Ultimate)
            {
                return;
            }

            if (attack == Hero.Combat.Skill)
            {
                if (Hero.Combat.Weapon.IsRanged)
                {
                    FireSkillShot(attack);
                    return;
                }

                Sweep(attack);
                return;
            }

            var target = Hero.Target as Enemy;
            if (target == null || !target.IsAlive || !Hero.Combat.IsInRange(ToTarget(target)))
            {
                return;
            }

            var weapon = Hero.Combat.Weapon;
            if (weapon.IsRanged)
            {
                Launch(weapon.Projectile, target, attack, 1f);
                return;
            }

            // 맞는 순간 타겟을 히어로와 같은 줄(Z)에 세운다. 사거리 판정이 Z 를 빡세게 보니까
            // 조금 어긋난 채로 맞으면 허공을 치는 그림이 된다. 어긋난 양은 어차피 Z 허용폭 안이라 순간이동은 안 보인다.
            // 스플래시로 같이 맞는 주변 적은 안 건드린다 — 한 줄로 뭉쳐버린다.
            _enemyManager.AlignDepth(target, Hero.transform.position.z);
            Splash(target, attack, SplashRadius);
        }

        /// <summary>
        /// 가슴께에서 투사체를 쏜다. 날아가는 동안 타겟이 죽었으면 도착해도 때릴 게 없어 헛맞는다.
        /// 도착할 때까지 산 놈들 중 터진 자리 근처에 있는 놈이 맞는다 — 근접의 스플래시와 같은 규칙이다.
        /// </summary>
        /// <param name="scale">투사체 크기 배수. 터지는 반경도 같은 배수로 넓어진다.</param>
        private void Launch(Projectile prefab, Enemy target, BasicAttack attack, float scale)
        {
            var muzzle = Hero.transform.position + new Vector3(0f, MuzzleHeight, 0f);

            var projectile = Object.Instantiate(prefab);
            projectile.SetScale(scale);
            projectile.Launch(muzzle, target, () => Splash(target, attack, SplashRadius * scale));
        }

        /// <summary>
        /// 원거리 스킬: 두 배 크기의 왕 투사체 한 발. 지금 싸우는 상대가 없으면 사거리 안의 가장 가까운 적을 노리고,
        /// 그마저 없으면 쏘지 않는다.
        /// </summary>
        private void FireSkillShot(BasicAttack skill)
        {
            var target = Hero.Target as Enemy;
            if (target == null || !target.IsAlive)
            {
                target = _enemyManager.FindNearestEnemy(Hero.transform.position, Hero.Combat.AttackRange);
            }

            if (target == null)
            {
                return;
            }

            var shot = new BasicAttack(skill.Name, skill.Animation, SkillShotPower, staggerSeconds: skill.StaggerSeconds);
            Launch(Hero.Combat.Weapon.Projectile, target, shot, SkillShotScale);
        }

        /// <summary>
        /// 스킬은 모션이 앞을 크게 휩쓴다 — 타겟이 사거리에 있든 없든 몸 앞쪽 넓은 범위의 산 적을 전부 때린다.
        /// </summary>
        private void Sweep(BasicAttack attack)
        {
            var enemies = _enemyManager.FindAliveEnemiesInFront(Hero.transform.position, Hero.IsFacingRight,
                SkillBackReach, SkillFrontReach, SkillDepthReach);

            foreach (var enemy in enemies)
            {
                Hero.Combat.Hit(enemy, attack);
            }
        }

        /// <summary>터진 자리 근처의 산 적을 전부 때린다.</summary>
        private void Splash(Enemy center, BasicAttack attack, float radius)
        {
            // 투사체가 날아가는 사이에 터진 자리의 주인이 죽어 사라졌을 수 있다. 그러면 허공에서 터진다.
            if (center == null)
            {
                return;
            }

            foreach (var enemy in _enemyManager.FindAliveEnemiesInReach(center.transform.position, radius))
            {
                Hero.Combat.Hit(enemy, attack);
            }
        }

        public void UpdateControl()
        {
            if (Hero == null || !Hero.IsAlive)
            {
                return;
            }

            // 수동 이동이 들어오면 교전을 끊는다. 스킬을 쓰는 중이었으면 스킬도 끊고 걷는다.
            // 손을 떼면 타겟 찾기부터 다시 시작한다.
            if (_moveSource.IsActive)
            {
                Hero.Combat.CancelCast();
                Hero.SetTarget(null);
                Hero.Move(_moveSource.Direction);
                return;
            }

            // 스킬 모션이 도는 동안 자동 전투는 끼어들지 않는다 — 타겟 변경·자동 이동·평타 전부 모션이 끝난 뒤다.
            if (Hero.Combat.IsCasting)
            {
                return;
            }

            // ⓪ 싸울 상대를 정한다. 죽었거나 사라졌으면 다시 고른다.
            AcquireTarget();

            AutoMove();
            TryAutoCast();
            TryAutoAttack();
        }

        /// <summary>
        /// 싸울 상대는 사거리 안에서만 고른다. 지금 상대가 아직 사거리 안이면 그놈을 계속 친다 —
        /// 매 프레임 가까운 놈으로 갈아타면 콤보가 끊기고 몸이 두리번거린다.
        /// </summary>
        private void AcquireTarget()
        {
            if (Hero.Target is Enemy current && current != null && current.IsAlive && IsInAcquireRange(current))
            {
                return;
            }

            Hero.SetTarget(_enemyManager.FindNearestEnemy(Hero.transform.position, AcquireRange));
        }

        /// <summary>
        /// 걷는 건 사거리와 상관없이 언제나 제일 가까운 적 쪽이다. 사거리 안에 들고 좌우로 안 겹치면 멈춘다 —
        /// 걸으면서는 못 때리기 때문이다. 타겟은 멈춘 뒤 AcquireTarget 이 알아서 잡는다.
        /// </summary>
        private void AutoMove()
        {
            var nearest = _enemyManager.FindNearestEnemy(Hero.transform.position, float.MaxValue);

            if (nearest == null)
            {
                Hero.StopMove();
                return;
            }

            if (CharacterCombat.IsStandable(ToTarget(nearest), Hero.Combat.AttackRange * StopDistanceFactor))
            {
                Hero.StopMove();
                return;
            }

            // 적 몸통이 아니라 적 옆자리로 걷는다. 너무 붙어 있으면 이게 곧 옆으로 물러서는 걸음이 된다.
            var toStand = CharacterCombat.StandPoint(Hero.transform.position, nearest.transform.position)
                - Hero.transform.position;
            toStand.y = 0f;

            Hero.Move(toStand.normalized);
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

            // 쓸 수 있으면 멈춰 서고 쿨을 돌린 뒤 연출에 넘긴다. 쿨·잠금이 안 풀렸거나 무대에 오를 적이 없으면 아무 일도 없다.
            if (!_ultimateDirector.CanPlay(Hero) || !Hero.Combat.TryStartUltimate())
            {
                return;
            }

            _ultimateDirector.PlayUltimateSequenceAsync(Hero).Forget();
        }

        /// <summary>자동전투의 스킬·궁극기. 사거리에 타겟이 있고 쿨이 돌아왔으면 그 자리에서 쓴다. 궁극기가 먼저다.</summary>
        private void TryAutoCast()
        {
            if (Hero.Target is not Enemy target || !Hero.Combat.IsInRange(ToTarget(target)))
            {
                return;
            }

            var canPlayUltimate = _ultimateDirector.CanPlay(Hero);

            if (!canPlayUltimate && !Hero.Combat.CanUseSkill)
            {
                return;
            }

            Hero.Look(ToTarget(target));

            if (canPlayUltimate)
            {
                UseUltimate();
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

        private float AcquireRange => Hero.Combat.AttackRange * AcquireRangeFactor;

        private bool IsInAcquireRange(Enemy enemy)
        {
            return CharacterCombat.DistanceOf(ToTarget(enemy)) <= AcquireRange;
        }

        private Vector3 ToTarget(Enemy enemy)
        {
            var offset = enemy.transform.position - Hero.transform.position;
            offset.y = 0f;
            return offset;
        }
    }
}
