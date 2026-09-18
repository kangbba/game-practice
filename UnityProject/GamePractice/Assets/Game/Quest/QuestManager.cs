using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 퀘스트의 주인. 선언 목록을 위에서부터 한 장씩 내주고, 다 채우면 받아 갈 수 있는 상태로 둔다.
    /// 보상은 저절로 들어오지 않는다 — 눌러서 Claim 해야 골드가 들어오고 다음 장으로 넘어간다.
    ///
    /// 처치 수는 퀘스트와 상관없이 판이 시작된 뒤로 계속 센다. 그래서 이미 채운 조건의 퀘스트를 받으면
    /// 받자마자 완료로 떠서 누르기만 하면 된다 — 건너뛰는 게 따로 있는 게 아니라 그냥 바로 받아진다.
    /// </summary>
    public class QuestManager : ManagerBase
    {
        private readonly EnemyManager _enemyManager;
        private readonly GrowthManager _growthManager;
        private readonly CurrencyManager _currencyManager;

        /// <summary>적 ID 마다 통산 처치 수. 아무거나 세는 퀘스트는 이 값들의 합을 본다.</summary>
        private readonly Dictionary<string, int> _killCounts = new Dictionary<string, int>();
        private int _totalKills;

        private readonly ReactiveProperty<int> _currentIndex = new ReactiveProperty<int>(0);
        private readonly ReactiveProperty<int> _progress = new ReactiveProperty<int>(0);
        private readonly Subject<QuestPlan> _claimed = new Subject<QuestPlan>();

        /// <summary>지금 받은 퀘스트. 목록을 다 끝내면 null 이 흐른다.</summary>
        public Observable<QuestPlan> CurrentQuest => _currentIndex.Select(index => QuestPlans.Get(index));

        /// <summary>몇 번째 퀘스트인지. 화면에는 1 부터 세어 보여준다.</summary>
        public ReadOnlyReactiveProperty<int> CurrentIndex => _currentIndex;

        /// <summary>지금 퀘스트의 진행도. 목표를 넘겨도 실제 수치 그대로 흐른다.</summary>
        public ReadOnlyReactiveProperty<int> Progress => _progress;

        /// <summary>지금 눌러서 받아 갈 수 있나.</summary>
        public Observable<bool> IsClaimable =>
            CurrentQuest.CombineLatest(_progress, (quest, progress) => quest != null && progress >= quest.Goal)
                .DistinctUntilChanged();

        /// <summary>한 장을 받아 갔다. 보상 연출·토스트가 이걸 본다.</summary>
        public Observable<QuestPlan> Claimed => _claimed;

        public QuestManager(EnemyManager enemyManager, GrowthManager growthManager, CurrencyManager currencyManager)
        {
            _enemyManager = enemyManager;
            _growthManager = growthManager;
            _currencyManager = currencyManager;
        }

        protected override void OnInit()
        {
            _enemyManager.Died
                .Subscribe(this, (enemy, self) => self.CountKill(enemy.ID))
                .RegisterTo(LifeToken);

            _growthManager.Level
                .Subscribe(this, (_, self) => self.RefreshProgress())
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            _currentIndex.Dispose();
            _progress.Dispose();
            _claimed.Dispose();
        }

        /// <summary>다 채운 퀘스트를 받아 간다. 아직 못 채웠으면 아무 일도 없다.</summary>
        public void Claim()
        {
            var quest = QuestPlans.Get(_currentIndex.Value);

            if (quest == null || _progress.Value < quest.Goal)
            {
                return;
            }

            Debug.Log($"퀘스트 {_currentIndex.Value + 1} 완료 — {quest.Title} (보상 {quest.GoldReward:N0} G)");

            _currencyManager.AddGold(quest.GoldReward);
            _claimed.OnNext(quest);

            _currentIndex.Value++;
            RefreshProgress();
        }

        private void CountKill(string enemyID)
        {
            _killCounts.TryGetValue(enemyID, out var count);
            _killCounts[enemyID] = count + 1;
            _totalKills++;

            RefreshProgress();
        }

        /// <summary>진행도는 들고 있는 게 아니라 지금 퀘스트를 보고 그때그때 읽어 온 값이다.</summary>
        private void RefreshProgress()
        {
            var quest = QuestPlans.Get(_currentIndex.Value);

            if (quest == null)
            {
                _progress.Value = 0;
                return;
            }

            _progress.Value = quest.Type == QuestType.LevelReach
                ? _growthManager.Level.CurrentValue
                : KillsOf(quest.TargetEnemyID);
        }

        private int KillsOf(string enemyID)
        {
            if (string.IsNullOrEmpty(enemyID))
            {
                return _totalKills;
            }

            _killCounts.TryGetValue(enemyID, out var killed);
            return killed;
        }
    }
}
