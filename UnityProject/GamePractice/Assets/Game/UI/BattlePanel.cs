using System.Collections.Generic;
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
        [SerializeField] private EquipmentWindow _equipmentWindow;
        [SerializeField] private Button _growthMenuButton;
        [SerializeField] private GrowthWindow _growthWindow;

        public FloatingJoystick Joystick => _joystick;
        public Observable<Unit> SkillClicked => _skillButton.Clicked;
        public Observable<Unit> UltimateClicked => _ultimateButton.Clicked;

        /// <summary>이 HUD 위에 뜨는 창 전부. 새 창을 만들면 여기에 올린다 — 열린 동안 게임 중단은 그걸로 따라온다.</summary>
        public IReadOnlyList<PopupWindow> Popups => new PopupWindow[] { _equipmentWindow, _growthWindow };

        public void Init(HeroManager heroManager, WaveManager waveManager, QuestManager questManager,
            CurrencyManager currencyManager, GrowthManager growthManager, EquipmentManager equipmentManager,
            IAssets<CharacterProfile> profiles, IAssets<Hero> heroAssets)
        {
            _heroProfile.Init(heroManager, growthManager, profiles);
            _stageWidget.Init(waveManager);
            _questWidget.Init(questManager);

            _goldWidget.Init(currencyManager.Gold);
            _gemWidget.Init(currencyManager.Gem);

            _skillButton.Init(heroManager, SkillSlotType.Skill);
            _ultimateButton.Init(heroManager, SkillSlotType.Ultimate);

            _growthWindow.Init(growthManager, currencyManager, heroManager);
            _equipmentWindow.Init(equipmentManager, heroManager, heroAssets);

            BindRevive(heroManager);
            BindMenuButtons();
        }

        /// <summary>부활 카운트다운. 위젯 없이 글자 하나뿐이라 여기서 그린다.</summary>
        private void BindRevive(HeroManager heroManager)
        {
            heroManager.ReviveRemainTime
                .Subscribe(this, (remain, self) => self._reviveText.text =
                    remain > 0f ? $"부활까지 {remain:0.0}초" : string.Empty)
                .AddTo(this);
        }

        /// <summary>창을 여는 버튼들. 무엇을 보여줄지는 창이 알아서 하고, 여기는 열어 주기만 한다.</summary>
        private void BindMenuButtons()
        {
            _growthMenuButton.onClick.AsObservable()
                .Subscribe(this, (_, self) => self._growthWindow.Show())
                .AddTo(this);

            _equipMenuButton.onClick.AsObservable()
                .Subscribe(this, (_, self) => self._equipmentWindow.Toggle())
                .AddTo(this);
        }
    }
}
