namespace Sayne
{
    /// <summary>
    /// 싸우러 오는 캐릭터. 적끼리의 차이는 전부 설계값(EnemyPlan)에 있으므로 몸은 이 하나로 족하다.
    /// 누구인지도 설계값이 정한다 — 같은 프리팹을 쓰는 변종이 자기 이름을 말할 수 있어야 하기 때문이다.
    /// </summary>
    public class Enemy : Character
    {
        /// <summary>자기 설계값. 태어난 근거이자, 죽으면 뭘 흘릴 운명인지도 여기 적혀 있다.</summary>
        public EnemyPlan Plan { get; private set; }

        public override string ID => Plan.EnemyID;

        public bool IsBoss => Plan.IsBoss;

        protected override string SortingLayer => SortingLayers.Enemy;
        protected override string UltimateSortingLayer => SortingLayers.UltimateEnemy;

        /// <summary>스폰 직후 한 번. 몸을 만드는 Init 보다 먼저 받아 둔다.</summary>
        public void SetPlan(EnemyPlan plan)
        {
            Plan = plan;
        }
    }
}
