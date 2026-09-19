using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 좌상단 히어로 명패: 초상화·이름·레벨·HP바·EXP바. 지금 싸우는 히어로가 누구인지와 그 상태를 보여준다.
    /// 히어로가 바뀌는 것도 스스로 듣는다 — 부르는 쪽은 볼 스트림만 넘긴다. 그게 어느 매니저의 것인지는 모른다.
    /// </summary>
    public class HeroProfileWidget : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private SlicedFillBar _hpFill;
        [SerializeField] private TextMeshProUGUI _hpLabel;
        [SerializeField] private SlicedFillBar _expFill;

        public void Init(Observable<Hero> hero, Observable<int> level, Observable<float> expRatio, IAssets<CharacterProfile> profiles)
        {
            // 레벨과 경험치는 히어로가 죽고 바뀌어도 이어진다 — 한 번만 건다.
            level
                .Subscribe(this, (value, self) => self._levelText.text = $"Lv.{value}")
                .AddTo(this);

            expRatio
                .Subscribe(this, (ratio, self) => self._expFill.FillAmount = ratio)
                .AddTo(this);

            hero
                .Where(current => current != null)
                .Subscribe((self: this, profiles), (current, state) => state.self.SetHero(current, state.profiles.Get(current.ID)))
                .AddTo(this);
        }

        /// <summary>히어로가 바뀔 때마다. HP 구독은 그 히어로의 수명을 따라간다.</summary>
        private void SetHero(Character hero, CharacterProfile profile)
        {
            _portrait.sprite = profile.Portrait;
            _nameText.text = profile.DisplayName;

            // 최대치도 같이 구독한다 — 성장으로 MaxHP 가 변해도 "현재/최대" 가 그 자리에서 맞는다.
            hero.CurrentHP
                .CombineLatest(hero.CurrentStats, (hp, stats) => (hp, max: (int)stats.Get(StatType.MaxHP)))
                .Subscribe(this, (pair, self) => self.DrawHP(pair.hp, pair.max))
                .AddTo(hero);
        }

        /// <summary>구독 없이 최종 모습만 그린다. 프리팹을 구울 때 빈 명패로 남지 않게 하는 용도다.</summary>
        public void Preview(string heroName, int level, int hp, int maxHP, float expRatio)
        {
            _nameText.text = heroName;
            _levelText.text = $"Lv.{level}";
            _expFill.FillAmount = expRatio;

            DrawHP(hp, maxHP);
        }

        private void DrawHP(int current, int max)
        {
            _hpFill.FillAmount = max > 0 ? (float)current / max : 0f;
            _hpLabel.text = $"{current}/{max}";
        }
    }
}
