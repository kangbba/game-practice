using System.Collections.Generic;

namespace Sayne
{
    /// <summary>스테이지 하나의 설계값. 웨이브를 순서대로 다 돌고 나면 보스가 나온다.</summary>
    public class StagePlan
    {
        /// <summary>순서대로 도는 웨이브. 한 장이 "적 ID 마다 몇 마리" 다.</summary>
        public readonly IReadOnlyList<Dictionary<string, int>> Waves;

        /// <summary>웨이브를 다 넘기면 마지막에 혼자 나오는 놈.</summary>
        public readonly string BossEnemyID;

        public StagePlan(IReadOnlyList<Dictionary<string, int>> waves, string bossEnemyID)
        {
            Waves = waves;
            BossEnemyID = bossEnemyID;
        }
    }
}
