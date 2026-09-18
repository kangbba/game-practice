using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 전투 HUD. 화면에 늘 떠 있다.
    /// 여기는 배선판이다 — 어느 위젯이 무엇을 볼지만 정해 주고, 구독과 표시는 각자 한다.
    /// </summary>
    public class BattlePanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _reviveText;
        [SerializeField] private FloatingJoystick _joystick;
        [SerializeField] private HeroProfileWidget _heroProfile;
        [SerializeField] private CurrencyWidget _goldWidget;
        [SerializeField] private CurrencyWidget _gemWidget;
        [SerializeField] private StageWidget _stageWidget;
        [SerializeField] private QuestWidget _questWidget;
        [SerializeField] private SkillButtonWidget _skillButton;
        [SerializeField] private SkillButtonWidget _ultimateButton;
        [SerializeField] private Button _equipMenuButton;
        [SerializeField] private Button _growthMenuButton;
        [SerializeField] private Button _formationMenuButton;

        public FloatingJoystick Joystick => _joystick;
        public Observable<Unit> SkillClicked => _skillButton.Clicked;
        public Observable<Unit> UltimateClicked => _ultimateButton.Clicked;

        public void Init(HeroManager heroManager, StageManager stageManager, QuestManager questManager,
            CurrencyManager currencyManager, GrowthManager growthManager,
            IAssets<CharacterProfile> profiles, PopupManager popupManager, UltimateDirector ultimateDirector)
        {
            _heroProfile.Init(heroManager, growthManager, profiles);
            _stageWidget.Init(stageManager);
            _questWidget.Init(questManager);

            _goldWidget.Init(currencyManager.Gold);
            _gemWidget.Init(currencyManager.Gem);

            _skillButton.Init(heroManager, SkillSlotType.Skill, hero => hero.Combat.CanUseSkill);
            _ultimateButton.Init(heroManager, SkillSlotType.Ultimate, ultimateDirector.CanPlay);

            BindRevive(heroManager);
            BindMenuButtons(popupManager);
        }

        /// <summary>부활 카운트다운. 위젯 없이 글자 하나뿐이라 여기서 그린다.</summary>
        private void BindRevive(HeroManager heroManager)
        {
            heroManager.ReviveRemainTime
                .Subscribe(this, (remain, self) => self._reviveText.text =
                    remain > 0f ? $"부활까지 {remain:0.0}초" : string.Empty)
                .AddTo(this);
        }

        /// <summary>창을 여는 버튼들. 어느 종류를 열지만 말하고, 여는 건 팝업 매니저가 한다.</summary>
        private void BindMenuButtons(PopupManager popupManager)
        {
            _growthMenuButton.onClick.AsObservable()
                .Subscribe(popupManager, (_, manager) => manager.Open(PopupType.Growth))
                .AddTo(this);

            _equipMenuButton.onClick.AsObservable()
                .Subscribe(popupManager, (_, manager) => manager.Open(PopupType.Equipment))
                .AddTo(this);

            _formationMenuButton.onClick.AsObservable()
                .Subscribe(popupManager, (_, manager) => manager.Open(PopupType.Formation))
                .AddTo(this);
        }
    }
}
