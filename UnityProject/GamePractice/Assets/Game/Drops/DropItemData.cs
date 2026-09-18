using System;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 적 설계값의 드랍 목록 한 줄 — 장비 한 점과 그 확률. 줄끼리는 독립이라 확률 하나가 다른 줄의 당첨을 막지 않는다.
    /// 골드·회복처럼 모든 적이 흔하게 떨구는 것은 여기 없다 — BaseDropPlan 이 따로 쥔다.
    /// 그림과 효과로 옮기는 건 DropDirector 의 일이고, 구슬을 뿌리는 DropManager 는 이걸 모른다.
    /// </summary>
    [Serializable]
    public class DropItemData
    {
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _equipmentID;

        [Range(0f, 1f)] [SerializeField] private float _chance = 0.03f;

        public string EquipmentID => _equipmentID;

        public float Chance => _chance;

        /// <summary>장비를 안 고른 줄은 굴려도 줄 게 없다.</summary>
        public bool IsValid => !string.IsNullOrEmpty(_equipmentID);
    }
}
