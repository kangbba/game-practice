using R3;
using UnityEngine;

namespace Sayne
{
    public abstract class Character : MonoBehaviour, IDamageable, IMovable, IAttacker
    {
        private readonly ReactiveProperty<int> _currentHP = new ReactiveProperty<int>();
        private readonly ReactiveProperty<bool> _isAlive = new ReactiveProperty<bool>(true);
        private readonly Subject<int> _damaged = new Subject<int>();

        private readonly ReactiveProperty<float> _currentSpeed = new ReactiveProperty<float>();
        private readonly ReactiveProperty<CharacterState> _state = new ReactiveProperty<CharacterState>(CharacterState.Idle);
        private readonly ReactiveProperty<bool> _isFacingRight = new ReactiveProperty<bool>(true);
        private readonly Subject<Unit> _attacked = new Subject<Unit>();

        private const float FacingThreshold = 0.35f;
        private const float HitStunDuration = 0.2f;
        private const float KnockBackSpeed = 4f;

        private readonly CharacterEquipment _equipment = new CharacterEquipment();

        private int _maxHP;
        private float _moveSpeed;
        private AttackProfile _bareHandsAttack;
        private float _attackCooldown;
        private Vector3 _moveDirection;
        private Vector3 _knockBackDirection;
        private float _hitStunRemain;

        public int MaxHP => _maxHP;
        public ReadOnlyReactiveProperty<int> CurrentHP => _currentHP;
        public ReadOnlyReactiveProperty<bool> IsAlive => _isAlive;
        public Observable<Character> Died => _isAlive.Where(isAlive => !isAlive).Select(this, (_, self) => self);
        public Observable<int> Damaged => _damaged;
        public ReadOnlyReactiveProperty<float> CurrentSpeed => _currentSpeed;
        public ReadOnlyReactiveProperty<CharacterState> State => _state;
        public ReadOnlyReactiveProperty<bool> IsFacingRight => _isFacingRight;

        /// <summary>장비 상태. 현재 무기를 리액티브로 노출한다. null 이면 맨손 — 맨손 공격 명세로 싸운다.</summary>
        public CharacterEquipment Equipment => _equipment;

        public Weapon CurrentWeapon => _equipment.CurrentWeapon.CurrentValue;
        public bool IsArmed => CurrentWeapon != null;

        public int AttackPower => CurrentAttack.Power;
        public float AttackRange => CurrentAttack.Range;
        public bool HasKnockbackAttack => CurrentAttack.HasKnockback;

        /// <summary>공격 모션 이름. 비어 있으면 기본 모션 — 무기별 모션은 이 값으로 맵핑한다 (지금은 전부 기본값).</summary>
        public string AttackAnimation => CurrentWeapon != null ? CurrentWeapon.AttackAnimation : string.Empty;

        private AttackProfile CurrentAttack => CurrentWeapon != null ? CurrentWeapon.Attack : _bareHandsAttack;
        public bool CanAttack => _isAlive.Value && _attackCooldown <= 0f;
        public Observable<Unit> Attacked => _attacked;

        public void Init(CharacterStats stats, AttackProfile bareHandsAttack)
        {
            _maxHP = stats.MaxHP;
            _moveSpeed = stats.MoveSpeed;
            _bareHandsAttack = bareHandsAttack;
            _equipment.Unequip();
            _currentHP.Value = _maxHP;
            _isAlive.Value = true;
        }

        public void Equip(Weapon weapon)
        {
            _equipment.Equip(weapon);
        }

        public void Unequip()
        {
            _equipment.Unequip();
        }

        public void Move(Vector3 direction)
        {
            _moveDirection = direction.normalized;

            if (Mathf.Abs(_moveDirection.x) >= FacingThreshold)
            {
                _isFacingRight.Value = _moveDirection.x > 0f;
            }
        }

        public void StopMove()
        {
            _moveDirection = Vector3.zero;
        }

        private void Update()
        {
            if (!_isAlive.Value)
            {
                return;
            }

            _attackCooldown -= Time.deltaTime;

            if (_hitStunRemain > 0f)
            {
                UpdateHitStun();
                return;
            }

            transform.position += _moveDirection * (_moveSpeed * Time.deltaTime);

            _currentSpeed.Value = _moveDirection.sqrMagnitude > 0f ? _moveSpeed : 0f;
            _state.Value = _currentSpeed.Value > 0f ? CharacterState.Walk : CharacterState.Idle;
        }

        private void UpdateHitStun()
        {
            _hitStunRemain -= Time.deltaTime;
            transform.position += _knockBackDirection * (KnockBackSpeed * Time.deltaTime);

            if (_hitStunRemain > 0f)
            {
                return;
            }

            _state.Value = _currentSpeed.Value > 0f ? CharacterState.Walk : CharacterState.Idle;
        }

        /// <summary>공격 동작만 낸다. 맞았는지는 부르는 쪽이 판정한다.</summary>
        public void Attack()
        {
            _attackCooldown = CurrentAttack.Interval;
            _attacked.OnNext(Unit.Default);
        }

        public void TakeDamage(int amount, Character attacker)
        {
            _currentHP.Value = Mathf.Max(_currentHP.Value - amount, 0);
            _isAlive.Value = _currentHP.Value > 0;

            if (_isAlive.Value)
            {
                BeginHitStun(attacker);
            }
            else
            {
                StopMove();
                _currentSpeed.Value = 0f;
                _state.Value = CharacterState.Death;
            }

            _damaged.OnNext(amount);
        }

        private void BeginHitStun(Character attacker)
        {
            _hitStunRemain = HitStunDuration;
            _currentSpeed.Value = 0f;
            _state.Value = CharacterState.Hit;

            // 밀려나는 것은 공격자의 평타에 넉백 능력이 있을 때만. 경직·피격 연출은 항상 나간다.
            if (attacker == null || !attacker.HasKnockbackAttack)
            {
                _knockBackDirection = Vector3.zero;
                return;
            }

            var away = transform.position - attacker.transform.position;
            away.y = 0f;
            _knockBackDirection = away.sqrMagnitude > 0f ? away.normalized : Vector3.zero;
        }

        protected virtual void OnDestroy()
        {
            _currentHP.Dispose();
            _isAlive.Dispose();
            _damaged.Dispose();
            _currentSpeed.Dispose();
            _state.Dispose();
            _isFacingRight.Dispose();
            _attacked.Dispose();
            _equipment.Dispose();
        }
    }
}
