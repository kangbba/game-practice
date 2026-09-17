namespace Sayne
{
    /// <summary>퀘스트 한 장의 선언. 무엇을 얼마나 해야 하는지와 그 대가만 적는다.</summary>
    public class QuestPlan
    {
        public readonly QuestType Type;
        public readonly string Title;

        /// <summary>이만큼 채우면 완료다. 레벨 퀘스트는 도달할 레벨, 처치 퀘스트는 잡을 마릿수.</summary>
        public readonly int Goal;

        /// <summary>처치 퀘스트가 노리는 적. 비어 있으면 아무 적이나 센다.</summary>
        public readonly string TargetEnemyID;

        /// <summary>화면에 쓸 대상 이름. 적 ID 를 그대로 보여줄 수는 없어서 따로 적는다.</summary>
        public readonly string TargetName;

        public readonly long GoldReward;

        private QuestPlan(QuestType type, string title, int goal, string targetEnemyID, string targetName,
            long goldReward)
        {
            Type = type;
            Title = title;
            Goal = goal;
            TargetEnemyID = targetEnemyID;
            TargetName = targetName;
            GoldReward = goldReward;
        }

        public static QuestPlan LevelReach(string title, int level, long goldReward)
        {
            return new QuestPlan(QuestType.LevelReach, title, level, string.Empty, string.Empty, goldReward);
        }

        /// <summary>적을 가리지 않는 처치 퀘스트. 아무거나 잡으면 센다.</summary>
        public static QuestPlan AnyKill(string title, int count, long goldReward)
        {
            return new QuestPlan(QuestType.EnemyKill, title, count, string.Empty, "적", goldReward);
        }

        public static QuestPlan EnemyKill(string title, string enemyID, string enemyName, int count, long goldReward)
        {
            return new QuestPlan(QuestType.EnemyKill, title, count, enemyID, enemyName, goldReward);
        }

        /// <summary>화면에 쓰는 할 일 한 줄. 선언에 적지 않고 종류에서 만든다.</summary>
        public string Description => Type == QuestType.LevelReach
            ? $"레벨 {Goal} 달성하기"
            : $"{TargetName} {Goal}마리 처치하기";
    }
}
