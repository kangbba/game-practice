using System;
using DG.Tweening;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>캐릭터 본체. 누구이고 얼마나 튼튼하고 어디 있는가를 안다. 싸우는 일은 Combat 이 한다.</summary>
    public abstract class Character : MonoBehaviour, IDamageable, IMovable, IAttacker
    {
        [SerializeField] private CharacterMotion _motion;
        [SerializeField] private CharacterSkin _skin;

        private readonly ReactiveProperty<StatGroup> _currentStats = new ReactiveProperty<StatGroup>();

        private readonly ReactiveProperty<int> _currentHP = new ReactiveProperty<int>();
        private readonly ReactiveProperty<CharacterStateType> _state = new ReactiveProperty<CharacterStateType>(CharacterStateType.Idle);

        private readonly Subject<Vector3> _looked = new Subject<Vector3>();
        private readonly Subject<int> _damaged = new Subject<int>();
        private readonly Subject<Character> _fell = new Subject<Character>();
        private readonly Subject<Character> _died = new Subject<Character>();
        private readonly ReactiveProperty<bool> _isOnUltimateStage = new ReactiveProperty<bool>();

        /// <summary>이보다 약한 이동 요청은 멈춤으로 본다. 밀어내기 힘이 아주 약할 때 제자리걸음을 하지 않게.</summary>
        private const float MinMoveMagnitude = 0.1f;

        private Vector3 _moveDirection;
        private IDisposable _stagger;
        private bool _isDead;

        /// <summary>내가 누구인지. 이름·초상화 같은 건 이 ID 로 전역 테이블에서 찾는다.</summary>
        public abstract string ID { get; }

        private StatGroup _baseStats;
        private StatGroup _growthBonus;
        private StatGroup _equipmentBonus;

        /// <summary>타고난 몸. Init 이후 변하지 않는다.</summary>
        public StatGroup BaseStats => _baseStats;

        /// <summary>성장이 얹은 몫. 성장 없는 캐릭터(적)는 0 이다.</summary>
        public StatGroup GrowthBonus => _growthBonus;

        /// <summary>낀 장비 전부가 얹은 몫. 무기 공격력도 여기로 들어온다.</summary>
        public StatGroup EquipmentBonus => _equipmentBonus;

        /// <summary>최종 스탯. 기본 + 성장 + 장비 합성 하나뿐이다 — 직접 쓰는 값이 아니라 파생값이다.</summary>
        public ReadOnlyReactiveProperty<StatGroup> CurrentStats => _currentStats;

        /// <summary>최종 공격력. 합성은 CurrentStats 가 이미 했다.</summary>
        public int FinalAttackPower => (int)_currentStats.Value.Get(StatType.AttackPower);

        /// <summary>최종 체력. 합성은 CurrentStats 가 이미 했다.</summary>
        public int FinalMaxHP => (int)_currentStats.Value.Get(StatType.MaxHP);

        /// <summary>뭘 입고 있나. 입기·벗기·구독은 전부 여기 있다.</summary>
        public CharacterEquipment Equipment { get; } = new CharacterEquipment();

        /// <summary>지금 걸린 상태이상. 경직·기절 같은 것들.</summary>
        public CharacterDebuffs Debuffs { get; } = new CharacterDebuffs();

        /// <summary>지금 싸우는 상대. 없으면 null — 고르는 건 컨트롤러·AI 가 한다.</summary>
        public Character Target { get; private set; }

        /// <summary>무엇으로 어떻게 때리는가.</summary>
        public CharacterCombat Combat { get; private set; }

        /// <summary>발에서 머리 꼭대기까지의 키. 머리 위에 무언가 띄우는 쪽이 이걸 쓴다.</summary>
        public float GetHeight() => _skin.GetHeight();

        public ReadOnlyReactiveProperty<int> CurrentHP => _currentHP;
        public ReadOnlyReactiveProperty<CharacterStateType> State => _state;

        /// <summary>싸울 수 있는 몸인가. HP 의 파생값이라 따로 들지 않는다.</summary>
        public bool IsAlive => _currentHP.Value > 0;

        /// <summary>
        /// 죽음처리까지 끝났나. HP 가 0 이 되는 것(쓰러짐)과 죽음처리는 따로다 —
        /// 궁극기 동안은 HP 0 인 적이 죽음처리 없이 서서 계속 맞는다. 그 사이는 IsAlive 도 IsDead 도 아니다.
        /// </summary>
        public bool IsDead => _isDead;

        /// <summary>궁극기 무대에 올라 있나. 무대 밖 캐릭터의 HP바를 가리는 쪽이 이걸 본다.</summary>
        public ReadOnlyReactiveProperty<bool> IsOnUltimateStage => _isOnUltimateStage;

        /// <summary>손에 실제로 붙어 있는 무기. 맨손이면 null.</summary>
        public Weapon WornWeapon => _skin.WornWeapon;

        /// <summary>그림이 오른쪽을 보고 있나. 앞쪽을 휩쓰는 판정이 이걸 본다.</summary>
        public bool IsFacingRight => _motion.IsFacingRight;

        /// <summary>죽음처리가 몇 초 걸리나 — 쓰러지는 모션 길이.</summary>
        public float DeathSeconds => _motion.DeathSeconds;

        /// <summary>이 몸이 그 모션을 몇 초 동안 하나. 같은 기술이라도 캐릭터마다 클립 길이가 다르다.</summary>
        public float GetMotionSeconds(int stateHash)
        {
            return _motion.GetClipSeconds(stateHash);
        }

        /// <summary>맞아서 움찔하는 중인가. 평타경직은 상태이상이 아니라 기본 반응이다.</summary>
        public bool IsStaggered => _stagger != null;

        /// <summary>움직이거나 때릴 수 있는 상태인가. 움찔·기절·빙결이면 아무것도 못 한다.</summary>
        public bool CanAct => IsAlive && !IsStaggered
            && !Debuffs.Has(DebuffType.Stun) && !Debuffs.Has(DebuffType.Freeze);

        /// <summary>기술을 쓰는 중인가. 그 동안은 평타·다른 기술·이동을 받지 않는다 — 스킬을 걸어서 끊으려면 먼저 CancelCast 를 부른다.</summary>
        public bool IsActing => Combat != null && Combat.IsCasting;

        public bool CanMove => CanAct && !IsActing;

        /// <summary>걷는 중인가. 걸으면서는 공격하지 못한다.</summary>
        public bool IsMoving => _moveDirection.sqrMagnitude > 0f;

        /// <summary>방금 바라본 방향. 좌우 반전은 이걸 보고 그림 쪽에서 정한다.</summary>
        public Observable<Vector3> Looked => _looked;

        public Observable<int> Damaged => _damaged;

        /// <summary>HP 가 0 이 됐다. 언제 죽음처리할지는 이 몸의 수명 주인(매니저)이 정해 Die 를 부른다.</summary>
        public Observable<Character> Fell => _fell;

        public Observable<Character> Died => _died;

        /// <summary>평소 그려지는 정렬 레이어.</summary>
        protected abstract string SortingLayer { get; }

        /// <summary>궁극기 무대에 올랐을 때의 정렬 레이어. 궁극기 백그라운드 위다.</summary>
        protected abstract string UltimateSortingLayer { get; }

        /// <summary>궁극기 무대에 올리거나 내린다. 어느 레이어인지만 정하고, 까는 건 그림 쪽이 한다.</summary>
        public void SetOnUltimateStage(bool isOn)
        {
            _skin.SetSortingLayer(isOn ? UltimateSortingLayer : SortingLayer);
            _isOnUltimateStage.Value = isOn;
        }

        /// <summary>스폰 직후 호출. 프리팹은 벗은 상태이고, 여기서 받은 한 벌을 그때 입는다. 맨손도 무기 한 종류다.</summary>
        public void Init(StatGroup stats, CombatPlan combatPlan, EquipmentSet equipment)
        {
            _baseStats = stats;
            _growthBonus = default;

            Equipment.Wear(equipment);
            Combat = new CharacterCombat(this, Equipment, combatPlan);

            // 방금 입은 한 벌은 여기서 직접 반영한다. 구독은 그 다음에 바뀌는 것만 받는다.
            _equipmentBonus = Equipment.TotalStats();
            RefreshStats();

            foreach (var slot in EquipmentSlots.All)
            {
                Equipment.Observe(slot)
                    .Skip(1)
                    .Subscribe(this, (_, self) => self.SetEquipmentBonus(self.Equipment.TotalStats()))
                    .AddTo(this);
            }

            _currentHP.Value = FinalMaxHP;
            _state.Value = CharacterStateType.Idle;

            _motion.Bind(this);
            _skin.Bind(this);
            _skin.SetSortingLayer(SortingLayer);
        }

        /// <summary>성장 몫을 갈아끼운다. 성장은 오르기만 하고, 늘어난 MaxHP 만큼 현재 HP 도 같이 차오른다 — 성장이 벌점이 되지 않게.</summary>
        public void SetGrowthBonus(StatGroup bonus)
        {
            var maxHPGain = (int)(bonus.Get(StatType.MaxHP) - _growthBonus.Get(StatType.MaxHP));

            _growthBonus = bonus;
            RefreshStats();

            _currentHP.Value = Mathf.Clamp(_currentHP.Value + maxHPGain, 0, FinalMaxHP);
        }

        /// <summary>장비 몫을 갈아끼운다. 입고 벗는 건 회복이 아니라서 현재 HP 는 건드리지 않는다.</summary>
        private void SetEquipmentBonus(StatGroup bonus)
        {
            _equipmentBonus = bonus;
            RefreshStats();
        }

        /// <summary>최종 스탯 = 기본 + 성장 + 장비. _currentStats 를 채우는 곳은 여기 하나뿐이고, 현재 HP 도 여기서 새 최대치 안으로 들어온다.</summary>
        private void RefreshStats()
        {
            _currentStats.Value = _baseStats.Add(_growthBonus).Add(_equipmentBonus);
            _currentHP.Value = Mathf.Min(_currentHP.Value, FinalMaxHP);
        }

        /// <summary>
        /// 이 방향으로 걷는다. 방향이 거의 없으면 멈춘다 — 제자리에 선 채 걷는 모션이 돌면 안 된다.
        /// AI 는 사거리 안이거나 타겟이 없을 때 0 방향을 보낸다.
        /// </summary>
        public void Move(Vector3 direction)
        {
            if (!CanMove)
            {
                return;
            }

            if (direction.magnitude < MinMoveMagnitude)
            {
                StopMove();
                return;
            }

            _moveDirection = Vector3.ClampMagnitude(direction, 1f);

            // 상태는 명령이 들어온 그 자리에서 정한다. Update 를 기다리면 한 프레임 늦는다.
            _state.Value = CharacterStateType.Walk;
            Look(_moveDirection);
        }

        /// <summary>싸울 상대를 정한다. null 이면 교전 해제.</summary>
        public void SetTarget(Character target)
        {
            Target = target;
        }

        /// <summary>
        /// 맞아서 잠깐 움찔한다. 그 사이엔 못 움직이고 못 때린다.
        /// 기술(스킬·궁극기)을 쓰는 중엔 움찔하지 않는다 — 기술 클립은 끝까지 재생돼야 한다.
        /// 스킬을 끊는 건 수동 이동과 쓰러짐·죽음뿐이고, 궁극기는 이동으로도 끊기지 않는다.
        /// </summary>
        public void Stagger(float seconds)
        {
            if (!IsAlive || seconds <= 0f || Combat.IsCasting)
            {
                return;
            }

            StopMove();
            _state.Value = CharacterStateType.Hit;

            _stagger?.Dispose();
            _stagger = Observable.Timer(TimeSpan.FromSeconds(seconds))
                .Subscribe(this, (_, self) => self.EndStagger())
                .AddTo(this);
        }

        private void EndStagger()
        {
            _stagger = null;

            if (IsAlive)
            {
                _state.Value = CharacterStateType.Idle;
            }
        }

        /// <summary>그쪽을 바라본다. 걷지 않고 제자리에서 때릴 때도 방향은 맞춰야 한다.</summary>
        public void Look(Vector3 direction)
        {
            if (!IsAlive)
            {
                return;
            }

            // 길이를 1 로 맞춰서 보낸다. 거리가 가깝든 멀든 방향 판정이 같아야 한다.
            _looked.OnNext(direction.normalized);
        }

        public void StopMove()
        {
            _moveDirection = Vector3.zero;

            if (IsAlive)
            {
                _state.Value = CharacterStateType.Idle;
            }
        }

        /// <summary>움직이는 동안 자리를 옮기기만 한다. 상태는 Move·StopMove 가 이미 정했다.</summary>
        private void Update()
        {
            if (!IsAlive || _moveDirection.sqrMagnitude <= 0f)
            {
                return;
            }

            transform.Translate(_moveDirection * (_currentStats.Value.Get(StatType.MoveSpeed) * Time.deltaTime));
        }

        /// <summary>
        /// 맞는다. HP 가 0 이 되면 쓰러질 뿐 죽음처리는 하지 않는다 — Fell 을 받은 매니저가 정한다.
        /// 쓰러진 채 죽음처리를 기다리는 몸은 마네킹처럼 계속 맞고, 데미지도 계속 뜬다.
        /// </summary>
        public void TakeDamage(int amount)
        {
            // 죽음처리까지 끝난 몸은 다시 죽지 않는다. 시체를 때려도 아무 일도 일어나지 않는다.
            if (_isDead)
            {
                return;
            }

            var wasAlive = IsAlive;
            _currentHP.Value = Mathf.Max(_currentHP.Value - amount, 0);

            _damaged.OnNext(amount);

            if (wasAlive && !IsAlive)
            {
                Fall();
            }
        }

        /// <summary>몸을 흐려 없앤다. 그림만 사라질 뿐 파괴는 수명의 주인(매니저)이 한다.</summary>
        public Tween FadeOut(float seconds)
        {
            return _skin.FadeOut(seconds);
        }

        /// <summary>HP 를 채운다. 최대치를 넘지 않고, 쓰러진 몸은 차오르지 않는다.</summary>
        public void Heal(int amount)
        {
            if (!IsAlive)
            {
                return;
            }

            _currentHP.Value = Mathf.Min(_currentHP.Value + amount, FinalMaxHP);
        }

        /// <summary>HP 가 0 이 된 순간. 움직임을 멈추고 맞는 자세로 굳는다.</summary>
        private void Fall()
        {
            StopMove();
            _stagger?.Dispose();
            _stagger = null;
            _state.Value = CharacterStateType.Hit;

            _fell.OnNext(this);
        }

        /// <summary>죽음처리. 쓰러지는 모션이 나가고 Died 가 울린다. 수명 주인(매니저)만 부른다.</summary>
        public void Die()
        {
            _isDead = true;
            _state.Value = CharacterStateType.Death;

            _died.OnNext(this);
        }

        protected virtual void OnDestroy()
        {
            _currentStats.Dispose();
            _currentHP.Dispose();
            _state.Dispose();
            _looked.Dispose();
            _damaged.Dispose();
            _fell.Dispose();
            _died.Dispose();
            _isOnUltimateStage.Dispose();
            _stagger?.Dispose();
            Combat?.Dispose();
            Equipment.Dispose();
            Debuffs.Dispose();
        }
    }
}
