using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 신발 한 켤레. 장비로서는 하나이고 밖에서는 한 세트로 입힌다 — 짝짝이로 신는 경우는 없다.
    /// 다만 리그의 다리 본이 둘이라(RightLeg·LeftLeg) 그림은 그 둘에 각각 붙어야 한다.
    /// 그래서 한 켤레 프리팹이 두 장을 자식으로 들고, 붙은 자리에 맞는 쪽만 남긴다.
    ///
    /// Front·Rear 는 본 이름이 아니라 아트 파일명 규칙이다 — 아트팩이 그렇게 내보냈고, PNG 파일명과
    /// 치수표 키가 이 글자에 묶여 있다. 실제 본은 RightLeg·LeftLeg 이고, 둘을 잇는 건
    /// EquipmentCatalogBuilder.LegBone 한 곳뿐이다.
    /// </summary>
    public class Boots : MonoBehaviour, IEquipment
    {
        /// <summary>오른다리에 붙는 쪽. 아트 파일명이 _Front 다.</summary>
        public const string FrontSide = "Front";

        /// <summary>왼다리에 붙는 쪽. 아트 파일명이 _Rear 다.</summary>
        public const string RearSide = "Rear";

        public EquipmentSlot Slot => EquipmentSlot.Boots;

        /// <summary>그쪽 그림만 남긴다. 한 켤레가 두 다리 자리에 각각 붙으므로 자리마다 다른 쪽을 남긴다.</summary>
        public void ShowSide(string side)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(child.name == side);
            }
        }
    }
}
