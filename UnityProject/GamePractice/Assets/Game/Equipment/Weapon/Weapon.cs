using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 무기 프리팹 뿌리에 붙는 무기 그 자체. 붙은 클래스가 곧 무기 종류다(근접 MeleeWeapon, 활 Bow).
    /// 특수 무기는 종류 클래스를 상속해 궁극기만 덮어쓰고 그 프리팹에 붙이면 된다.
    ///
    /// 설계값(EquipmentPlan)과 나눠 든다 — 거기는 어느 부위든 같은 아이템 카드(ID·이름·설명·얹는 스탯),
    /// 여기는 무기로서의 것 전부(쥐는 법, 평타 방식, 궁극기와 그 수치). 궁극기 수치는 궁극기를 어떻게 굽느냐와 떨어질 수 없다 —
    /// 같은 배수라도 활은 발수로 나눠 쏘고 근접은 판정마다 전부 때린다. 무기 위력은 카드의 공격력 스탯이다.
    /// 맨손도 무기다 — 그림 없는 근접 무기 프리팹이다.
    ///
    /// 그림 규격:
    /// · 원점 = 쥐는 점(Grip). 캐릭터 소켓(WeaponMount)에 붙으면 손이 곧 여기다.
    /// · +Y = 무기가 겨누는 쪽(Tip). 칼·몽둥이는 날 끝, 활은 화살이 나가는 쪽(시위 반대편)이다 — 그래야 활대가 팔과 직각으로 선다.
    /// · 쥔 손에서 끝까지 = 표준 길이 × 배수. 모든 무기는 배수 1 이고, 일부러 큰 무기만 올린다.
    /// 쥐는 점과 끝은 그림(Art)의 자식으로 찍어 둔 두 점이다 — 프리팹에서 그림 위에 끌어다 놓으면 된다.
    /// </summary>
    public abstract class Weapon : MonoBehaviour, IEquipment
    {
        /// <summary>맞은 쪽에 거는 상태이상 한 줄. Debuff 는 읽기전용 구조체라 직렬화용 껍데기를 따로 둔다.</summary>
        [Serializable]
        private class DebuffEntry
        {
            [SerializeField] private DebuffType _type;
            [SerializeField] private float _seconds = 1f;

            public Debuff ToDebuff() => new Debuff(_type, _seconds);
        }

        /// <summary>모든 무기의 기본 길이. 쥔 손에서 끝까지, 소켓 단위. 켄의 검이 기준이다.</summary>
        public const float StandardLength = 1.2f;

        [Header("평타")]
        [SerializeField] private float _range = 2f;

        /// <summary>평타 묶음이 몇 타인가. 모션이 모자라면 앞으로 되감아 쓴다.</summary>
        [SerializeField] private int _comboCount = 4;

        /// <summary>묶음 안에서 다음 타까지의 간격. 짧아야 콤보가 이어진 느낌이 난다.</summary>
        [SerializeField] private float _comboInterval = 0.35f;

        /// <summary>묶음을 마친 뒤 다음 묶음까지의 간격.</summary>
        [SerializeField] private float _cycleInterval = 1.6f;

        [SerializeField] private DebuffEntry[] _hitDebuffs = Array.Empty<DebuffEntry>();

        [Header("그림 — 맨손처럼 그림 없는 무기는 비워 둔다")]
        [SerializeField] private Transform _art;
        [SerializeField] private Transform _grip;
        [SerializeField] private Transform _tip;

        /// <summary>표준 길이에 곱하는 배수. 두 손 무기처럼 일부러 큰 것만 1 이 아니다.</summary>
        [SerializeField] private float _sizeScale = 1f;

        [Header("궁극기 — 이름이 비어 있으면 이 무기로는 궁극기를 못 쓴다")]
        [SerializeField] private string _ultimateName;
        [SerializeField] private float _ultimatePowerMultiplier = 3f;
        [SerializeField] private float _ultimateCooldown = 30f;

        /// <summary>궁극기 전용 연출. 비우면 평타와 같은 베기 연출을 쓴다.</summary>
        [ParticleIDPicker(allowEmpty: true)] [SerializeField] private string _ultimateParticleID;

        private Debuff[] _debuffs;

        public EquipmentSlot Slot => EquipmentSlot.MainHand;

        public float Range => _range;
        public int ComboCount => _comboCount;
        public float ComboInterval => _comboInterval;
        public float CycleInterval => _cycleInterval;

        /// <summary>이 무기에 맞은 쪽에 거는 상태이상들. 맞을 때마다 읽으므로 처음 한 번만 만든다.</summary>
        public IReadOnlyList<Debuff> HitDebuffs => _debuffs ??= Array.ConvertAll(_hitDebuffs, entry => entry.ToDebuff());

        /// <summary>끝점. 휘두른 궤적이 이 선 위에 놓인다.</summary>
        public Transform Tip => _tip;

        /// <summary>평타가 쏘는 투사체. 쏘지 않는 무기면 null — 맞히는 순간 그 자리에서 판정한다.</summary>
        public virtual Projectile Projectile => null;

        /// <summary>
        /// 쏘는 무기인가. 투사체를 가졌으면 쏜다 — 때리는 순간 투사체를 쏘고, 판정은 투사체가 닿을 때로 밀린다.
        /// 사거리가 길다고 원거리가 아니다.
        /// </summary>
        public bool IsRanged => Projectile != null;

        /// <summary>궁극기 전용 연출. 없으면 null.</summary>
        public string UltimateParticleID => string.IsNullOrEmpty(_ultimateParticleID) ? null : _ultimateParticleID;

        /// <summary>이 종류의 궁극기가 트는 영웅 모션(애니메이터 상태 이름).</summary>
        protected abstract string UltimateAnimation { get; }

        /// <summary>이 무기를 들었을 때의 궁극기 기술. 없으면 null. 장착할 때 한 번 만들어 쥐어 둔다.</summary>
        public CharacterSkill CreateUltimate()
        {
            return string.IsNullOrEmpty(_ultimateName)
                ? null
                : new CharacterSkill(_ultimateName, UltimateAnimation, _ultimatePowerMultiplier, _ultimateCooldown);
        }

        /// <summary>
        /// 궁극기 본편. 캐릭터 손에 붙은 이 무기가 굽는다. 연출의 뼈대(컷씬 → 무대 → 본편 → 여운 → 쓰러짐 → 복귀)는
        /// UltimateDirector 가 쥐고, 무기는 받은 무대 위에서 때리기만 한다. 끝나면 연출기가 여운으로 넘어간다.
        /// </summary>
        public abstract UniTask PlayUltimateAsync(UltimateStage stage, CancellationToken token);

        private void Awake()
        {
            Arrange();
        }

#if UNITY_EDITOR
        /// <summary>프리팹에서 점을 옮기거나 배수를 바꾸면 그 자리에서 다시 세워 보인다. OnValidate 안에서는 트랜스폼을 못 바꿔 한 박자 미룬다.</summary>
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                // 미뤄진 사이에 프리팹을 닫았으면 이미 없는 컴포넌트다.
                if (this != null)
                {
                    Arrange();
                }
            };
        }
#endif

        /// <summary>그림을 돌리고 줄이고 옮겨, 쥐는 점이 원점에 오고 끝이 +Y 로 표준 길이만큼 뻗게 한다.</summary>
        public void Arrange()
        {
            // 맨손은 그릴 게 없다.
            if (_art == null)
            {
                return;
            }

            var grip = (Vector2)_grip.localPosition;
            var along = (Vector2)_tip.localPosition - grip;

            var scale = StandardLength * _sizeScale / along.magnitude;
            var rotation = Quaternion.Euler(0f, 0f, 90f - Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);

            _art.localRotation = rotation;
            _art.localScale = Vector3.one * scale;
            _art.localPosition = -(rotation * (grip * scale));
        }
    }
}
