namespace Sayne
{
    /// <summary>
    /// 플레이어가 굴리는 캐릭터. 히어로끼리의 차이는 전부 설계값(HeroPlan)과 낀 무기에 있으므로 몸은 이 하나로 족하다.
    /// 누구인지도 설계값이 정한다 — 적과 같은 규칙이다.
    /// </summary>
    public class Hero : Character
    {
        /// <summary>자기 설계값. 태어난 근거다.</summary>
        public HeroPlan Plan { get; private set; }

        public override string ID => Plan.HeroID;

        protected override string SortingLayer => SortingLayers.Hero;
        protected override string UltimateSortingLayer => SortingLayers.UltimateHero;

        /// <summary>스폰 직후 한 번. 몸을 만드는 Init 보다 먼저 받아 둔다.</summary>
        public void SetPlan(HeroPlan plan)
        {
            Plan = plan;
        }
    }
}
