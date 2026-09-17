namespace Sayne
{
    /// <summary>
    /// 장비 ID 선언 테이블. 값은 Assets/Game/Equipment 의 에셋 이름과 같다.
    /// 그림이 아직 없는 장비도 여기 선언한다 — 비주얼이 없으면 뼈에 아무것도 안 달 뿐, 스탯은 그대로 얹힌다.
    /// </summary>
    public static class EquipmentID
    {
        public static class Weapon
        {
            /// <summary>뼈에 아무것도 안 다는 무기. 무기 자리를 비우는 대신 이걸 낀다.</summary>
            public const string BareHands = "BareHands";

            public const string Scythe = "Scythe";
            public const string Sword = "Sword";
            public const string Staff = "Staff";

            /// <summary>적 무기. 그림은 아직 적 프리팹에 박혀 있어서 비주얼이 없다.</summary>
            public const string GoblinClub = "GoblinClub";
            public const string OgreClub = "OgreClub";
        }

        /// <summary>보조장비. 손에 드는 것 중 때리지 않는 것들 — 스탯만 얹는다.</summary>
        public static class OffHand
        {
            public const string WoodenShield = "WoodenShield";
            public const string TowerShield = "TowerShield";
            public const string BloodTalisman = "BloodTalisman";
        }

        /// <summary>몸을 덮는 방어구. 아직 비주얼 프리팹이 없어 스탯만 얹는다.</summary>
        public static class Armor
        {
            public const string LeatherHood = "LeatherHood";
            public const string IronHelm = "IronHelm";

            public const string LeatherArmor = "LeatherArmor";
            public const string PlateArmor = "PlateArmor";

            public const string IronGreaves = "IronGreaves";

            public const string TravelerBoots = "TravelerBoots";
            public const string SwiftBoots = "SwiftBoots";
        }
    }
}
