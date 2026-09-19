using System.Collections.Generic;

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

        /// <summary>
        /// 기록을 찾는 열쇠들. 처치 퀘스트의 적 ID 같은 것 — 비어 있는 열쇠 하나면 그 종류의 통산값을 본다.
        /// 여럿이면 그 기록들을 합친 값이 진행도다(오우거·몽둥이 오우거·오우거 족장을 한 퀘스트로 세는 식).
        /// </summary>
        public readonly IReadOnlyList<string> Keys;

        public readonly string Title;

        /// <summary>기록이 이만큼이 되면 완료다.</summary>
        public readonly int Goal;

        /// <summary>화면에 쓸 대상 이름. 적 ID 를 그대로 보여줄 수는 없어서 따로 적는다.</summary>
        public readonly string TargetName;

        public readonly long GoldReward;

        private QuestPlan(RecordType type, IReadOnlyList<string> keys, string title, int goal, string targetName,
            long goldReward)
        {
            Type = type;
            Keys = keys;
            Title = title;
            Goal = goal;
            TargetName = targetName;
            GoldReward = goldReward;
        }

        public static QuestPlan LevelReach(string title, int level, long goldReward)
        {
            return new QuestPlan(RecordType.LevelReach, new[] { string.Empty }, title, level, string.Empty, goldReward);
        }

        /// <summary>적을 가리지 않는 처치 퀘스트. 아무거나 잡으면 센다.</summary>
        public static QuestPlan AnyKill(string title, int count, long goldReward)
        {
            return new QuestPlan(RecordType.EnemyKill, new[] { string.Empty }, title, count, "적", goldReward);
        }

        /// <summary>
        /// 이 적들을 잡으면 세는 처치 퀘스트. 같은 종족의 변종(몽둥이 오우거, 오우거 족장 등)도 세려면 ID 를 전부 적는다 —
        /// 변종이 새로 생기면 여기에도 적어 줘야 센다.
        /// </summary>
        public static QuestPlan EnemyKill(string title, string enemyName, int count, long goldReward,
            params string[] enemyIDs)
        {
            return new QuestPlan(RecordType.EnemyKill, enemyIDs, title, count, enemyName, goldReward);
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
