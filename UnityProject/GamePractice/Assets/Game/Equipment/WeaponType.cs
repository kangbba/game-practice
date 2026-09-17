namespace Sayne
{
    /// <summary>
    /// 무기 계열. 계열이 곧 때리는 방식이다 — 원거리류만 투사체를 쏘고, 나머지는 때리는 순간 그 자리에서 판정한다.
    /// 지팡이류는 나중에 마법으로 갈라질 자리다. 그때까지는 근접과 같은 즉시 타격이다.
    /// 값의 순서는 설계값 에셋에 숫자로 박혀 있으니 중간에 끼워 넣지 말 것.
    /// </summary>
    public enum WeaponType
    {
        /// <summary>검·낫처럼 날로 베는 것. 맨손도 여기다.</summary>
        Blade,

        /// <summary>곤봉·몽둥이처럼 휘둘러 찍는 것.</summary>
        Blunt,

        /// <summary>투사체를 쏘는 무기. 활이 대표지만 총이든 투척이든 날려 보내는 건 전부 여기다.</summary>
        Ranged,

        Staff
    }

    public static class WeaponTypes
    {
        public static string DisplayName(WeaponType type)
        {
            return type switch
            {
                WeaponType.Blade => "날붙이류",
                WeaponType.Blunt => "둔기류",
                WeaponType.Ranged => "원거리류",
                _ => "지팡이류"
            };
        }
    }
}
