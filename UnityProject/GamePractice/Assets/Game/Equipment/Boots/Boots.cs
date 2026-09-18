using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 신발 프리팹 뿌리에 붙는 신발 한 켤레. 앞발·뒷발 그림을 자식(Front·Rear)으로 나눠 든다.
    /// 캐릭터의 발 자리는 자기가 어느 발인지만 알고, 그 발의 그림을 고르는 건 신발이 한다.
    /// </summary>
    public class Boots : MonoBehaviour, IEquipment
    {
        public const string FrontSide = "Front";
        public const string RearSide = "Rear";

        public EquipmentSlot Slot => EquipmentSlot.Boots;

        /// <summary>그 발의 그림만 남긴다. 한 켤레가 두 발 자리에 각각 붙으므로 자리마다 다른 쪽을 남긴다.</summary>
        public void ShowSide(string side)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(child.name == side);
            }
        }
    }
}
