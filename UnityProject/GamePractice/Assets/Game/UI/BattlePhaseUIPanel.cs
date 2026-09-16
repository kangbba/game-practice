using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>Combat 페이즈 동안만 보이는 전투 HUD. PhaseUIManager가 표시를 토글한다.</summary>
    public class BattlePhaseUIPanel : PhaseUIBase
    {
        [SerializeField] private TextMeshProUGUI _reviveText;
        [SerializeField] private FloatingJoystick _joystick;
        [SerializeField] private HeroStatusWidget _heroStatus;
        [SerializeField] private SkillButtonWidget _attackButton;
        [SerializeField] private SkillButtonWidget _ultimateButton;
        [SerializeField] private Button _equipMenuButton;
        [SerializeField] private EquipmentWindow _equipmentWindow;

        public override string PhaseKey => PhaseID.Combat;

        public FloatingJoystick Joystick => _joystick;
        public Observable<Unit> AttackClicked => _attackButton.Clicked;
        public Observable<Unit> UltimateClicked => _ultimateButton.Clicked;
        public Observable<Unit> EquipMenuClicked => _equipMenuButton.onClick.AsObservable();
        public EquipmentWindow EquipmentWindow => _equipmentWindow;

        public void Bind(HeroManager heroManager)
        {
            heroManager.ReviveRemainTime
                .Subscribe(this, (remain, self) => self._reviveText.text =
                    remain > 0f ? $"부활까지 {remain:0.0}초" : string.Empty)
                .RegisterTo(destroyCancellationToken);

            heroManager.Spawned
                .Subscribe(this, (character, self) => self.BindHero(character))
                .RegisterTo(destroyCancellationToken);
        }

        private void BindHero(Character hero)
        {
            _heroStatus.SetName(hero.name.Replace("(Clone)", string.Empty));

            hero.CurrentHP
                .Subscribe((self: this, hero), (hp, state) => state.self._heroStatus.SetHP(hp, state.hero.MaxHP))
                .RegisterTo(hero.destroyCancellationToken);
        }
    }
}
