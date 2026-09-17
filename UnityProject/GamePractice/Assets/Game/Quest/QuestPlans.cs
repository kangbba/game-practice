using System.Collections.Generic;

namespace Sayne
{
    /// <summary>퀘스트 선언 목록. 위에서부터 차례로 하나씩 준다 — 이 순서가 곧 몇 번째 퀘스트인지다.</summary>
    public static class QuestPlans
    {
        private static readonly List<QuestPlan> All = new List<QuestPlan>
        {
            QuestPlan.AnyKill("첫 사냥", count: 1, goldReward: 50),
            QuestPlan.LevelReach("첫 성장", level: 2, goldReward: 100),
            QuestPlan.EnemyKill("고블린 사냥꾼", EnemyID.Goblin, "고블린", count: 10, goldReward: 300),
            QuestPlan.LevelReach("무럭무럭", level: 3, goldReward: 200),
            QuestPlan.EnemyKill("오우거 학살자", EnemyID.Ogre, "오우거", count: 5, goldReward: 500),
            QuestPlan.LevelReach("한 사람 몫", level: 5, goldReward: 500),
            QuestPlan.AnyKill("백 번의 사냥", count: 100, goldReward: 1500),
            QuestPlan.LevelReach("어엿한 모험가", level: 8, goldReward: 1000),
        };

        public static int Count => All.Count;

        /// <summary>몇 번째 퀘스트. 목록을 다 끝냈으면 null — 더 줄 게 없다는 뜻이다.</summary>
        public static QuestPlan Get(int index)
        {
            return index < All.Count ? All[index] : null;
        }
    }
}
