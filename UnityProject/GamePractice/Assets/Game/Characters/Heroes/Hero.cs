namespace Sayne
{
    /// <summary>
    /// 플레이어가 굴리는 캐릭터. 히어로끼리의 차이는 전부 설계값(HeroData)과 낀 무기에 있으므로 몸은 이 하나로 족하다.
    /// 누구인지도 설계값이 정한다 — 적과 같은 규칙이다.
    /// </summary>
    public class Hero : Character
    {
        /// <summary>자기 설계값. 태어난 근거다.</summary>
        public HeroData Data { get; private set; }

        public override string ID => Data.HeroID;

        protected override string SortingLayer => SortingLayers.Hero;
        protected override string UltimateSortingLayer => SortingLayers.UltimateHero;

        /// <summary>스폰 직후 한 번. 설계값을 쥐고 그대로 몸을 만든다. 장비 세트는 설계값의 EquipmentSet 으로 만든 걸 받는다.</summary>
        public void Init(HeroData data, EquipmentSet equipment)
        {
            Data = data;
            base.Init(data.BaseStats, data.Skill, equipment);
        }
    }
}
