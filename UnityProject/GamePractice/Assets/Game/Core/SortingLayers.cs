namespace Sayne
{
    /// <summary>
    /// 정렬 레이어 이름. 순서는 프로젝트 설정 그대로다 — 아래로 갈수록 위에 그려진다.
    /// Default(맵·소품·투사체·드롭·일반 파티클) · Enemy · Hero · UltimateBackground · UltimateEnemy · UltimateHero · UltimateEffect
    /// 궁극기 무대는 평소 레이어 앞에 Ultimate 를 붙인 짝으로 올라간다.
    /// </summary>
    public static class SortingLayers
    {
        public const string Enemy = "Enemy";
        public const string Hero = "Hero";

        /// <summary>궁극기 백그라운드. 이 아래는 전부 덮인다.</summary>
        public const string UltimateBackground = "UltimateBackground";

        public const string UltimateEnemy = "UltimateEnemy";
        public const string UltimateHero = "UltimateHero";

        /// <summary>궁극기 무대 위에서 터지는 이펙트. 무대에 오른 캐릭터를 덮는다.</summary>
        public const string UltimateEffect = "UltimateEffect";
    }
}
