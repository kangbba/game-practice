using System.Collections.Generic;

namespace Sayne
{
    /// <summary>장비 세트. 부위 → 장비 조각. 비어 있는 부위는 장착하지 않은 상태다.</summary>
    public class EquipmentSet
    {
        /// <summary>아무것도 장착하지 않은 장비 세트. 프리팹 기본 상태(맨몸)와 같다.</summary>
        public static readonly EquipmentSet Naked = new EquipmentSet();

        private readonly Dictionary<EquipmentSlot, EquipmentPart> _parts = new Dictionary<EquipmentSlot, EquipmentPart>();

        public EquipmentSet Put(EquipmentPart part)
        {
            _parts[part.Slot] = part;
            return this;
        }

        public EquipmentPart Get(EquipmentSlot slot)
        {
            return _parts.TryGetValue(slot, out var part) ? part : null;
        }
    }
}
