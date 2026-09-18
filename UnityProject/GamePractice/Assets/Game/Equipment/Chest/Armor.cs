using UnityEngine;

namespace Sayne
{
    /// <summary>몸통 장비 프리팹 뿌리에 붙는 갑옷·옷 그 자체. 지금은 부위만 밝힌다 — 몸통만의 것이 생기면 여기에 둔다.</summary>
    public class Armor : MonoBehaviour, IEquipment
    {
        public EquipmentSlot Slot => EquipmentSlot.Chest;
    }
}
