using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 편성창 영웅 명단의 카드 한 장. 초상화·이름·고유색 띠와 "출전 중 / 대기" 를 그리고, 눌리면 자기 영웅 ID 를 넘긴다.
    /// 누가 누구인지는 빌더가 프로필을 보고 구워 둔다. 고름(금빛 테두리)과 출전 여부(글자)는 따로 창이 넣어 준다 —
    /// 고르고 적용을 눌러야 출전이 바뀐다.
    /// </summary>
    public class FormationHeroWidget : MonoBehaviour
    {
        private static readonly Color DeployedStateColor = new Color(0.94f, 0.78f, 0.46f);
        private static readonly Color IdleStateColor = new Color(0.65f, 0.65f, 0.7f);

        [SerializeField] private Button _button;
        [SerializeField] private Image _portrait;
        [SerializeField] private Image _themeBar;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _stateText;
        [SerializeField] private GameObject _selectedMark;
        [SerializeField] private string _heroID;

        public string HeroID => _heroID;

        /// <summary>눌리면 onClick 에 이 카드의 영웅 ID 를 넘긴다.</summary>
        public void Init(Action<string> onClick)
        {
            _button.onClick.AsObservable()
                .Subscribe((self: this, onClick), (_, state) => state.onClick(state.self._heroID))
                .AddTo(this);
        }

        public void Setup(string heroID, CharacterProfile profile)
        {
            _heroID = heroID;
            _nameText.text = profile.DisplayName;
            _portrait.sprite = profile.Portrait;
            _themeBar.color = profile.ThemeColor;
        }

        public void SetSelected(bool isSelected)
        {
            _selectedMark.SetActive(isSelected);
        }

        public void SetDeployed(bool isDeployed)
        {
            _stateText.text = isDeployed ? "출전 중" : "대기";
            _stateText.color = isDeployed ? DeployedStateColor : IdleStateColor;
        }
    }
}
