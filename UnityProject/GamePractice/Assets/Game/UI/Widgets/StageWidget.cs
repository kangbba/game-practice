using R3;
using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>중앙 상단 스테이지 정보: 단계 이름, 진행바, 킬 카운트. 웨이브 시작·처치 수를 구독해서 그리기만 한다.</summary>
    public class StageWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _stageText;
        [SerializeField] private SlicedFillBar _progressFill;
        [SerializeField] private TextMeshProUGUI _killLabel;

        /// <summary>wave 는 지금 웨이브의 표기와 목표 수다. 웨이브가 바뀔 때마다 흐른다.</summary>
        public void Init(Observable<(string label, int goal)> wave, Observable<int> kills)
        {
            wave
                .CombineLatest(kills, (current, count) => (current.label, count, current.goal))
                .Subscribe(this, (state, self) => self.Draw(state.label, state.count, state.goal))
                .AddTo(this);
        }

        /// <summary>구독 없이 최종 모습만 그린다. 프리팹을 구울 때 쓰는 문이다.</summary>
        public void Preview(string stageName, int kills, int goal)
        {
            Draw(stageName, kills, goal);
        }

        private void Draw(string label, int current, int max)
        {
            _stageText.text = label;
            _progressFill.FillAmount = max > 0 ? (float)current / max : 0f;
            _killLabel.text = $"{current}/{max}";
        }
    }
}
