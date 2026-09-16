namespace Sayne
{
    /// <summary>장비 ID 선언 테이블. 값은 Assets/Game/Equipment 의 에셋 이름과 같다.</summary>
    public static class EquipmentID
    {
        public static class Weapon
        {
            /// <summary>뼈에 아무것도 안 다는 무기. 무기 부위를 비우는 대신 이걸 낀다.</summary>
            public const string BareHands = "BareHands";

            public const string Scythe = "Scythe";
            public const string Sword = "Sword";
            public const string Staff = "Staff";

            /// <summary>적 무기. 그림은 아직 적 프리팹에 박혀 있어서 비주얼이 없다.</summary>
            public const string GoblinClub = "GoblinClub";
            public const string OgreClub = "OgreClub";
        }

        public static class Head
        {
            public const string FoxMask = "FoxMaskHead";
            public const string Silver = "SilverHead";
            public const string WhiteTwin = "WhiteTwinHead";
        }

        public static class Hair
        {
            public const string Silver = "SilverHair";
            public const string White = "WhiteHair";
        }

        /// <summary>망토. 누구나 낄 수 있다 — 이름에 주인이 없는 이유다.</summary>
        public static class Back
        {
            public const string Navy = "NavyCape";
            public const string Black = "BlackCape";
            public const string Violet = "VioletCape";
        }

        public static class Neck
        {
            public const string Crimson = "CrimsonScarf";
        }
    }
}
