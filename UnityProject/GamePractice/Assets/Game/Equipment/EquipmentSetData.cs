using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 장비 세트(EquipmentSet)의 저장 형태 — 부위별 장비 ID 묶음이다. 설계값 에셋(HeroData·EnemyData)이 시작 장비 세트로 들고 있다.
    /// 빈 부위는 장착하지 않고 시작한다. EquipmentManager.CreateSet 이 이걸 실제로 입힐 EquipmentSet 으로 만든다.
    /// </summary>
    [Serializable]
    public class EquipmentSetData
    {
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _mainHand;
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _offHand;
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _helmet;
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _chest;
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _greaves;
        [EquipmentIDPicker(allowEmpty: true)] [SerializeField] private string _boots;

        /// <summary>이 장비 세트에 실제로 들어 있는 ID 들. 빈 부위는 빠진다.</summary>
        public IEnumerable<string> All()
        {
            foreach (var slot in EquipmentSlots.All)
            {
                var id = Get(slot);
                if (!string.IsNullOrEmpty(id))
                {
                    yield return id;
                }
            }
        }

        public string Get(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.MainHand => _mainHand,
                EquipmentSlot.OffHand => _offHand,
                EquipmentSlot.Helmet => _helmet,
                EquipmentSlot.Chest => _chest,
                EquipmentSlot.Greaves => _greaves,
                EquipmentSlot.Boots => _boots,
                _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
            };
        }
    }
}
