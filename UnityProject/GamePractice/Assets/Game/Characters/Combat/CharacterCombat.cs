using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 캐릭터의 싸움 담당. 무엇으로 어떻게 때리는지를 안다.
    /// 스킬은 설계값에서 오고, 평타·궁극기와 공격 수치는 낀 무기에서 꺼낸다.
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

        /// <summary>가젯(돌진)으로 달려가는 중이다. 닿아야 풀린다.</summary>
        private bool _gadgetRunning;

        /// <summary>타격을 클립 이벤트로 받는 기술이 도는 중이면 그 기술. 이벤트가 올 때 이걸로 판정한다.</summary>
        private CharacterSkill _clipAttack;
        private readonly Character _owner;

        /// <summary>캐릭터의 스킬 설계값. 모션이 무기를 따라가므로 굳힌 스킬은 무기를 바꿔 들 때마다 다시 만든다.</summary>
        private readonly SkillData _skillData;
        private CharacterSkill _skill;
        private Weapon _skillWeapon;
        private readonly CharacterEquipment _equipment;

        /// <summary>평타 4콤보가 도는 사이클.</summary>
        public AttackCycle Cycle { get; }

        /// <summary>스킬. 궁극기와 같은 꼴이고 쿨이 짧다. 없는 캐릭터면 null — 적이 그렇다.</summary>
        /// <summary>
        /// 스킬. 이름·위력·쿨은 캐릭터 것이고, 모션은 지금 든 무기 계열(근접·원거리)을 따른다.
        /// 무기를 바꿔 들었을 때만 새로 만든다 — 같은 무기인 동안은 같은 객체라 "이 타격이 스킬인가"를 비교로 가린다.
        /// </summary>
        public CharacterSkill Skill
        {
            get
            {
                if (_skillWeapon != Weapon)
                {
                    _skillWeapon = Weapon;
                    _skill = _skillData.ToSkill(Weapon.SkillAnimation);
                }

                return _skill;
            }
        }

        /// <summary>
        /// 궁극기. 캐릭터가 아니라 든 무기의 것이다 — 무기를 바꿔 들면 궁극기도 바뀐다.
        /// 궁극기가 없는 무기(맨손 등)를 들었으면 null 이다. 쿨타임은 무기를 바꿔도 이어서 돈다.
        /// </summary>
        public CharacterSkill Ultimate => _equipment.Weapon.Ultimate;

        /// <summary>
        /// 가젯. 스킬보다 가벼운 잡기술 — 무게는 가젯 &lt; 스킬 &lt; 궁극기. 캐릭터의 것이라 무기를 바꿔 들어도 그대로다.
        /// 없는 캐릭터면 null — 적이 그렇다.
        /// </summary>
        public CharacterGadget Gadget { get; }

        /// <summary>궁극기 전용 연출. 든 무기에 없으면 null.</summary>
        public string UltimateParticleID => Weapon.UltimateParticleID;

        /// <summary>평타 쿨타임. 남은 초와 비율을 들고 있어서 UI 는 구독만 하면 된다.</summary>
        public CooldownTimer AttackCooldown { get; } = new CooldownTimer();

        /// <summary>스킬 쿨타임.</summary>
        public CooldownTimer SkillCooldown { get; } = new CooldownTimer();

        /// <summary>궁극기 쿨타임.</summary>
        public CooldownTimer UltimateCooldown { get; } = new CooldownTimer();

        /// <summary>가젯 쿨타임.</summary>
        public CooldownTimer GadgetCooldown { get; } = new CooldownTimer();

        /// <summary>
        /// 깊이(Z)를 몇 배로 쳐서 볼지. 클수록 위아래로 떨어진 적이 빨리 사거리 밖이 된다.
        /// 6 이면 맨손(사거리 2) 기준 Z 허용폭이 ±0.33 — 거의 같은 줄에 서야 닿는다.
        /// </summary>
        private const float DepthWeight = 6f;

        /// <summary>들고 있는 무기의 싸움 방식. 사거리·투사체를 묻는 곳이 전부 여기를 거친다.</summary>
        public Weapon Weapon => _equipment.Weapon.Model;

        public float AttackRange => Weapon.Range;

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
        /// 상대와 좌우로 최소 이만큼은 떨어져 선다. 사거리가 Z 를 빡세게 보니 비스듬히 다가가면
        /// Z 만 맞추고 X 는 거의 0 인 채로 사거리에 들어 몸이 겹친다 — 그걸 막는 몸 간격이다.
        /// </summary>
        public const float MinGap = 1.6f;

        /// <summary>다가갈 때 노리는 좌우 간격. MinGap 보다 넉넉해서 경계에서 섰다 걸었다 하지 않는다.</summary>
        private const float StandGap = 2f;

        /// <summary>상대 옆에 설 자리. 지금 내가 있는 쪽 옆, 상대와 같은 줄(Z)이다.</summary>
        public static Vector3 StandPoint(Vector3 self, Vector3 target)
        {
            return StandPoint(self, target, StandGap);
        }

        /// <summary>상대와 같은 줄(Z)에서, 지금 내가 있는 쪽으로 gap 만큼 떨어진 자리.</summary>
        private static Vector3 StandPoint(Vector3 self, Vector3 target, float gap)
        {
            var side = self.x >= target.x ? 1f : -1f;
            return new Vector3(target.x + side * gap, self.y, target.z);
        }

        /// <summary>사거리에서 이만큼 안쪽까지 들어가 선다. 딱 사거리 끝에 서면 조금만 밀려도 벗어난다.</summary>
        private const float EngageRangeFactor = 0.8f;

        /// <summary>
        /// 이 상대를 칠 수 있게 붙어 설 자리. 근접은 옆자리, 원거리는 사거리 안쪽 끝이다 — 원거리가 옆에 딱 붙을 까닭은 없다.
        /// 가젯(돌진)이 달려갈 곳으로 쓴다.
        /// </summary>
        public Vector3 EngagePoint(Vector3 self, Vector3 target)
        {
            return StandPoint(self, target, Mathf.Max(StandGap, AttackRange * EngageRangeFactor));
        }

        /// <summary>여기서 멈춰 때려도 되는가. 닿는 거리 안이고, 좌우로 겹치지 않았다.</summary>
        public static bool IsStandable(Vector3 offset, float reach)
        {
            return DistanceOf(offset) <= reach && Mathf.Abs(offset.x) >= MinGap;
        }

        /// <summary>
        /// 기술을 쓰는 중인가. 모션이 안 끝났거나 타격이 아직 안 나갔거나 가젯으로 달려가는 중이면 참이다.
        /// 이 동안은 평타·다른 기술·이동 어느 커맨드도 받지 않는다 — 시간 비교가 아니라 상태로 잠근다.
        /// </summary>
        public bool IsCasting => _isCasting.Value;

        /// <summary>같은 잠금을 UI 가 구독하는 통로. 캐스팅 중엔 발동 버튼이 꺼져 있어야 한다.</summary>
        public ReadOnlyReactiveProperty<bool> Casting => _isCasting;

        public bool CanAttack => !IsCasting && _owner.CanAct && !_owner.IsMoving && AttackCooldown.IsReady;
        public bool CanUseSkill => CanCast(Skill, SkillCooldown);
        public bool CanUseUltimate => CanCast(Ultimate, UltimateCooldown);
        public bool CanUseGadget => !IsCasting && _owner.CanAct && Gadget != null && GadgetCooldown.IsReady;

        /// <summary>① 휘두르기 시작했다. 모션·연출이 이걸 본다.</summary>
        public Observable<BasicAttack> Attacked => _attacked;

        /// <summary>② 맞는 순간이 됐다. 이때 대상을 다시 찾아 판정한다 — 아무도 없으면 헛친다.</summary>
        public Observable<BasicAttack> HitMoment => _hitMoment;

        public CharacterCombat(Character owner, CharacterEquipment equipment, SkillData skill, CharacterGadget gadget)
        {
            _owner = owner;
            _equipment = equipment;
            Cycle = new AttackCycle();
            _skillData = skill;
            Gadget = gadget;

            // 태어나자마자 기술부터 쏘지 않게, 방금 쓴 것과 같은 상태로 시작한다.
            StartOnCooldown(Skill, SkillCooldown);
            StartOnCooldown(Ultimate, UltimateCooldown);

            if (Gadget != null)
            {
                GadgetCooldown.Begin(Gadget.Cooldown);
            }
        }

        /// <summary>쿨이 다 찬 상태가 아니라, 한 번 쓰고 난 직후와 똑같이 쿨타임 전체가 남은 상태로 둔다.</summary>
        private static void StartOnCooldown(CharacterSkill skill, CooldownTimer cooldown)
        {
            if (skill != null)
            {
                cooldown.Begin(skill.Cooldown);
            }
        }

        /// <summary>사이클에서 다음 평타를 꺼내 휘두른다. 맞히는 건 HitMoment 를 받는 쪽이 한다.</summary>
        public void Attack()
        {
            if (!CanAttack)
            {
                return;
            }

            // 묶음의 마지막 타를 내면 다음 묶음까지 길게 쉰다. 그 사이는 짧게 이어 친다.
            var info = Weapon;
            var interval = Cycle.IsAtLast(info.ComboCount) ? info.CycleInterval : info.ComboInterval;

            var attack = ComboAttack(Cycle.Draw(info.ComboCount), info.ComboCount);
            Swing(attack, interval, attack.HitTime);
        }

        /// <summary>
        /// 이번에 나갈 평타 한 타. 위력은 무기 공격력 그대로고, 타마다 다르게 주지 않는다.
        /// 모션은 네 개뿐이라 그보다 긴 묶음은 앞으로 되감아 쓴다.
        /// </summary>
        private BasicAttack ComboAttack(int step, int comboCount)
        {
            // 선언한 이름은 네 개뿐이라 그보다 긴 묶음은 이름을 되감는다.
            // 몸이 그 모션을 실제로 가졌는지는 CharacterMotion 이 다시 한 번 추린다.
            var animation = CharacterAnimations.ComboNames[step % CharacterAnimations.ComboNames.Length];

            // 영웅의 평타만 움찔하게 한다. 적의 평타까지 움찔하게 하면 계속 얻어맞는 동안 영웅 조작이 막힌다.
            // 마지막 타가 조금 더 오래 움찔하게 해서 묶음의 맺음을 준다.
            var stagger = _owner is Hero ? (step == comboCount - 1 ? 0.28f : 0.16f) : 0f;

            return new BasicAttack($"평타{step + 1}", animation, 1f, staggerSeconds: stagger);
        }

        /// <summary>스킬을 휘두른다. 쿨이 안 돌았거나 스킬이 없으면 아무 일도 없다.</summary>
        public void UseSkill()
        {
            Cast(Skill, SkillCooldown);
        }

        /// <summary>
        /// 궁극기를 쓸 수 있으면 멈춰 서고 쿨을 돌린 뒤 참을 돌려준다. 못 쓰면 아무 일 없이 거짓.
        /// 휘두르는 건 연출이 무대를 다 깐 뒤 PerformUltimateAsync 로 한다 — 여기선 전투 쪽 준비만 한다.
        /// </summary>
        public bool TryStartUltimate()
        {
            if (!CanUseUltimate)
            {
                return false;
            }

            _owner.StopMove();
            UltimateCooldown.Begin(Ultimate.Cooldown);
            return true;
        }

        /// <summary>
        /// 궁극기 본편. 모션을 틀고 타격을 내고, 모션과 타격이 둘 다 끝나면 끝난다.
        /// 모션 길이는 이 캐릭터의 궁극기 클립 길이 그대로다 — 클립을 늘리면 기다리는 시간도 같이 는다.
        /// </summary>
        public UniTask PerformUltimateAsync(CancellationToken token)
        {
            StartCast(Ultimate);

            return UniTask.WaitUntil(() => !_castMotionRunning && !_castHitPending, cancellationToken: token);
        }

        /// <summary>
        /// 가젯을 쓴다 — 그 자리까지 달려가고, 닿을 때까지 다른 커맨드를 받지 않는다(스킬과 같은 잠금).
        /// 어디로 달려갈지는 부르는 쪽이 정한다. 쿨이 안 돌았거나 가젯이 없으면 아무 일도 없다.
        /// </summary>
        public void UseGadget(Vector3 destination)
        {
            if (!CanUseGadget)
            {
                return;
            }

            GadgetCooldown.Begin(Gadget.Cooldown);

            _gadgetRunning = true;
            SyncCasting();

            _owner.Dash(destination, Gadget.DashSpeed, EndGadget);
        }

        private void EndGadget()
        {
            _gadgetRunning = false;
            SyncCasting();
        }

        /// <summary>스킬 모션과 아직 안 나간 타격, 달려가던 가젯을 끊는다.</summary>
        public void CancelCast()
        {
            if (!IsCasting) return;

            if (_gadgetRunning)
            {
                _gadgetRunning = false;
                _owner.StopDash();
            }

            _castMotionRunning = false;
            _castHitPending = false;
            _clipAttack = null;
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

            StartCast(skill);
        }

        /// <summary>모션을 틀고 잠근다. 모션이 끝나고 타격이 나가야 풀린다. 길이는 이 몸의 클립에서 읽는다.</summary>
        private void StartCast(CharacterSkill skill)
        {
            var motionSeconds = _owner.GetMotionSeconds(skill.AnimationHash);

            // 모션이 끝날 때까지 잠근다. 타격 대기는 Swing 이 따로 걸어서, 둘 다 풀려야 잠금이 열린다.
            _castMotionRunning = true;
            SyncCasting();

            _castTimer?.Dispose();
            _castTimer = Observable.Timer(TimeSpan.FromSeconds(motionSeconds))
                .Subscribe(this, (_, self) => self.EndCastMotion());

            if (skill.HitsFromClip)
            {
                // 타격은 클립 키프레임의 이벤트가 HitFrame 으로 들고 온다. 모션이 끝나면 타격도 끝이다.
                _clipAttack = skill;
                _hitTimer?.Dispose();
                _hitTimer = null;
                _attacked.OnNext(skill);
                return;
            }

            Swing(skill, 0f, motionSeconds * skill.ImpactRatio, isCast: true);
        }

        /// <summary>클립 키프레임이 "지금 맞는다"고 알렸다(CharacterMotion.OnHitFrame). 도는 중인 기술로 판정을 낸다.</summary>
        public void HitFrame()
        {
            _hitMoment.OnNext(_clipAttack);
        }

        private void EndCastMotion()
        {
            _castTimer = null;
            _castMotionRunning = false;
            _clipAttack = null;
            SyncCasting();
        }

        /// <summary>잠금은 모션·타격 대기·가젯 돌진의 합집합이다. 이 한 줄 밖에서 _isCasting 을 건드리는 곳은 없다.</summary>
        private void SyncCasting()
        {
            _isCasting.Value = _castMotionRunning || _castHitPending || _gadgetRunning;
        }

        /// <summary>휘두르기 시작 — 모션을 알리고, 타격 시점에 판정 신호를 낸다. 이전 타격 판정은 여기서 끊긴다.</summary>
        private void Swing(BasicAttack attack, float cooldown, float hitTime, bool isCast = false)
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
            _hitTimer = Observable.Timer(TimeSpan.FromSeconds(hitTime))
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

            foreach (var debuff in Weapon.HitDebuffs)
            {
                target.Debuffs.Apply(debuff);
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
            GadgetCooldown.Dispose();
        }
    }
}
