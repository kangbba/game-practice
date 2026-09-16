using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>중앙 상단 스테이지 정보: 단계 이름, 진행바, 킬 카운트.</summary>
    public class StageWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _stageText;
        [SerializeField] private Image _progressFill;
        [SerializeField] private TextMeshProUGUI _killLabel;

        public void SetStage(string stageName)
        {
            _stageText.text = stageName;
        }

        public void SetKills(int current, int max)
        {
            _progressFill.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            _killLabel.text = $"{current}/{max}";
        }
    }
}
