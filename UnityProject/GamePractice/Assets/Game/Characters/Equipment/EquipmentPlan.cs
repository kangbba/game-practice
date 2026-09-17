using System;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 장비 하나의 설계값 전부. 어디에 끼고, 뭐라 불리고, 스탯을 뭘 몇 올리고, 무기면 어떻게 싸우는가.
    /// 주인은 파일명이 아니라 _equipmentID 필드다 — 같은 이름의 비주얼 프리팹이 따로 있기 때문이다.
    /// 비주얼이 없는 장비도 설계값은 있다. 뼈에 아무것도 안 달 뿐, 스탯은 그대로 얹힌다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Equipment Plan", fileName = "EquipmentPlan")]
    public class EquipmentPlan : ScriptableObject
    {
        /// <summary>맞은 쪽에 거는 상태이상 한 줄. HitEffect 는 읽기전용 구조체라 직렬화용 껍데기를 따로 둔다.</summary>
        [Serializable]
        private class HitEffectEntry
        {
            [SerializeField] private Debuff _debuff;
            [SerializeField] private float _seconds = 1f;

            public HitEffect Effect => new HitEffect(_debuff, _seconds);
        }

        [EquipmentIDPicker] [SerializeField] private string _equipmentID;

        /// <summary>끼우는 자리. 주장비면 무기로 취급한다.</summary>
        [SerializeField] private EquipmentSlot _slot;

        [Header("장비창 표시")]
        [SerializeField] private string _displayName;
        [TextArea(2, 4)] [SerializeField] private string _description;

        [Header("얹는 스탯")]
        [SerializeField] private int _maxHP;
        [SerializeField] private float _moveSpeed;

        /// <summary>공격력. 무기면 이 값이 곧 무기 위력이다 — 위력과 스탯을 두 벌로 적지 않는다.</summary>
        [SerializeField] private int _attackPower;

        [Header("무기일 때만 쓰는 값")]
        [SerializeField] private WeaponType _weaponType;
        [SerializeField] private float _range = 2f;

        /// <summary>묶음 안에서 다음 타까지의 간격.</summary>
        [SerializeField] private float _comboInterval = 0.35f;

        /// <summary>묶음을 마친 뒤 다음 묶음까지의 간격.</summary>
        [SerializeField] private float _cycleInterval = 1.6f;

        [SerializeField] private bool _useTrail;
        [SerializeField] private HitEffectEntry[] _hitEffects = Array.Empty<HitEffectEntry>();

        /// <summary>이 설계값의 주인.</summary>
        public string EquipmentID => _equipmentID;

        public EquipmentSlot Slot => _slot;

        /// <summary>장비창에 보여줄 이름. 안 적었으면 ID 를 그대로 보여준다.</summary>
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? _equipmentID : _displayName;

        public string Description => _description;

        /// <summary>이 장비가 얹는 스탯. 최종 스탯의 "장비" 근원으로 합류한다.</summary>
        public CharacterStats Stats => new CharacterStats(_maxHP, _moveSpeed, _attackPower);

        /// <summary>주장비 자리에 끼는 것은 전부 무기다 — 맨손도 포함해서.</summary>
        public bool IsWeapon => _slot == EquipmentSlot.MainHand;

        /// <summary>무기 한 자루의 싸움 방식. 위력은 스탯의 공격력을 그대로 쓴다.</summary>
        public WeaponInfo Weapon => new WeaponInfo(_weaponType, _attackPower, _range, _comboInterval,
            _cycleInterval, _useTrail, BuildHitEffects());

        private HitEffect[] BuildHitEffects()
        {
            var effects = new HitEffect[_hitEffects.Length];

            for (var i = 0; i < _hitEffects.Length; i++)
            {
                effects[i] = _hitEffects[i].Effect;
            }

            return effects;
        }
    }
}
