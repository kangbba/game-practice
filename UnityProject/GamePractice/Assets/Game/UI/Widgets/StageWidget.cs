using R3;
using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>중앙 상단 스테이지 정보: 단계 이름, 진행바, 킬 카운트. 웨이브 상태를 구독해서 그리기만 한다.</summary>
    public class StageWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _stageText;
        [SerializeField] private SlicedFillBar _progressFill;
        [SerializeField] private TextMeshProUGUI _killLabel;

        public void Init(WaveManager waveManager)
        {
            waveManager.Label
                .Subscribe(this, (label, self) => self._stageText.text = label)
                .AddTo(this);

            waveManager.Kills
                .CombineLatest(waveManager.Goal, (kills, goal) => (kills, goal))
                .Subscribe(this, (progress, self) => self.DrawKills(progress.kills, progress.goal))
                .AddTo(this);
        }

        /// <summary>구독 없이 최종 모습만 그린다. 프리팹을 구울 때 쓰는 문이다.</summary>
        public void Preview(string stageName, int kills, int goal)
        {
            _stageText.text = stageName;

            DrawKills(kills, goal);
        }

        private void DrawKills(int current, int max)
        {
            _progressFill.FillAmount = max > 0 ? (float)current / max : 0f;
            _killLabel.text = $"{current}/{max}";
        }
    }
}
