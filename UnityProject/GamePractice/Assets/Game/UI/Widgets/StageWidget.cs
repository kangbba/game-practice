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

        private StageManager _stageManager;

        public void Init(StageManager stageManager)
        {
            _stageManager = stageManager;

            DrawWave();

            stageManager.WaveStarted
                .Subscribe(this, (_, self) => self.DrawWave())
                .AddTo(this);

            stageManager.WaveKills
                .Subscribe(this, (kills, self) => self.DrawKills(kills, self._stageManager.WaveGoal))
                .AddTo(this);
        }

        /// <summary>구독 없이 최종 모습만 그린다. 프리팹을 구울 때 쓰는 문이다.</summary>
        public void Preview(string stageName, int kills, int goal)
        {
            _stageText.text = stageName;

            DrawKills(kills, goal);
        }

        /// <summary>웨이브가 바뀌면 명패와 목표 수를 새로 그린다.</summary>
        private void DrawWave()
        {
            _stageText.text = _stageManager.Label;

            DrawKills(_stageManager.WaveKills.CurrentValue, _stageManager.WaveGoal);
        }

        private void DrawKills(int current, int max)
        {
            _progressFill.FillAmount = max > 0 ? (float)current / max : 0f;
            _killLabel.text = $"{current}/{max}";
        }
    }
}
