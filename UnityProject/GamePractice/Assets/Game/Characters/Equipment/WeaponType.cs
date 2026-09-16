namespace Sayne
{
    /// <summary>무기 계열. 지금은 분류만 하고, 나중에 계열별 상태이상·특효가 여기에 붙는다.</summary>
    public enum WeaponType
    {
        Sword,
        Axe,
        Bow,
        Staff
    }

    public static class WeaponTypes
    {
        public static string DisplayName(WeaponType type)
        {
            return type switch
            {
                WeaponType.Sword => "검류",
                WeaponType.Axe => "도끼류",
                WeaponType.Bow => "활류",
                _ => "지팡이류"
            };
        }
    }
}
