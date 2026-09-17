using System.Collections.Generic;

namespace Sayne
{
    /// <summary>장비 한 벌(장비 인포). 자리 → 파츠. 비어 있는 자리는 벗은 상태다.</summary>
    public class EquipmentSet
    {
        /// <summary>아무것도 안 걸친 한 벌. 프리팹 기본 상태와 같다.</summary>
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
