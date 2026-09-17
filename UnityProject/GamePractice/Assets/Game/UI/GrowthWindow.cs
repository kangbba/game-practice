using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 성장 모달. 계산하지 않는다 — 항목별 레벨·값·살 수 있는지를 패널이 구독해서 넣어 주고,
    /// 이 창은 받은 대로 그리고 "이 항목을 올려달라" 만 흘린다.
    /// 줄 순서는 GrowthPlan.All 과 같다 — 빌더가 그 순서로 꽂는다.
    /// </summary>
    public class GrowthWindow : PopupWindow
    {
        [SerializeField] private Button _closeBtn;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private GrowthStatWidget[] _statWidgets;

        private readonly Subject<GrowthStatType> _upgradeRequested = new Subject<GrowthStatType>();

        /// <summary>강화를 요청한 항목. 실제로 살 수 있는지는 성장 매니저가 마지막으로 판단한다.</summary>
        public Observable<GrowthStatType> UpgradeRequested => _upgradeRequested;

        private void Awake()
        {
            _closeBtn.onClick.AsObservable()
                .Subscribe(this, (_, self) => self.Hide())
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

        /// <summary>가진 골드. 값을 보면서 살지 말지 정하는 창이라 액수가 창 안에 있어야 한다.</summary>
        public void SetGold(long gold)
        {
            _goldText.text = $"보유 골드  <color=#F0C776>{gold:N0}</color>";
        }

        public void SetStat(GrowthStatType stat, int level, int baseValue, int growth)
        {
            var widget = Widget(stat);

            if (widget != null)
            {
                widget.SetLevel(GrowthPlan.DisplayName(stat), level);
                widget.SetValue(baseValue, growth);
            }
        }

        public void SetCost(GrowthStatType stat, long cost, bool affordable, bool maxed)
        {
            var widget = Widget(stat);

            if (widget != null)
            {
                widget.SetCost(cost, affordable, maxed);
            }
        }

        private GrowthStatWidget Widget(GrowthStatType stat)
        {
            var index = Array.IndexOf(GrowthPlan.All, stat);

            return index >= 0 && index < _statWidgets.Length ? _statWidgets[index] : null;
        }
    }
}
