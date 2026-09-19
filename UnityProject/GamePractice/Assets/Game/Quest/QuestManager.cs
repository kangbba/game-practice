using System.Linq;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 퀘스트의 주인. 선언 목록을 위에서부터 한 장씩 내주고, 다 채우면 받아 갈 수 있는 상태로 둔다.
    /// 보상은 저절로 들어오지 않는다 — 눌러서 Claim 해야 골드가 들어오고 다음 장으로 넘어간다.
    ///
    /// 무엇을 얼마나 했는지는 세지 않는다. 그건 RecordManager 가 판이 시작된 뒤로 계속 세고 있고,
    /// 여기서는 지금 퀘스트가 보는 기록 하나만 들여다본다. 그래서 이미 채운 조건의 퀘스트를 받으면
    /// 받자마자 완료로 떠서 누르기만 하면 된다 — 건너뛰는 게 따로 있는 게 아니라 그냥 바로 받아진다.
    /// </summary>
    public class QuestManager : ManagerBase
    {
        private readonly RecordManager _recordManager;
        private readonly CurrencyManager _currencyManager;

        /// <summary>지금 퀘스트가 보는 기록(여럿이면 그 묶음). 다음 장으로 넘어가면 보던 줄을 놓고 새 줄을 잡는다.</summary>
        private readonly SerialDisposable _watchedRecord = new SerialDisposable();

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

        public QuestManager(RecordManager recordManager, CurrencyManager currencyManager)
        {
            _recordManager = recordManager;
            _currencyManager = currencyManager;
        }

        protected override void OnInit()
        {
            _watchedRecord.RegisterTo(LifeToken);

            CurrentQuest
                .Subscribe(this, (quest, self) => self.Watch(quest))
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
        }

        /// <summary>진행도는 들고 있는 게 아니라 지금 퀘스트가 보는 기록을 그대로 흘려보낸 값이다. 열쇠가 여럿이면 그 기록들의 합이다.</summary>
        private void Watch(QuestPlan quest)
        {
            if (quest == null)
            {
                _watchedRecord.Disposable = null;
                _progress.Value = 0;
                return;
            }

            _watchedRecord.Disposable = Observable.CombineLatest(quest.Keys.Select(key => _recordManager.Get(quest.Type, key)))
                .Subscribe(this, (values, self) => self._progress.Value = (int)values.Sum());
        }
    }
}
