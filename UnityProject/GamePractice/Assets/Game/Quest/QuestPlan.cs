namespace Sayne
{
    /// <summary>
    /// 퀘스트 한 장의 선언. "어떤 기록이 얼마가 되면 얼마를 준다" 한 문장뿐이다 —
    /// 그래서 새 종류의 퀘스트는 RecordManager 가 그걸 세기 시작하면 그것으로 끝이다.
    /// </summary>
    public class QuestPlan
    {
        /// <summary>어떤 기록을 보는가.</summary>
        public readonly RecordType Type;

        /// <summary>기록을 찾는 열쇠. 처치 퀘스트의 적 ID 같은 것 — 비어 있으면 그 종류의 통산값을 본다.</summary>
        public readonly string Key;

        public readonly string Title;

        /// <summary>기록이 이만큼이 되면 완료다.</summary>
        public readonly int Goal;

        /// <summary>화면에 쓸 대상 이름. 적 ID 를 그대로 보여줄 수는 없어서 따로 적는다.</summary>
        public readonly string TargetName;

        public readonly long GoldReward;

        private QuestPlan(RecordType type, string key, string title, int goal, string targetName, long goldReward)
        {
            Type = type;
            Key = key;
            Title = title;
            Goal = goal;
            TargetName = targetName;
            GoldReward = goldReward;
        }

        public static QuestPlan LevelReach(string title, int level, long goldReward)
        {
            return new QuestPlan(RecordType.LevelReach, string.Empty, title, level, string.Empty, goldReward);
        }

        /// <summary>적을 가리지 않는 처치 퀘스트. 아무거나 잡으면 센다.</summary>
        public static QuestPlan AnyKill(string title, int count, long goldReward)
        {
            return new QuestPlan(RecordType.EnemyKill, string.Empty, title, count, "적", goldReward);
        }

        public static QuestPlan EnemyKill(string title, string enemyID, string enemyName, int count, long goldReward)
        {
            return new QuestPlan(RecordType.EnemyKill, enemyID, title, count, enemyName, goldReward);
        }

        /// <summary>화면에 쓰는 할 일 한 줄. 선언에 적지 않고 보는 기록에서 만든다.</summary>
        public string Description
        {
            get
            {
                switch (Type)
                {
                    case RecordType.PlayTime:
                        return $"{Goal / 60}분 플레이하기";
                    case RecordType.EnemyKill:
                        return $"{TargetName} {Goal}마리 처치하기";
                    case RecordType.LevelReach:
                        return $"레벨 {Goal} 달성하기";
                    case RecordType.GoldEarned:
                        return $"골드 {Goal:N0} 모으기";
                    case RecordType.ExpEarned:
                        return $"경험치 {Goal:N0} 얻기";
                    case RecordType.WaveReach:
                        return $"웨이브 {Goal} 도달하기";
                    default:
                        return string.Empty;
                }
            }
        }
    }
}
