namespace Sayne
{
    /// <summary>
    /// 장비를 끼우는 자리. 머리·머리카락·망토처럼 그 캐릭터를 그 캐릭터로 만드는 것들은 여기 없다 —
    /// 그건 프리팹에 구워진 아바타지 갈아입는 장비가 아니다.
    /// 자리에 맞는 장비면 누가 입든 낄 수 있고, 그 자리에 낄 게 없으면 그냥 비어 있다.
    /// </summary>
    public enum EquipmentSlot
    {
        MainHand,
        OffHand,
        Helmet,
        Chest,
        Greaves,
        Boots
    }

    public static class EquipmentSlots
    {
        public static readonly EquipmentSlot[] All =
        {
            EquipmentSlot.MainHand,
            EquipmentSlot.OffHand,
            EquipmentSlot.Helmet,
            EquipmentSlot.Chest,
            EquipmentSlot.Greaves,
            EquipmentSlot.Boots
        };

        public static string DisplayName(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.MainHand => "주장비",
                EquipmentSlot.OffHand => "보조장비",
                EquipmentSlot.Helmet => "투구",
                EquipmentSlot.Chest => "몸통갑옷",
                EquipmentSlot.Greaves => "정강이",
                _ => "신발"
            };
        }
    }
}
