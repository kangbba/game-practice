namespace Sayne
{
    /// <summary>
    /// 상태이상. 특수한 효과가 걸어주는 것들이다.
    /// 맞으면 누구나 잠깐 움찔하는 평타경직(Character.Stagger)은 여기 들어가지 않는다.
    /// </summary>
    public enum DebuffType
    {
        /// <summary>기절. 한참 아무것도 못 한다.</summary>
        Stun,

        /// <summary>빙결. 얼어붙어 움직이지도 때리지도 못한다.</summary>
        Freeze,

        /// <summary>속박. 이동만 막고 공격은 된다.</summary>
        Root,

        /// <summary>침묵. 기술만 막고 이동은 된다.</summary>
        Silence,

        /// <summary>둔화. 이동 속도가 깎인다.</summary>
        Slow,

        /// <summary>중독. 시간에 따라 피해를 받는다.</summary>
        Poison,
    }

    public static class DebuffTypes
    {
        public static readonly DebuffType[] All =
        {
            DebuffType.Stun,
            DebuffType.Freeze,
            DebuffType.Root,
            DebuffType.Silence,
            DebuffType.Slow,
            DebuffType.Poison,
        };

        public static string DisplayName(DebuffType debuff)
        {
            return debuff switch
            {
                DebuffType.Stun => "기절",
                DebuffType.Freeze => "빙결",
                DebuffType.Root => "속박",
                DebuffType.Silence => "침묵",
                DebuffType.Slow => "둔화",
                _ => "중독"
            };
        }
    }
}
