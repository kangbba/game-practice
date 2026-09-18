using System;
using UnityEngine;

namespace Sayne
{
    /// <summary>드랍 하나가 주는 것. 장비 한 점이거나 골드 한 뭉치다.</summary>
    public enum DropType
    {
        Equipment,
        Gold
    }

    /// <summary>
    /// 죽을 때 굴리는 항목 하나. 항목끼리는 독립이라 확률 하나가 다른 항목의 당첨을 막지 않는다.
    /// "무엇을 줄지" 를 선언하는 곳은 여기 하나뿐이다 — 설계값(ScriptableObject)에 적히는 데이터라
    /// 콜백이 아니라 종류와 수치로 적는다. 그걸 그림과 효과로 옮기는 건 DropDirector 의 일이고,
    /// 이 모양은 거기서 끝난다 — 구슬을 뿌리는 DropManager 는 이걸 모른다.
    /// </summary>
    [Serializable]
    public class DropItemData
    {
        [SerializeField] private DropType _type;

        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _equipmentID;

        [SerializeField] private long _goldAmount;

        [Range(0f, 1f)] [SerializeField] private float _chance = 0.1f;

        public DropType Type => _type;

        /// <summary>장비 드랍일 때만 채워진다.</summary>
        public string EquipmentID => _equipmentID;

        /// <summary>골드 드랍일 때만 채워진다.</summary>
        public long GoldAmount => _goldAmount;

        public float Chance => _chance;

        /// <summary>장비 드랍인데 장비를 안 고른 줄은 굴려도 줄 게 없다.</summary>
        public bool IsValid => _type != DropType.Equipment || !string.IsNullOrEmpty(_equipmentID);
    }
}
