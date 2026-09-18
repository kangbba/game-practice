namespace Sayne
{
    /// <summary>
    /// 모든 적이 죽을 때 굴리는 기본 드랍 — 골드와 회복 구슬. 흔하게 떨어진다.
    /// 적 설계값의 드랍 목록(장비, 드물게)과 따로 논다. 여기 숫자만 고치면 모든 적에 한꺼번에 먹는다.
    /// 보스는 굴리지 않고 둘 다 반드시 떨군다.
    /// </summary>
    public static class BaseDropPlan
    {
        public const float GoldChance = 0.8f;
        public const long GoldAmount = 30;

        /// <summary>보스 골드는 한 번에 이만큼 곱절이다.</summary>
        public const int BossGoldMultiplier = 5;

        public const float HealChance = 0.3f;

        /// <summary>회복 구슬 하나가 채우는 양. 주운 영웅의 최대 체력에 대한 비율이다.</summary>
        public const float HealRatio = 0.15f;
    }
}
