using System;
using R3;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sayne
{
    /// <summary>캐릭터 본체. 누구이고 얼마나 튼튼하고 어디 있는가를 안다. 싸우는 일은 Combat 이 한다.</summary>
    public abstract class Character : MonoBehaviour, IDamageable, IMovable, IAttacker
    {
        [SerializeField] private CharacterMotion _motion;
        [SerializeField] private CharacterSkin _skin;

        private readonly ReactiveProperty<CharacterStats> _currentStats = new ReactiveProperty<CharacterStats>();

        private readonly ReactiveProperty<int> _currentHP = new ReactiveProperty<int>();
        private readonly ReactiveProperty<CharacterStateType> _state = new ReactiveProperty<CharacterStateType>(CharacterStateType.Idle);

        private readonly Subject<Vector3> _looked = new Subject<Vector3>();
        private readonly Subject<int> _damaged = new Subject<int>();
        private readonly Subject<Character> _died = new Subject<Character>();

        private Vector3 _moveDirection;
        private IDisposable _stagger;

        /// <summary>내가 누구인지. 이름·초상화 같은 건 이 ID 로 전역 테이블에서 찾는다.</summary>
        public abstract string ID { get; }

        private CharacterStats _baseStats;
        private CharacterStats _growthBonus;
        private CharacterStats _equipmentBonus;

        /// <summary>타고난 몸. Init 이후 변하지 않는다.</summary>
        public CharacterStats BaseStats => _baseStats;

        /// <summary>성장이 얹은 몫. 성장 없는 캐릭터(적)는 0 이다.</summary>
        public CharacterStats GrowthBonus => _growthBonus;

        /// <summary>낀 장비 전부가 얹은 몫. 무기 공격력도 여기로 들어온다.</summary>
        public CharacterStats EquipmentBonus => _equipmentBonus;

        /// <summary>최종 공격력 = 기본 + 성장 + 장비.</summary>
        public int FinalAttackPower => _baseStats.AttackPower + _growthBonus.AttackPower + _equipmentBonus.AttackPower;

        /// <summary>최종 체력 = 기본 + 성장 + 장비.</summary>
        public int FinalMaxHP => _baseStats.MaxHP + _growthBonus.MaxHP + _equipmentBonus.MaxHP;

        /// <summary>최종 스탯. 언제나 기본 + 성장 합성으로만 나온다 — 직접 쓰는 값이 아니라 파생값이다.</summary>
        public ReadOnlyReactiveProperty<CharacterStats> CurrentStats => _currentStats;

        /// <summary>뭘 입고 있나. 입기·벗기·구독은 전부 여기 있다.</summary>
        public CharacterEquipment Equipment { get; } = new CharacterEquipment();

        /// <summary>뭘 갖고 있나. 주운 장비가 쌓이는 가방이다 — 입고 있는 것과는 별개다.</summary>
        public CharacterInventory Inventory { get; } = new CharacterInventory();

        /// <summary>지금 걸린 상태이상. 경직·기절 같은 것들.</summary>
        public CharacterDebuffs Debuffs { get; } = new CharacterDebuffs();

        /// <summary>지금 싸우는 상대. 없으면 null — 고르는 건 컨트롤러·AI 가 한다.</summary>
        public Character Target { get; private set; }

        /// <summary>무엇으로 어떻게 때리는가.</summary>
        public CharacterCombat Combat { get; private set; }

        public ReadOnlyReactiveProperty<int> CurrentHP => _currentHP;
        public ReadOnlyReactiveProperty<CharacterStateType> State => _state;

        /// <summary>HP 의 파생값이라 따로 들지 않는다.</summary>
        public bool IsAlive => _currentHP.Value > 0;

        /// <summary>맞아서 움찔하는 중인가. 평타경직은 상태이상이 아니라 기본 반응이다.</summary>
        public bool IsStaggered => _stagger != null;

        /// <summary>움직이거나 때릴 수 있는 상태인가. 움찔·기절·빙결이면 아무것도 못 한다.</summary>
        public bool CanAct => IsAlive && !IsStaggered
            && !Debuffs.Has(DebuffType.Stun) && !Debuffs.Has(DebuffType.Freeze);

        /// <summary>기술을 쓰는 중인가. 그 동안은 평타·다른 기술은 물론 이동 명령도 받지 않는다.</summary>
        public bool IsActing => Combat != null && Combat.IsCasting;

        public bool CanMove => CanAct && !IsActing;

        /// <summary>걷는 중인가. 걸으면서는 공격하지 못한다.</summary>
        public bool IsMoving => _moveDirection.sqrMagnitude > 0f;

        /// <summary>방금 바라본 방향. 좌우 반전은 이걸 보고 그림 쪽에서 정한다.</summary>
        public Observable<Vector3> Looked => _looked;

        public Observable<int> Damaged => _damaged;
        public Observable<Character> Died => _died;

        private void Awake()
        {
        }
        /// <summary>스폰 직후 호출. 프리팹은 벗은 상태이고, 여기서 받은 한 벌을 그때 입는다. 맨손도 무기 한 종류다.</summary>
        public void Init(CharacterStats stats, CombatPlan combatPlan, EquipmentSet equipment)
        {
            _baseStats = stats;
            _growthBonus = default;
            _equipmentBonus = default;

            Equipment.Wear(equipment);
            Combat = new CharacterCombat(this, Equipment, combatPlan);

            // 장비가 바뀔 때마다 장비 몫을 다시 합산한다. 구독 즉시 한 번 돌아서 방금 입은 한 벌도 반영된다.
            foreach (var slot in EquipmentSlots.All)
            {
                Equipment.Observe(slot)
                    .Subscribe(this, (_, self) => self.RecalcEquipmentBonus())
                    .AddTo(this);
            }

            _currentHP.Value = FinalMaxHP;
            _state.Value = CharacterStateType.Idle;



            _motion.Bind(this);
            _skin.Bind(this);
        }

        /// <summary>성장 몫을 갈아끼운다. 최종 스탯은 언제나 재합성이라 누적 오차가 없다.</summary>
        public void SetGrowthBonus(CharacterStats bonus)
        {
            _growthBonus = bonus;
            OnBonusChanged();
        }

        private void RecalcEquipmentBonus()
        {
            _equipmentBonus = Equipment.TotalStats();
            OnBonusChanged();
        }

        /// <summary>어느 근원이든 몫이 바뀌면 여기로 모인다. MaxHP 가 늘어난 만큼 현재 HP 도 같이 찬다 — 성장·장착이 벌점이 되지 않게.</summary>
        private void OnBonusChanged()
        {
            var oldMaxHP = _currentStats.Value.MaxHP;
            RefreshStats();

            var maxHPGain = FinalMaxHP - oldMaxHP;
            _currentHP.Value = Mathf.Clamp(_currentHP.Value + Mathf.Max(maxHPGain, 0), 0, FinalMaxHP);
        }

        /// <summary>최종 스탯 = 기본 + 성장 + 장비. 이 한 줄 밖에서 _currentStats 를 채우는 곳은 없다.</summary>
        private void RefreshStats()
        {
            _currentStats.Value = _baseStats.Add(_growthBonus).Add(_equipmentBonus);
        }

        /// <summary>이 방향으로 걷는다. 멈추려면 StopMove 를 부른다.</summary>
        public void Move(Vector3 direction)
        {
            if (!CanMove)
            {
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

        /// <summary>맞아서 잠깐 움찔한다. 그 사이엔 못 움직이고 못 때린다.</summary>
        public void Stagger(float seconds)
        {
            if (!IsAlive || seconds <= 0f)
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

            transform.Translate(_moveDirection * (_currentStats.Value.MoveSpeed * Time.deltaTime));
        }

        public void TakeDamage(int amount)
        {
            // 죽은 자는 다시 죽지 않는다. 시체를 때려도 아무 일도 일어나지 않는다.
            if (!IsAlive)
            {
                return;
            }

            _currentHP.Value = Mathf.Max(_currentHP.Value - amount, 0);

            if (!IsAlive)
            {
                Die();
            }

            _damaged.OnNext(amount);
        }

        private void Die()
        {
            StopMove();
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
            _died.Dispose();
            _stagger?.Dispose();
            Combat?.Dispose();
            Equipment.Dispose();
            Inventory.Dispose();
            Debuffs.Dispose();
        }
    }
}
