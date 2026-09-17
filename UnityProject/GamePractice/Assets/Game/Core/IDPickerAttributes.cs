using System;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 문자열 ID 필드를 인스펙터에서 드롭다운으로 고르게 한다.
    /// 저장되는 값은 그대로 string 이라 직렬화·에셋 이름 대조가 깨지지 않고,
    /// 고르는 순간에는 enum 처럼 목록에서만 집을 수 있어 오타가 나지 않는다.
    /// 목록의 출처는 SourceType 의 public const string 들이다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class IDPickerAttribute : PropertyAttribute
    {
        public Type SourceType { get; }

        /// <summary>빈 문자열도 고를 수 있게 할지. 비우면 기본값으로 떨어지는 필드에 쓴다.</summary>
        public bool AllowEmpty { get; }

        public IDPickerAttribute(Type sourceType, bool allowEmpty = false)
        {
            SourceType = sourceType;
            AllowEmpty = allowEmpty;
        }
    }

    /// <summary>무기 ID 드롭다운. 목록은 EquipmentID.Weapon 의 상수들.</summary>
    public class WeaponIDPickerAttribute : IDPickerAttribute
    {
        public WeaponIDPickerAttribute(bool allowEmpty = false) : base(typeof(EquipmentID.Weapon), allowEmpty)
        {
        }
    }

    /// <summary>장비 ID 드롭다운. 목록은 EquipmentID 아래 모든 자리의 상수들.</summary>
    public class EquipmentIDPickerAttribute : IDPickerAttribute
    {
        public EquipmentIDPickerAttribute(bool allowEmpty = false) : base(typeof(EquipmentID), allowEmpty)
        {
        }
    }

    /// <summary>적 ID 드롭다운. 목록은 EnemyID 의 상수들.</summary>
    public class EnemyIDPickerAttribute : IDPickerAttribute
    {
        public EnemyIDPickerAttribute(bool allowEmpty = false) : base(typeof(EnemyID), allowEmpty)
        {
        }
    }

    /// <summary>히어로 ID 드롭다운. 목록은 HeroID 의 상수들.</summary>
    public class HeroIDPickerAttribute : IDPickerAttribute
    {
        public HeroIDPickerAttribute(bool allowEmpty = false) : base(typeof(HeroID), allowEmpty)
        {
        }
    }

    /// <summary>파티클 ID 드롭다운. 목록은 ParticleID 아래 모든 상수들.</summary>
    public class ParticleIDPickerAttribute : IDPickerAttribute
    {
        public ParticleIDPickerAttribute(bool allowEmpty = false) : base(typeof(ParticleID), allowEmpty)
        {
        }
    }
}
