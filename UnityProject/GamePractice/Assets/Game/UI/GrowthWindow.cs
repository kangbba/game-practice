using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 성장 모달. 성장과 재화를 구독해서 스스로 다시 그리고, 강화 버튼도 스스로 성장 매니저에게 넘긴다.
    /// 값은 열려 있든 닫혀 있든 늘 맞춰져 있다 — 열리는 순간 이미 그려져 있다.
    /// 성장은 저절로 오르지 않는다. 버튼을 눌러 골드를 내면 그때 한 칸 오른다.
    /// 줄 순서는 GrowthPlan.All 과 같다 — 빌더가 그 순서로 꽂는다.
    /// 왼쪽엔 지금 영웅이 입은 그대로의 모습을 장비창과 같은 프리뷰 무대로 비춘다.
    /// </summary>
    public class GrowthWindow : PopupWindow
    {
        [SerializeField] private Button _closeBtn;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private GrowthStatWidget[] _statWidgets;
        [SerializeField] private RawImage _preview;

        private readonly Subject<StatType> _upgradeRequested = new Subject<StatType>();

        private GrowthManager _growthManager;
        private CurrencyManager _currencyManager;
        private IAssets<Hero> _heroAssets;
        private CharacterPreviewStage _previewStage;

        /// <summary>"기본 + 성장" 을 그릴 때 보는 히어로. 부활하면 새 히어로로 갈린다.</summary>
        private Character _hero;

        private void Awake()
        {
            _closeBtn.onClick.AsObservable()
                .Subscribe(this, (_, self) => self.Close())
                .AddTo(this);

            for (var i = 0; i < _statWidgets.Length && i < GrowthPlan.All.Length; i++)
            {
                var stat = GrowthPlan.All[i];

                _statWidgets[i].Clicked
                    .Subscribe((self: this, stat), (_, state) => state.self._upgradeRequested.OnNext(state.stat))
                    .AddTo(this);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _upgradeRequested.Dispose();
        }

        public void Init(GrowthManager growthManager, CurrencyManager currencyManager, HeroManager heroManager,
            IAssets<Hero> heroAssets)
        {
            _growthManager = growthManager;
            _currencyManager = currencyManager;
            _heroAssets = heroAssets;

            _previewStage = new CharacterPreviewStage(_preview);

            // 인형은 창이 열려 있는 동안만 돌린다.
            IsOpen
                .Subscribe(this, (isOpen, self) => self._previewStage.SetActive(isOpen))
                .AddTo(this);

            // 살 수 있는지는 성장 매니저가 마지막으로 판단한다 — 여기는 눌렸다는 사실만 넘긴다.
            _upgradeRequested
                .Subscribe(growthManager, (stat, manager) => manager.TryUpgrade(stat))
                .AddTo(this);

            // 한 줄을 사면 골드가 줄어 다른 줄의 버튼도 같이 흔들린다 — 그래서 늘 창 전체를 다시 그린다.
            currencyManager.Gold
                .Subscribe(this, (_, self) => self.Redraw())
                .AddTo(this);

            growthManager.Bonus
                .Subscribe(this, (_, self) => self.Redraw())
                .AddTo(this);

            heroManager.Spawned
                .Subscribe(this, (character, self) =>
                {
                    if (character is Hero hero)
                    {
                        self.SetHero(hero);
                    }
                })
                .AddTo(this);
        }

        /// <summary>성장 모달은 캐릭터 자체(기본 + 성장)만 다룬다 — 장비 몫은 장비창이 보여준다.</summary>
        private void SetHero(Hero hero)
        {
            _hero = hero;

            // 인형은 같은 프리팹의 빈 몸이다. 아래 구독이 즉시 한 번 돌면서 지금 입은 한 벌이 그대로 입혀진다.
            _previewStage.SetDoll(_heroAssets.Get(hero.ID));

            foreach (var slot in EquipmentSlots.All)
            {
                hero.Equipment.Observe(slot)
                    .Subscribe((self: this, slot), (part, state) => state.self._previewStage.Wear(state.slot, part?.Visual))
                    .AddTo(hero);
            }

            hero.CurrentStats
                .Subscribe(this, (_, self) => self.Redraw())
                .AddTo(hero);
        }

        private void Redraw()
        {
            _goldText.text = $"보유 골드  <color=#F0C776>{_currencyManager.Gold.CurrentValue:N0}</color>";

            foreach (var stat in GrowthPlan.All)
            {
                var widget = Widget(stat);

                if (widget == null)
                {
                    continue;
                }

                widget.SetLevel(StatTypes.DisplayName(stat), _growthManager.StatLevel(stat).CurrentValue);
                widget.SetValue(
                    _hero != null ? (int)_hero.BaseStats.Get(stat) : 0,
                    _hero != null ? (int)_hero.GrowthBonus.Get(stat) : 0);
                widget.SetCost(_growthManager.CostToUpgrade(stat),
                    _growthManager.CanUpgrade(stat), _growthManager.IsMaxLevel(stat));
            }
        }

        private GrowthStatWidget Widget(StatType stat)
        {
            var index = Array.IndexOf(GrowthPlan.All, stat);

            return index >= 0 && index < _statWidgets.Length ? _statWidgets[index] : null;
        }
    }
}
