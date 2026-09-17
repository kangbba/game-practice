using System;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 캐릭터의 싸움 담당. 무엇으로 어떻게 때리는지를 안다.
    /// 싸우는 방식은 CombatPlan 선언에서 오고, 공격 수치는 낀 무기에서 꺼낸다.
    /// </summary>
    public class CharacterCombat : IDisposable
    {
        private readonly Subject<BasicAttack> _attacked = new Subject<BasicAttack>();
        private readonly Subject<BasicAttack> _hitMoment = new Subject<BasicAttack>();

        private readonly ReactiveProperty<bool> _isCasting = new ReactiveProperty<bool>();

        private IDisposable _hitTimer;
        private IDisposable _castTimer;

        /// <summary>기술 모션이 아직 끝나지 않았다.</summary>
        private bool _castMotionRunning;

        /// <summary>기술 타격이 아직 안 나갔다. 모션보다 늦게 오는 타격이 있어도 잠금이 먼저 풀리지 않는다.</summary>
        private bool _castHitPending;
        private readonly Character _owner;
        private readonly CharacterEquipment _equipment;

        /// <summary>평타 4콤보가 도는 사이클.</summary>
        public AttackCycle Cycle { get; }

        /// <summary>스킬. 궁극기와 같은 꼴이고 쿨이 짧다. 없는 캐릭터면 null — 적이 그렇다.</summary>
        public CharacterSkill Skill { get; }

        /// <summary>궁극기. 없는 캐릭터면 null — 적이 그렇다.</summary>
        public CharacterSkill Ultimate { get; }

        /// <summary>궁극기 전용 연출. 없는 캐릭터면 null.</summary>
        public string UltimateParticleID { get; }

        /// <summary>평타 쿨타임. 남은 초와 비율을 들고 있어서 UI 는 구독만 하면 된다.</summary>
        public CooldownTimer AttackCooldown { get; } = new CooldownTimer();

        /// <summary>스킬 쿨타임.</summary>
        public CooldownTimer SkillCooldown { get; } = new CooldownTimer();

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

        /// <summary>
        /// 기술을 쓰는 중인가. 모션이 안 끝났거나 타격이 아직 안 나갔으면 참이다.
        /// 이 동안은 평타·다른 기술·이동 어느 커맨드도 받지 않는다 — 시간 비교가 아니라 상태로 잠근다.
        /// </summary>
        public bool IsCasting => _isCasting.Value;

        /// <summary>같은 잠금을 UI 가 구독하는 통로. 캐스팅 중엔 발동 버튼이 꺼져 있어야 한다.</summary>
        public ReadOnlyReactiveProperty<bool> Casting => _isCasting;

        public bool CanAttack => !IsCasting && _owner.CanAct && !_owner.IsMoving && AttackCooldown.IsReady;
        public bool CanUseSkill => CanCast(Skill, SkillCooldown);
        public bool CanUseUltimate => CanCast(Ultimate, UltimateCooldown);

        /// <summary>① 휘두르기 시작했다. 모션·연출이 이걸 본다.</summary>
        public Observable<BasicAttack> Attacked => _attacked;

        /// <summary>② 맞는 순간이 됐다. 이때 대상을 다시 찾아 판정한다 — 아무도 없으면 헛친다.</summary>
        public Observable<BasicAttack> HitMoment => _hitMoment;

        public CharacterCombat(Character owner, CharacterEquipment equipment, CombatPlan plan)
        {
            _owner = owner;
            _equipment = equipment;
            Cycle = new AttackCycle(plan.Combo);
            Skill = plan.Skill;
            Ultimate = plan.Ultimate;
            UltimateParticleID = plan.UltimateParticleID;
        }

        /// <summary>사이클에서 다음 평타를 꺼내 휘두른다. 맞히는 건 HitMoment 를 받는 쪽이 한다.</summary>
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

        /// <summary>스킬을 휘두른다. 쿨이 안 돌았거나 스킬이 없으면 아무 일도 없다.</summary>
        public void UseSkill()
        {
            Cast(Skill, SkillCooldown);
        }

        /// <summary>궁극기를 휘두른다. 쿨이 안 돌았거나 궁극기가 없으면 아무 일도 없다.</summary>
        public void UseUltimate()
        {
            Cast(Ultimate, UltimateCooldown);
        }

        public void CancelCast()
        {
            if (!IsCasting) return;

            _castMotionRunning = false;
            _castHitPending = false;
            SyncCasting();

            _castTimer?.Dispose();
            _castTimer = null;
            _hitTimer?.Dispose();
            _hitTimer = null;
        }

        private bool CanCast(CharacterSkill skill, CooldownTimer cooldown)
        {
            return !IsCasting && _owner.CanAct && skill != null && cooldown.IsReady;
        }

        /// <summary>쿨타임 기술 공용 발사. 평타 콤보 도중이어도 진행 중이던 타격을 끊고 즉발한다.</summary>
        private void Cast(CharacterSkill skill, CooldownTimer cooldown)
        {
            if (!CanCast(skill, cooldown))
            {
                return;
            }

            _owner.StopMove();
            cooldown.Begin(skill.Cooldown);

            // 모션이 끝날 때까지 잠근다. 타격 대기는 Swing 이 따로 걸어서, 둘 다 풀려야 잠금이 열린다.
            _castMotionRunning = true;
            SyncCasting();

            _castTimer?.Dispose();
            _castTimer = Observable.Timer(TimeSpan.FromSeconds(skill.MotionSeconds))
                .Subscribe(this, (_, self) => self.EndCastMotion());

            Swing(skill, 0f, isCast: true);
        }

        private void EndCastMotion()
        {
            _castTimer = null;
            _castMotionRunning = false;
            SyncCasting();
        }

        /// <summary>잠금은 모션과 타격 대기의 합집합이다. 이 한 줄 밖에서 _isCasting 을 건드리는 곳은 없다.</summary>
        private void SyncCasting()
        {
            _isCasting.Value = _castMotionRunning || _castHitPending;
        }

        /// <summary>휘두르기 시작 — 모션을 알리고, 타격 시점에 판정 신호를 낸다. 이전 타격 판정은 여기서 끊긴다.</summary>
        private void Swing(BasicAttack attack, float cooldown, bool isCast = false)
        {
            if (cooldown > 0f)
            {
                AttackCooldown.Begin(cooldown);
            }

            if (isCast)
            {
                _castHitPending = true;
                SyncCasting();
            }

            _attacked.OnNext(attack);

            // 앞선 타격 판정은 여기서 끊긴다. 캐스팅 타격이 대기 중이면 IsCasting 이 참이라
            // 평타가 이 지점까지 오지 못한다 — 기술의 타격이 평타에 먹히는 일은 구조적으로 없다.
            _hitTimer?.Dispose();
            _hitTimer = Observable.Timer(TimeSpan.FromSeconds(attack.HitTime))
                .Subscribe((self: this, attack, isCast), (_, state) => state.self.FireHit(state.attack, state.isCast));
        }

        private void FireHit(BasicAttack attack, bool isCast)
        {
            _hitTimer = null;

            if (isCast)
            {
                _castHitPending = false;
                SyncCasting();
            }

            _hitMoment.OnNext(attack);
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

        /// <summary>실제 피해 = 최종 공격력 × 타격 배수. 최종 공격력 = 기본 + 성장 + 장비(무기 포함) — 합산은 캐릭터가 이미 해뒀다.</summary>
        public int DamageOf(BasicAttack attack)
        {
            return Mathf.RoundToInt(_owner.FinalAttackPower * attack.PowerMultiplier);
        }

        public void Dispose()
        {
            _castTimer?.Dispose();
            _hitTimer?.Dispose();
            _isCasting.Dispose();
            _hitMoment.Dispose();
            _attacked.Dispose();
            AttackCooldown.Dispose();
            SkillCooldown.Dispose();
            UltimateCooldown.Dispose();
        }
    }
}
