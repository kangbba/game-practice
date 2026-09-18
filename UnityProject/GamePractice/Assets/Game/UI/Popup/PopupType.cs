using System;

namespace Sayne
{
    /// <summary>팝업 종류. 종류마다 프리팹이 하나씩 짝지어 있다 — 짝과 성질은 PopupTypes 에 적는다.</summary>
    public enum PopupType
    {
        Growth,
        Equipment,
    }

    public static class PopupTypes
    {
        public static readonly PopupType[] All = { PopupType.Growth, PopupType.Equipment };

        /// <summary>짝지은 프리팹의 어드레서블 주소.</summary>
        public static string GetAddress(PopupType type)
        {
            return type switch
            {
                PopupType.Growth => AssetAddresses.GrowthWindow,
                PopupType.Equipment => AssetAddresses.EquipmentWindow,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        /// <summary>열 때 뒤 화면을 흐려서 깔지. 아니면 옅은 암막만 깔린다.</summary>
        public static bool IsBlurred(PopupType type)
        {
            return type switch
            {
                PopupType.Growth => true,
                PopupType.Equipment => true,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
