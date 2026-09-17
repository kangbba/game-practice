namespace Sayne
{
    /// <summary>
    /// 장비 ID 선언 테이블. 값은 Assets/Game/Equipment 의 에셋 이름과 같다.
    /// 영웅이 입는 장비는 전부 몸에 입히는 그림(비주얼 프리팹)이 있다. 그림 없이 스탯만 얹는 건 맨손과 적 무기뿐이다.
    /// </summary>
    public static class EquipmentID
    {
        public static class Weapon
        {
            /// <summary>뼈에 아무것도 안 다는 무기. 무기를 벗으면 이게 든다 — 가방에 드는 아이템이 아니고, 장비창엔 빈 자리로 보인다.</summary>
            public const string BareHands = "BareHands";

            public const string Scythe = "Scythe";
            public const string Sword = "Sword";
            public const string Staff = "Staff";

            /// <summary>원거리류. 투사체를 쏜다.</summary>
            public const string HuntingBow = "HuntingBow";
            public const string LongBow = "LongBow";
            public const string FrostBow = "FrostBow";

            /// <summary>적 무기. 그림은 아직 적 프리팹에 박혀 있어서 비주얼이 없다.</summary>
            public const string GoblinClub = "GoblinClub";
            public const string OgreClub = "OgreClub";
        }

        /// <summary>
        /// 몸에 걸치는 장비. 영웅이 태어날 때 입고 있는 차림도 전부 여기 든 장비다 — 누구 것이든 서로 바꿔 입는다.
        /// 몸에 입히는 그림이 없는 장비는 만들지 않는다.
        /// </summary>
        public static class Armor
        {
            public const string IronHelm = "IronHelm";

            public const string AldricCoat = "AldricCoat";
            public const string KageArmor = "KageArmor";
            public const string NyxDress = "NyxDress";

            public const string AldricBoots = "AldricBoots";
            public const string KageBoots = "KageBoots";
            public const string NyxBoots = "NyxBoots";
        }
    }
}
