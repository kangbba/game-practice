using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 캐릭터의 싸움 담당. 무엇으로 어떻게 때리는지를 안다.
    /// 공격 수치는 낀 무기에서 꺼내므로 장비를 들여다본다.
    /// </summary>
    public class CharacterCombat : IDisposable
    {
        private readonly Subject<BasicAttack> _attacked = new Subject<BasicAttack>();
        private readonly Subject<BasicAttack> _hitMoment = new Subject<BasicAttack>();

        private IDisposable _hitTimer;
        private readonly CharacterEquipment _equipment;

        /// <summary>이 전투의 주인. 위치나 상태가 필요한 궁극기 행동이 쓴다.</summary>
        protected Character Owner { get; }

        /// <summary>평타 3타와 고유스킬이 도는 사이클.</summary>
        public AttackCycle Cycle { get; }

        /// <summary>궁극기. 없는 캐릭터면 null — 적이 그렇다.</summary>
        public CharacterSkill Ultimate { get; }

        /// <summary>공격 사이클 쿨타임. 남은 초와 비율을 들고 있어서 UI 는 구독만 하면 된다.</summary>
        public CooldownTimer AttackCooldown { get; } = new CooldownTimer();

        /// <summary>궁극기 쿨타임.</summary>
        public CooldownTimer UltimateCooldown { get; } = new CooldownTimer();

        /// <summary>깊이(Z)를 몇 배로 쳐서 볼지. 클수록 위아래로 떨어진 적이 빨리 사거리 밖이 된다.</summary>
        private const float DepthWeight = 3f;

        public float AttackRange => _equipment.Weapon.Info.Range;

        /// <summary>때릴 거리. 화면 깊이 방향은 좁게 본다 — 위아래로 멀리 있는 적은 안 닿는다.</summary>
        public static float DistanceOf(Vector3 offset)
        {
            return new Vector2(offset.x, offset.z * DepthWeight).magnitude;
        }

        public bool IsInRange(Vector3 offset)
        {
            return DistanceOf(offset) <= AttackRange;
        }

        public bool CanAttack => Owner.CanAct && !Owner.IsMoving && AttackCooldown.IsReady;
        public bool CanUseUltimate => Owner.CanAct && Ultimate != null && UltimateCooldown.IsReady;

        /// <summary>궁극기 전용 연출. 없는 캐릭터면 null.</summary>
        public virtual string UltimateParticleID => null;

        /// <summary>① 휘두르기 시작했다. 모션·연출이 이걸 본다.</summary>
        public Observable<BasicAttack> Attacked => _attacked;

        /// <summary>② 맞는 순간이 됐다. 이때 대상을 다시 찾아 판정한다 — 아무도 없으면 헛친다.</summary>
        public Observable<BasicAttack> HitMoment => _hitMoment;

        public CharacterCombat(Character owner, CharacterEquipment equipment, IReadOnlyList<BasicAttack> combo,
            CharacterSkill signature, CharacterSkill ultimate)
        {
            Owner = owner;
            _equipment = equipment;
            Cycle = new AttackCycle(combo, signature);
            Ultimate = ultimate;
        }

        /// <summary>사이클에서 다음 타를 꺼내 내보내고 피해량을 돌려준다. 맞았는지는 부르는 쪽이 판정한다.</summary>
        /// <summary>사이클에서 다음 타를 꺼내 휘두른다. 맞히는 건 HitMoment 를 받는 쪽이 한다.</summary>
        public void Attack()
        {
            if (!CanAttack)
            {
                return;
            }

            // 묶음의 마지막 타를 내면 다음 묶음까지 길게 쉰다. 그 사이는 짧게 이어 친다.
            var info = _equipment.Weapon.Info;
            var interval = Cycle.IsAtLast ? info.CycleInterval : info.ComboInterval;

            Swing(Cycle.Draw(), interval);
        }

        /// <summary>궁극기를 휘두른다. 쿨이 안 돌았거나 궁극기가 없으면 아무 일도 없다.</summary>
        public void UseUltimate()
        {
            if (!CanUseUltimate)
            {
                return;
            }

            UltimateCooldown.Begin(Ultimate.Cooldown);
            Swing(Ultimate, 0f);

            OnUltimate(DamageOf(Ultimate));
        }

        /// <summary>휘두르기 시작 — 모션을 알리고, 타격 시점에 판정 신호를 낸다.</summary>
        private void Swing(BasicAttack attack, float cooldown)
        {
            if (cooldown > 0f)
            {
                AttackCooldown.Begin(cooldown);
            }

            _attacked.OnNext(attack);

            _hitTimer?.Dispose();
            _hitTimer = Observable.Timer(TimeSpan.FromSeconds(attack.HitTime))
                .Subscribe((self: this, attack), (_, state) => state.self._hitMoment.OnNext(state.attack));
        }

        /// <summary>궁극기가 피해 말고 더 하는 일. 캐릭터별 전투 클래스가 재정의한다.</summary>
        protected virtual void OnUltimate(int damage)
        {
        }

        /// <summary>한 대 먹인다. 피해와 무기 효과가 같이 들어간다.</summary>
        public void Hit(Character target, BasicAttack attack)
        {
            target.TakeDamage(DamageOf(attack));

            if (!target.IsAlive)
            {
                return;
            }

            // 움찔하는 건 어떤 무기를 들든 나가는 기본 반응. 무기는 그 위에 상태이상을 더한다.
            target.Stagger(attack.StaggerSeconds);

            foreach (var effect in _equipment.Weapon.Info.HitEffects)
            {
                target.Debuffs.Apply(effect.Debuff, effect.Seconds);
            }
        }

        /// <summary>실제 피해 = 무기 공격력 × 타격 배수.</summary>
        public int DamageOf(BasicAttack attack)
        {
            return Mathf.RoundToInt(_equipment.Weapon.Info.Power * attack.PowerMultiplier);
        }

        public void Dispose()
        {
            _hitTimer?.Dispose();
            _hitMoment.Dispose();
            _attacked.Dispose();
            AttackCooldown.Dispose();
            UltimateCooldown.Dispose();
        }
    }
}
