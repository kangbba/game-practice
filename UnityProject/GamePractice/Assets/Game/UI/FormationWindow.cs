using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 편성 모달. 위에는 파티 칸 넷, 아래에는 영웅 명단과 적용 버튼.
    /// 파티 칸은 첫 칸만 진짜다 — 지금 싸우는 영웅을 입은 그대로 비춘다. 나머지 셋은 잠긴 장식이다(나중의 파티 사냥 자리).
    /// 명단에서 영웅을 고르고 적용을 누르면 히어로 매니저가 첫 칸의 영웅을 바꾼다. 창은 지금 영웅(CurrentHero)을 구독해 따라 그릴 뿐이다.
    /// 쓰러져 부활을 기다리는 동안은 적용이 막힌다.
    /// </summary>
    public class FormationWindow : PopupWindow
    {
        [SerializeField] private Button _closeBtn;
        [SerializeField] private Button _applyBtn;
        [SerializeField] private RawImage _leaderPreview;
        [SerializeField] private TextMeshProUGUI _leaderName;
        [SerializeField] private FormationHeroWidget[] _heroWidgets;
        [SerializeField] private GameObject _downHint;

        /// <summary>명단에서 고른 영웅. 적용 전까지는 고르기만 한 것이다.</summary>
        private readonly ReactiveProperty<string> _selected = new ReactiveProperty<string>(string.Empty);

        private HeroManager _heroManager;
        private IAssets<Hero> _heroAssets;
        private IAssets<CharacterProfile> _profiles;
        private CharacterPreviewStage _previewStage;

        private void Awake()
        {
            _closeBtn.onClick.AsObservable()
                .Subscribe(this, (_, self) => self.Close())
                .AddTo(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _selected.Dispose();
        }

        public void Init(HeroManager heroManager, IAssets<Hero> heroAssets, IAssets<CharacterProfile> profiles)
        {
            _heroManager = heroManager;
            _heroAssets = heroAssets;
            _profiles = profiles;

            _previewStage = new CharacterPreviewStage(_leaderPreview);

            // 인형은 창이 열려 있는 동안만 돌린다.
            IsOpen
                .Subscribe(this, (isOpen, self) => self._previewStage.SetActive(isOpen))
                .AddTo(this);

            foreach (var widget in _heroWidgets)
            {
                widget.Clicked
                    .Subscribe(this, (heroID, self) => self._selected.Value = heroID)
                    .AddTo(this);

                _selected
                    .Subscribe(widget, (heroID, card) => card.SetSelected(card.HeroID == heroID))
                    .AddTo(this);
            }

            _applyBtn.onClick.AsObservable()
                .Subscribe(this, (_, self) => self._heroManager.ChangeLeader(self._selected.Value))
                .AddTo(this);

            // 부활까지 남은 시간이 있으면 쓰러져 있는 것이다. 그동안은 바꿀 영웅이 서 있지 않다.
            var isDown = heroManager.ReviveRemainTime
                .Select(remain => remain > 0f)
                .DistinctUntilChanged();

            isDown
                .Subscribe(this, (down, self) => self._downHint.SetActive(down))
                .AddTo(this);

            // 적용은 지금 출전 중이 아닌 영웅을 골랐고, 서 있을 때만 된다.
            heroManager.CurrentHero
                .Where(hero => hero != null)
                .CombineLatest(_selected, isDown, (hero, selected, down) => !down && selected != hero.ID)
                .Subscribe(this, (canApply, self) => self._applyBtn.interactable = canApply)
                .AddTo(this);

            heroManager.CurrentHero
                .Where(hero => hero != null)
                .Subscribe(this, (hero, self) => self.SetHero(hero))
                .AddTo(this);
        }

        /// <summary>열 때마다 고름을 지금 출전 중인 영웅으로 되돌린다.</summary>
        protected override void OnShow()
        {
            _selected.Value = _heroManager.CurrentHero.CurrentValue.ID;
        }

        private void SetHero(Hero hero)
        {
            _leaderName.text = _profiles.Get(hero.ID).DisplayName;
            _selected.Value = hero.ID;

            foreach (var widget in _heroWidgets)
            {
                widget.SetDeployed(widget.HeroID == hero.ID);
            }

            // 인형은 같은 프리팹의 빈 몸이다. 아래 구독이 즉시 한 번 돌면서 지금 장착한 장비 세트가 그대로 장착된다.
            _previewStage.SetDoll(_heroAssets.Get(hero.ID));

            foreach (var slot in EquipmentSlots.All)
            {
                hero.Equipment.Observe(slot)
                    .Subscribe((self: this, slot), (part, state) => state.self._previewStage.Wear(state.slot, part?.Visual))
                    .AddTo(hero);
            }
        }
    }
}
