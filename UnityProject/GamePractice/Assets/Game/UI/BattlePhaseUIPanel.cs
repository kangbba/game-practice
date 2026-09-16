using System.Threading;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// Combat 페이즈 동안만 보이는 전투 HUD. PhaseUIManager가 표시를 토글한다.
    /// 여기서는 계산하지 않는다 — 매니저가 들고 있는 값을 구독해서 그리기만 한다.
    /// </summary>
    public class BattlePhaseUIPanel : PhaseUIBase
    {
        [SerializeField] private TextMeshProUGUI _reviveText;
        [SerializeField] private FloatingJoystick _joystick;
        [SerializeField] private HeroStatusWidget _heroStatus;
        [SerializeField] private CurrencyWidget _goldWidget;
        [SerializeField] private CurrencyWidget _gemWidget;
        [SerializeField] private StageWidget _stageWidget;
        [SerializeField] private GuideWidget _guideWidget;
        [SerializeField] private SkillButtonWidget _skillButton;
        [SerializeField] private SkillButtonWidget _ultimateButton;
        [SerializeField] private Button _equipMenuButton;
        [SerializeField] private EquipmentWindow _equipmentWindow;

        private IAssets<CharacterProfile> _profiles;

        public override string PhaseKey => PhaseID.Combat;

        public FloatingJoystick Joystick => _joystick;
        public Observable<Unit> SkillClicked => _skillButton.Clicked;
        public Observable<Unit> UltimateClicked => _ultimateButton.Clicked;
        public Observable<Unit> EquipMenuClicked => _equipMenuButton.onClick.AsObservable();
        public EquipmentWindow EquipmentWindow => _equipmentWindow;

        public void Bind(HeroManager heroManager, WaveManager waveManager,
            CurrencyManager currencyManager, GrowthManager growthManager, IAssets<CharacterProfile> profiles)
        {
            _profiles = profiles;

            BindHeroes(heroManager);
            BindStage(waveManager);
            BindCurrency(currencyManager);
            BindGrowth(growthManager);
        }

        private void BindHeroes(HeroManager heroManager)
        {
            heroManager.ReviveRemainTime
                .Subscribe(this, (remain, self) => self._reviveText.text =
                    remain > 0f ? $"부활까지 {remain:0.0}초" : string.Empty)
                .RegisterTo(destroyCancellationToken);

            heroManager.Spawned
                .Subscribe(this, (character, self) => self.BindHero(character))
                .RegisterTo(destroyCancellationToken);
        }

        private void BindStage(WaveManager waveManager)
        {
            waveManager.CurrentWave
                .Subscribe(this, (wave, self) => self._stageWidget.SetStage($"{wave} 웨이브"))
                .RegisterTo(destroyCancellationToken);

            waveManager.Kills
                .CombineLatest(waveManager.Goal, (kills, goal) => (kills, goal))
                .Subscribe(this, (progress, self) => self._stageWidget.SetKills(progress.kills, progress.goal))
                .RegisterTo(destroyCancellationToken);
        }

        private void BindCurrency(CurrencyManager currencyManager)
        {
            currencyManager.Gold
                .Subscribe(this, (gold, self) => self._goldWidget.SetAmount(gold))
                .RegisterTo(destroyCancellationToken);

            currencyManager.Gem
                .Subscribe(this, (gem, self) => self._gemWidget.SetAmount(gem))
                .RegisterTo(destroyCancellationToken);
        }

        /// <summary>성장 가이드는 "다음 레벨 찍기"다. 레벨이 오르면 목표도 따라 올라간다.</summary>
        private void BindGrowth(GrowthManager growthManager)
        {
            growthManager.Level
                .Subscribe(this, (level, self) =>
                {
                    self._heroStatus.SetLevel(level);
                    self._guideWidget.SetGuide($"성장 가이드 {level:00}", $"레벨 {level + 1} 달성하기");
                    self._guideWidget.SetReward(level * 10);
                    self._guideWidget.SetProgress(level, level + 1);
                })
                .RegisterTo(destroyCancellationToken);

            growthManager.ExpRatio
                .Subscribe(this, (ratio, self) => self._heroStatus.SetEXPRatio(ratio))
                .RegisterTo(destroyCancellationToken);
        }

        private void BindHero(Character hero)
        {
            var profile = _profiles.Get(hero.ID);

            _heroStatus.SetName(profile.DisplayName);
            _heroStatus.SetPortrait(profile.Portrait);

            hero.CurrentHP
                .Subscribe((self: this, hero), (hp, state) => state.self._heroStatus.SetHP(hp, state.hero.CurrentStats.CurrentValue.MaxHP))
                .RegisterTo(hero.destroyCancellationToken);

            // 스킬 버튼은 묶음을 마친 뒤 다음 묶음까지의 대기를 보여준다.
            _skillButton.SetText(hero.Combat.Cycle.Last.Name);
            BindCooldown(hero.Combat.AttackCooldown, _skillButton, hero.destroyCancellationToken);
            _ultimateButton.SetText(hero.Combat.Ultimate != null ? hero.Combat.Ultimate.Name : "궁극기");
            BindCooldown(hero.Combat.UltimateCooldown, _ultimateButton, hero.destroyCancellationToken);
        }

        /// <summary>버튼이 아는 건 남은 비율과 남은 초뿐이다. 해석은 캐릭터가 이미 해뒀다.</summary>
        private static void BindCooldown(CooldownTimer cooldown, SkillButtonWidget button, CancellationToken token)
        {
            cooldown.RemainRatio
                .Subscribe(button, (ratio, self) => self.SetCooldownRatio(ratio))
                .RegisterTo(token);

            cooldown.RemainSeconds
                .Subscribe(button, (remain, self) => self.SetCooldownRemain(remain))
                .RegisterTo(token);
        }
    }
}
