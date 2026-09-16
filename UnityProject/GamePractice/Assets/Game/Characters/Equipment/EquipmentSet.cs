using System.Collections.Generic;

namespace Sayne
{
    /// <summary>장비 한 벌(장비 인포). 부위 → 파츠. 비어 있는 부위는 벗은 상태다.</summary>
    public class EquipmentSet
    {
        /// <summary>아무것도 안 걸친 한 벌. 프리팹 기본 상태와 같다.</summary>
        public static readonly EquipmentSet Naked = new EquipmentSet();

        private readonly Dictionary<BodyPart, EquipmentPart> _parts = new Dictionary<BodyPart, EquipmentPart>();

        public EquipmentSet Put(EquipmentPart part)
        {
            _parts[part.BodyPart] = part;
            return this;
        }

        public EquipmentPart Get(BodyPart bodyPart)
        {
            return _parts.TryGetValue(bodyPart, out var part) ? part : null;
        }
    }
}
