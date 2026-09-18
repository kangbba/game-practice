using UnityEngine;

namespace Sayne
{
    /// <summary>투구 프리팹 뿌리에 붙는 투구 그 자체. 지금은 부위만 밝힌다 — 투구만의 것이 생기면 여기에 둔다.</summary>
    public class Helmet : MonoBehaviour, IEquipment
    {
        public EquipmentSlot Slot => EquipmentSlot.Helmet;
    }
}
