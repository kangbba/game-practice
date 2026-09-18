using System;
using System.Collections.Generic;

namespace Sayne
{
    /// <summary>자리별 장비 ID 묶음. "스폰 때 입고 나올 한 벌" 선언이다. 빈 자리는 벗은 채로 시작한다.</summary>
    public class EquipmentIDs
    {
        private readonly string _mainHand;
        private readonly string _offHand;
        private readonly string _helmet;
        private readonly string _chest;
        private readonly string _greaves;
        private readonly string _boots;

        public EquipmentIDs(string mainHand = "", string offHand = "", string helmet = "", string chest = "",
            string greaves = "", string boots = "")
        {
            _mainHand = mainHand;
            _offHand = offHand;
            _helmet = helmet;
            _chest = chest;
            _greaves = greaves;
            _boots = boots;
        }

        /// <summary>이 한 벌에 실제로 들어 있는 ID 들. 빈 자리는 빠진다.</summary>
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
