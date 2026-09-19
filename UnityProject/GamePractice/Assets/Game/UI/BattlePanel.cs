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
        [SerializeField] private SkillButtonWidget _gadgetButton;
        [SerializeField] private SkillButtonWidget _skillButton;
        [SerializeField] private SkillButtonWidget _ultimateButton;
        [SerializeField] private Button _equipMenuButton;
        [SerializeField] private Button _growthMenuButton;
        [SerializeField] private Button _formationMenuButton;

        public FloatingJoystick Joystick => _joystick;
        public Observable<Unit> GadgetClicked => _gadgetButton.Clicked;
        public Observable<Unit> SkillClicked => _skillButton.Clicked;
        public Observable<Unit> UltimateClicked => _ultimateButton.Clicked;

        /// <summary>HUD 는 게임·전투 양쪽을 두루 보여 준다. 컨텍스트를 받아 위젯마다 필요한 것만 골라 넘긴다.</summary>
        public void Init(GameContext game, BattleContext battle)
        {
            var heroManager = battle.HeroManager;
            var stageManager = battle.StageManager;
            var ultimateDirector = battle.UltimateDirector;
            var questManager = game.QuestManager;
            var currencyManager = game.CurrencyManager;
            var growthManager = game.GrowthManager;
            var profiles = game.Assets.Profiles;
            var popupManager = game.PopupManager;

            _heroProfile.Init(heroManager.CurrentHero, growthManager.Level, growthManager.ExpRatio, profiles);

            // 번호는 구독거리가 아니라 웨이브가 시작될 때 표기와 목표 수를 뽑는다. 첫 웨이브 전에도 지금 번호로 한 번 그린다.
            var wave = stageManager.WaveStarted
                .Select(stageManager, (number, manager) => (manager.GetLabel(number.stage, number.wave), manager.WaveGoal))
                .Prepend((stageManager.Label, stageManager.WaveGoal));

            _stageWidget.Init(wave, stageManager.WaveKills);

            _questWidget.Init(questManager.CurrentQuest, questManager.CurrentIndex, questManager.Progress,
                questManager.IsClaimable, questManager.Claimed, questManager.Claim);

            _goldWidget.Init(currencyManager.Gold);
            _gemWidget.Init(currencyManager.Gem);

            // 조이스틱을 잡고 있는 동안은 사용자가 몸을 모는 중이라 돌진이 안 나간다 — 버튼도 꺼 둔다.
            _gadgetButton.Init(heroManager.CurrentHero, SkillSlotType.Gadget,
                hero => hero.Combat.CanUseGadget && !_joystick.IsActive);
            _skillButton.Init(heroManager.CurrentHero, SkillSlotType.Skill, hero => hero.Combat.CanUseSkill);
            _ultimateButton.Init(heroManager.CurrentHero, SkillSlotType.Ultimate, ultimateDirector.CanPlay);

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
