using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>가이드 퀘스트 박스: 제목, 내용, 보상, 진행도.</summary>
    public class GuideWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descText;
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private SlicedFillBar _progressFill;
        [SerializeField] private TextMeshProUGUI _progressLabel;

        public void SetGuide(string title, string desc)
        {
            _titleText.text = title;
            _descText.text = desc;
        }

        public void SetReward(int amount)
        {
            _rewardText.text = amount.ToString();
        }

        public void SetProgress(int current, int goal)
        {
            _progressFill.FillAmount = goal > 0 ? (float)current / goal : 0f;
            _progressLabel.text = $"{current}/{goal}";
        }
    }
}
