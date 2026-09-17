using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 좌상단 퀘스트 박스: 몇 번째 퀘스트인지, 할 일, 보상, 진행도.
    /// 다 채우면 "완료" 덮개가 덮이고, 누르면 받아 간다 — 받는 판단은 퀘스트 매니저가 한다.
    /// </summary>
    public class QuestWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descText;
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private SlicedFillBar _progressFill;
        [SerializeField] private TextMeshProUGUI _progressLabel;

        [Header("완료 덮개")]
        [SerializeField] private GameObject _completeOverlay;
        [SerializeField] private Button _claimBtn;

        public void Init(QuestManager questManager)
        {
            questManager.CurrentQuest
                .CombineLatest(questManager.CurrentIndex, (quest, index) => (quest, index))
                .CombineLatest(questManager.Progress, (pair, progress) => (pair.quest, pair.index, progress))
                .Subscribe(this, (state, self) => self.Draw(state.quest, state.index, state.progress))
                .AddTo(this);

            questManager.IsClaimable
                .Subscribe(this, (isClaimable, self) => self._completeOverlay.SetActive(isClaimable))
                .AddTo(this);

            _claimBtn.onClick.AsObservable()
                .Subscribe(questManager, (_, manager) => manager.Claim())
                .AddTo(this);
        }

        /// <summary>구독 없이 최종 모습만 그린다. 프리팹을 구울 때 쓰는 문이다.</summary>
        public void Preview(QuestPlan quest, int index, int progress, bool isComplete)
        {
            Draw(quest, index, progress);

            _completeOverlay.SetActive(isComplete);
        }

        private void Draw(QuestPlan quest, int index, int progress)
        {
            if (quest == null)
            {
                DrawAllDone();
                return;
            }

            // 목록의 몇 번째인지를 그대로 제목에 붙인다 — 어디까지 왔는지가 숫자로 보인다.
            _titleText.text = $"퀘스트 {index + 1:00}  {quest.Title}";
            _descText.text = quest.Description;
            _rewardText.text = $"{quest.GoldReward:N0}";

            // 막대는 목표를 넘겨 그리지 않는다. 이미 넘긴 채로 받은 퀘스트는 가득 찬 채로 뜬다.
            var current = Mathf.Min(progress, quest.Goal);

            _progressFill.FillAmount = (float)current / quest.Goal;
            _progressLabel.text = $"{current}/{quest.Goal}";
        }

        /// <summary>줄 게 없을 때. 목록을 다 끝낸 상태다.</summary>
        private void DrawAllDone()
        {
            _titleText.text = "퀘스트";
            _descText.text = "모두 완료";
            _rewardText.text = "-";

            _progressFill.FillAmount = 1f;
            _progressLabel.text = string.Empty;
        }
    }
}
