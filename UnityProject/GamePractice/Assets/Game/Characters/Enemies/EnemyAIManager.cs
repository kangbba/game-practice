using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    public class EnemyAIManager : ManagerBase
    {
        private const float RetargetIntervalMin = 1.5f;
        private const float RetargetIntervalMax = 3f;
        private const float SeparationRadius = 3f;
        private const float SeparationWeight = 2.5f;
        private const float AimError = 1.5f;

        /// <summary>설 자리까지 이 거리 안으로 들면 흩은 조준을 점점 거둔다. 코앞에서 흩으면 도로 몸에 파고든다.</summary>
        private const float ScatterFadeDistance = 6f;

        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly UltimateDirector _ultimateDirector;

        private readonly Dictionary<Enemy, float> _nextThinkTime = new Dictionary<Enemy, float>();
        /// <summary>설 자리에서 조준을 얼마나 흩을지. 생각할 때마다 새로 뽑는다 — 적들이 한 줄로 몰려오지 않게.</summary>
        private readonly Dictionary<Enemy, Vector3> _aimScatter = new Dictionary<Enemy, Vector3>();

        public EnemyAIManager(HeroManager heroManager, EnemyManager enemyManager,
            UltimateDirector ultimateDirector)
        {
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _ultimateDirector = ultimateDirector;
        }

        protected override void OnInit()
        {
            // ② 적도 맞는 순간에 다시 판정한다.
            _enemyManager.Spawned
                .Subscribe(this, (character, self) =>
                {
                    if (character is Enemy enemy)
                    {
                        enemy.Combat.HitMoment
                            .Subscribe((self, enemy), (attack, state) => state.self.ApplyHit(state.enemy, attack))
                            .RegisterTo(enemy.destroyCancellationToken);
                    }
                })
                .RegisterTo(LifeToken);

            // 중단 중에도 Update 는 돈다 — 멈춘 게임에서 적이 판단을 내리면 안 된다.
            Observable.EveryUpdate(UnityFrameProvider.Update)
                .Where(this, (_, self) => !Pause.IsPaused.CurrentValue
                    && !self._ultimateDirector.IsPlaying.CurrentValue)
                .Subscribe(this, (_, self) => self.UpdateAI())
                .RegisterTo(LifeToken);

            // 궁극기 연출이 도는 동안 적은 그 자리에 선다. 판단을 멈춰도 걷던 방향은 남으니 발도 같이 세운다.
            _ultimateDirector.IsPlaying
                .Where(playing => playing)
                .Subscribe(this, (_, self) => self.StopAll())
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            _nextThinkTime.Clear();
            _aimScatter.Clear();
        }

        private void UpdateAI()
        {
            foreach (var enemy in _enemyManager.CurrentEnemies)
            {
                if (ShouldThink(enemy))
                {
                    Think(enemy);
                }

                enemy.Move(DesiredDirection(enemy));
                TryAttack(enemy);
            }

            // 걸음이 다 정해진 뒤 히어로 몸에 파고든 적을 밀어낸다.
            foreach (var hero in _heroManager.CurrentHeroes)
            {
                if (hero.IsAlive)
                {
                    _enemyManager.PushAwayFrom(hero.transform.position);
                }
            }
        }

        private void StopAll()
        {
            foreach (var enemy in _enemyManager.CurrentEnemies)
            {
                enemy.StopMove();
            }
        }

        /// <summary>③ 판정에 걸린 대상에게 피해를 준다. 사이 벌어졌으면 헛친다. 궁극기 연출 중엔 휘두르던 것도 안 맞는다.</summary>
        private void ApplyHit(Enemy enemy, BasicAttack attack)
        {
            if (_ultimateDirector.IsPlaying.CurrentValue)
            {
                return;
            }

            var target = enemy.Target;
            if (target == null || !target.IsAlive)
            {
                return;
            }

            var offset = target.transform.position - enemy.transform.position;
            offset.y = 0f;

            if (enemy.Combat.IsInRange(offset))
            {
                enemy.Combat.Hit(target, attack);
            }
        }

        private static void TryAttack(Enemy enemy)
        {
            var target = enemy.Target;
            if (target == null || !target.IsAlive || !enemy.Combat.CanAttack)
            {
                return;
            }

            var offset = target.transform.position - enemy.transform.position;
            offset.y = 0f;

            if (!enemy.Combat.IsInRange(offset))
            {
                return;
            }

            enemy.Look(offset);
            enemy.Combat.Attack();
        }

        private bool ShouldThink(Enemy enemy)
        {
            if (!_nextThinkTime.TryGetValue(enemy, out var nextTime))
            {
                return true;
            }

            return Time.time >= nextTime;
        }

        private void Think(Enemy enemy)
        {
            _nextThinkTime[enemy] = Time.time + Random.Range(RetargetIntervalMin, RetargetIntervalMax);

            var target = _heroManager.FindNearestAliveHero(enemy.transform.position);
            enemy.SetTarget(target);

            if (target == null)
            {
                _aimScatter.Remove(enemy);
                return;
            }

            var scatter = Random.insideUnitCircle * AimError;
            _aimScatter[enemy] = new Vector3(scatter.x, 0f, scatter.y);
        }


        private Vector3 DesiredDirection(Enemy enemy)
        {
            var separation = Separation(enemy);

            var target = enemy.Target;
            if (target == null || !_aimScatter.TryGetValue(enemy, out var scatter))
            {
                return separation;
            }

            // 멈출지는 실제 타겟까지의 거리로 정하고, 흩어진 조준점은 경로만 흔든다.
            var toTarget = target.transform.position - enemy.transform.position;
            toTarget.y = 0f;

            // 사거리 안이고 좌우로 안 겹쳤으면 멈춘다 — 걸으면서는 못 때린다.
            if (CharacterCombat.IsStandable(toTarget, enemy.Combat.AttackRange))
            {
                return Vector3.zero;
            }

            // 히어로 몸통이 아니라 히어로 옆자리를 노린다. 파고들어 겹치지 않는다.
            var toStand = CharacterCombat.StandPoint(enemy.transform.position, target.transform.position)
                - enemy.transform.position;
            toStand.y = 0f;

            var toAim = toStand + scatter * Mathf.Clamp01(toStand.magnitude / ScatterFadeDistance);

            return toAim.normalized + separation;
        }

        private Vector3 Separation(Enemy enemy)
        {
            var push = Vector3.zero;

            foreach (var other in _enemyManager.CurrentEnemies)
            {
                if (other == enemy)
                {
                    continue;
                }

                var away = enemy.transform.position - other.transform.position;
                away.y = 0f;

                var distance = away.magnitude;
                if (distance >= SeparationRadius || distance <= Mathf.Epsilon)
                {
                    continue;
                }

                push += away.normalized * ((SeparationRadius - distance) / SeparationRadius);
            }

            return push * SeparationWeight;
        }
    }
}
