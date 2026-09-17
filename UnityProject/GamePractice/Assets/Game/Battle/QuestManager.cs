using System.Collections.Generic;
using R3;

namespace Sayne
{
    /// <summary>
    /// 퀘스트의 주인. 지금은 처치 퀘스트만 안다 — 적이 죽으면 세고, 목표치를 채우면 완료를 알린다.
    /// 보상 지급·UI 연결은 아직 없다. 뼈대만 세워 둔 상태다.
    /// </summary>
    public class QuestManager : ManagerBase
    {
        /// <summary>
        /// 퀘스트 기본 설계값. 다른 매니저들처럼 ScriptableObject 대신 당분간 여기서 선언한다.
        /// Target 이 비어 있으면 아무 적이나 세는 퀘스트다.
        /// </summary>
        private static readonly Dictionary<string, (string Title, string Target, int Goal)> Plans =
            new Dictionary<string, (string, string, int)>
            {
                [QuestID.FirstBlood] = ("첫 사냥", "", 1),
                [QuestID.GoblinHunter] = ("고블린 사냥꾼", EnemyID.Goblin, 10),
                [QuestID.OgreSlayer] = ("오우거 학살자", EnemyID.Ogre, 5),
            };

        private readonly EnemyManager _enemyManager;

        private readonly Dictionary<string, int> _killCounts = new Dictionary<string, int>();
        private readonly HashSet<string> _completed = new HashSet<string>();
        private readonly Subject<string> _questCompleted = new Subject<string>();

        /// <summary>퀘스트 하나가 완료됐다. 값은 QuestID. 보상·토스트 UI 가 이걸 볼 예정이다.</summary>
        public Observable<string> QuestCompleted => _questCompleted;

        public QuestManager(EnemyManager enemyManager)
        {
            _enemyManager = enemyManager;
        }

        protected override void OnInit()
        {
            _enemyManager.Died
                .Subscribe(this, (enemy, self) => self.OnEnemyKilled(enemy.ID))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            _questCompleted.Dispose();
        }

        public bool IsCompleted(string questID) => _completed.Contains(questID);

        public int GetProgress(string questID)
        {
            var plan = Plans[questID];
            return CountKillsFor(plan.Target);
        }

        public int GetGoal(string questID) => Plans[questID].Goal;

        private void OnEnemyKilled(string enemyID)
        {
            _killCounts.TryGetValue(enemyID, out var count);
            _killCounts[enemyID] = count + 1;

            foreach (var pair in Plans)
            {
                if (_completed.Contains(pair.Key))
                {
                    continue;
                }

                if (CountKillsFor(pair.Value.Target) >= pair.Value.Goal)
                {
                    _completed.Add(pair.Key);
                    _questCompleted.OnNext(pair.Key);
                }
            }
        }

        private int CountKillsFor(string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                var total = 0;
                foreach (var count in _killCounts.Values)
                {
                    total += count;
                }

                return total;
            }

            _killCounts.TryGetValue(target, out var killed);
            return killed;
        }
    }

    /// <summary>퀘스트 ID. EnemyID 처럼 문자열 상수로 둔다 — 나중에 Data 폴더로 옮겨도 된다.</summary>
    public static class QuestID
    {
        public const string FirstBlood = "FirstBlood";
        public const string GoblinHunter = "GoblinHunter";
        public const string OgreSlayer = "OgreSlayer";
    }
}
