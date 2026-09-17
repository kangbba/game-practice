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
        [SerializeField] private Button _growthMenuButton;
        [SerializeField] private GrowthWindow _growthWindow;

        private IAssets<CharacterProfile> _profiles;
        private CurrencyManager _currencyManager;
        private GrowthManager _growthManager;

        /// <summary>성장 모달이 "기본 + 성장" 을 그릴 때 보는 히어로. 부활하면 새 히어로로 갈린다.</summary>
        private Character _hero;

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

            _currencyManager = currencyManager;
            _growthManager = growthManager;

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
                .Subscribe(this, (wave, self) => self._stageWidget.SetStage(wave.Label))
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

            BindGrowthWindow(growthManager);
        }

        /// <summary>
        /// 성장 모달. 데이터는 열려 있든 닫혀 있든 늘 구독 중이라, 열리는 순간 이미 맞는 값이 그려져 있다.
        /// 성장은 저절로 오르지 않는다 — 버튼을 눌러 골드를 내면 그때 한 칸 오른다.
        /// </summary>
        private void BindGrowthWindow(GrowthManager growthManager)
        {
            _growthMenuButton.onClick.AsObservable()
                .Subscribe(this, (_, self) => self._growthWindow.Show())
                .RegisterTo(destroyCancellationToken);

            // 사고 나면 골드와 성장 몫이 둘 다 움직인다 — 다시 그리는 건 아래 두 구독이 알아서 한다.
            _growthWindow.UpgradeRequested
                .Subscribe(growthManager, (stat, manager) => manager.TryUpgrade(stat))
                .RegisterTo(destroyCancellationToken);

            // 골드가 들어오면 살 수 있는 줄이 늘어난다 — 값과 버튼 상태를 같이 다시 그린다.
            _currencyManager.Gold
                .Subscribe(this, (_, self) => self.RefreshGrowthWindow())
                .RegisterTo(destroyCancellationToken);

            growthManager.Bonus
                .Subscribe(this, (_, self) => self.RefreshGrowthWindow())
                .RegisterTo(destroyCancellationToken);
        }

        /// <summary>창 전체를 한 번에 다시 그린다 — 한 줄을 사면 골드가 줄어 다른 줄의 버튼도 같이 흔들린다.</summary>
        private void RefreshGrowthWindow()
        {
            _growthWindow.SetGold(_currencyManager.Gold.CurrentValue);

            foreach (var stat in GrowthPlan.All)
            {
                var level = _growthManager.StatLevel(stat).CurrentValue;
                var baseValue = _hero != null ? GrowthPlan.ValueOf(stat, _hero.BaseStats) : 0;
                var growth = _hero != null ? GrowthPlan.ValueOf(stat, _hero.GrowthBonus) : 0;

                _growthWindow.SetStat(stat, level, baseValue, growth);
                _growthWindow.SetCost(stat, _growthManager.CostToUpgrade(stat),
                    _growthManager.CanUpgrade(stat), _growthManager.IsMaxLevel(stat));
            }
        }

        private void BindHero(Character hero)
        {
            var profile = _profiles.Get(hero.ID);

            _heroStatus.SetName(profile.DisplayName);
            _heroStatus.SetPortrait(profile.Portrait);

            // 최대치도 같이 구독한다 — 성장으로 MaxHP 가 변해도 "현재/최대" 가 그 자리에서 맞는다.
            hero.CurrentHP
                .CombineLatest(hero.CurrentStats, (hp, stats) => (hp, max: stats.MaxHP))
                .Subscribe(this, (pair, self) => self._heroStatus.SetHP(pair.hp, pair.max))
                .RegisterTo(hero.destroyCancellationToken);

            // 성장 모달은 캐릭터 자체(기본+성장)만 다룬다 — 장비 몫은 장비창이 보여준다.
            _hero = hero;
            hero.CurrentStats
                .Subscribe(this, (_, self) => self.RefreshGrowthWindow())
                .RegisterTo(hero.destroyCancellationToken);

            // 우측 하단 두 버튼 = 스킬·궁극기. 평타는 버튼이 없다 — 알아서 4콤보를 돈다.
            _skillButton.SetText(hero.Combat.Skill != null ? hero.Combat.Skill.Name : "스킬");
            BindSkillButton(hero.Combat, hero.Combat.SkillCooldown, hero.Combat.Skill,
                _skillButton, hero.destroyCancellationToken);

            _ultimateButton.SetText(hero.Combat.Ultimate != null ? hero.Combat.Ultimate.Name : "궁극기");
            BindSkillButton(hero.Combat, hero.Combat.UltimateCooldown, hero.Combat.Ultimate,
                _ultimateButton, hero.destroyCancellationToken);
        }

        /// <summary>
        /// 버튼이 아는 건 남은 비율과 남은 초, 그리고 지금 누를 수 있나뿐이다. 해석은 캐릭터가 이미 해뒀다.
        /// 켜짐 조건은 캐릭터의 CanUseSkill 과 같은 재료를 본다 — 눌러도 아무 일 없는 버튼은 켜두지 않는다.
        /// </summary>
        private static void BindSkillButton(CharacterCombat combat, CooldownTimer cooldown,
            CharacterSkill skill, SkillButtonWidget button, CancellationToken token)
        {
            cooldown.RemainRatio
                .Subscribe(button, (ratio, self) => self.SetCooldownRatio(ratio))
                .RegisterTo(token);

            cooldown.RemainSeconds
                .Subscribe(button, (remain, self) => self.SetCooldownRemain(remain))
                .RegisterTo(token);

            cooldown.RemainSeconds
                .CombineLatest(combat.Casting, (remain, casting) => skill != null && remain <= 0f && !casting)
                .Subscribe(button, (canUse, self) => self.SetInteractable(canUse))
                .RegisterTo(token);
        }
    }
}
