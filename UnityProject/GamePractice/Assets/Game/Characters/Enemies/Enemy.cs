namespace Sayne
{
    /// <summary>
    /// 싸우러 오는 캐릭터. 적끼리의 차이는 전부 설계값(EnemyData)에 있으므로 몸은 이 하나로 족하다.
    /// 누구인지도 설계값이 정한다 — 같은 프리팹을 쓰는 변종이 자기 이름을 말할 수 있어야 하기 때문이다.
    /// </summary>
    public class Enemy : Character
    {
        /// <summary>자기 설계값. 태어난 근거이자, 죽으면 뭘 흘릴 운명인지도 여기 적혀 있다.</summary>
        public EnemyData Data { get; private set; }

        public override string ID => Data.EnemyID;

        public bool IsBoss => Data.IsBoss;

        protected override string SortingLayer => SortingLayers.Enemy;
        protected override string UltimateSortingLayer => SortingLayers.UltimateEnemy;

        /// <summary>스폰 직후 한 번. 설계값을 쥐고 그대로 몸을 만든다. 장비 세트는 설계값의 EquipmentSet 으로 만든 걸 받는다.</summary>
        public void Init(EnemyData data, EquipmentSet equipment)
        {
            Data = data;
            base.Init(data.BaseStats, data.Skill, null, equipment);
        }
    }
}
