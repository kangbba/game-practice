namespace Sayne
{
    /// <summary>드랍 하나가 주는 것. 장비 한 점이거나 골드 한 뭉치다.</summary>
    public enum DropType
    {
        Equipment,
        Gold
    }

    /// <summary>굴려서 당첨된 전리품 하나. 무엇을 줄지는 정해졌고, 어떻게 건네줄지는 받는 쪽이 정한다.</summary>
    public readonly struct DropReward
    {
        public DropType Type { get; }

        /// <summary>장비 드랍일 때만 채워진다.</summary>
        public string EquipmentID { get; }

        /// <summary>골드 드랍일 때만 채워진다.</summary>
        public long GoldAmount { get; }

        public DropReward(DropType type, string equipmentID, long goldAmount)
        {
            Type = type;
            EquipmentID = equipmentID;
            GoldAmount = goldAmount;
        }
    }
}
