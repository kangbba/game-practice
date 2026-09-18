using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Sayne
{
    /// <summary>
    /// 화면을 덮고 잠깐 흐르다 사라지는 화면 연출의 주인. 연출 프리팹을 들고 있다가, 그 순간을 구독해서 스스로 띄우고 치운다.
    /// 게임 쪽은 이 매니저를 모른다 — 궁극기가 나갔다, 웨이브가 시작됐다는 신호를 듣고 끼어들 뿐이다.
    ///
    /// 궁극기 컷인: 도는 동안 게임이 멈춘다. 그 연결은 "IsPerforming 인 동안 중단" 한 줄이다.
    /// 컷인만은 신호를 듣지 않고 불려서 돈다 — 궁극기 연출의 한 단계라 UltimateDirector 가 순서에 맞춰 부르고 기다린다.
    /// 웨이브 시작 알림: 게임을 멈추지 않는다. 잠깐 떴다가 혼자 사라진다.
    /// 저체력 경고: 피가 얼마 안 남은 동안 계속 떠 있다. 히어로 체력을 구독해 저절로 켜고 끈다.
    /// </summary>
    public class ScreenPerformanceManager : ManagerBase
    {
        private const string RootName = "ScreenPerformanceRoot";

        /// <summary>전투 HUD(0)와 창들 위. 컷인 동안은 아래 UI 가 눌리면 안 되므로 레이캐스트도 여기서 막는다.</summary>
        private const int SortingOrder = 100;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private readonly PauseManager _pauseManager;
        private readonly HeroManager _heroManager;
        private readonly WaveManager _waveManager;
        private readonly IAssets<CharacterProfile> _profiles;
        private readonly UltimateCutscenePanel _ultimatePanelPrefab;
        private readonly WaveStartPanel _waveStartPanelPrefab;
        private readonly LowHealthPanel _lowHealthPanelPrefab;

        /// <summary>이 비율 아래로 떨어지면 위급하다고 본다.</summary>
        private const float LowHealthRatio = 0.3f;

        private readonly ReactiveProperty<bool> _isPerforming = new ReactiveProperty<bool>(false);

        private Canvas _canvas;
        private LowHealthPanel _lowHealthPanel;

        public ReadOnlyReactiveProperty<bool> IsPerforming => _isPerforming;

        public ScreenPerformanceManager(PauseManager pauseManager, HeroManager heroManager, WaveManager waveManager,
            IAssets<CharacterProfile> profiles, UltimateCutscenePanel ultimatePanelPrefab,
            WaveStartPanel waveStartPanelPrefab, LowHealthPanel lowHealthPanelPrefab)
        {
            _pauseManager = pauseManager;
            _heroManager = heroManager;
            _waveManager = waveManager;
            _profiles = profiles;
            _ultimatePanelPrefab = ultimatePanelPrefab;
            _waveStartPanelPrefab = waveStartPanelPrefab;
            _lowHealthPanelPrefab = lowHealthPanelPrefab;
        }

        protected override void OnInit()
        {
            CreateCanvas();

            _pauseManager.PauseWhile(_isPerforming);

            _waveManager.WaveStarted
                .Subscribe(this, (number, self) => self.PlayWaveStartAsync(number.stage, number.wave).Forget())
                .RegisterTo(LifeToken);

            CreateLowHealthPanel();
        }

        protected override void OnRelease()
        {
            if (_canvas != null)
            {
                Object.Destroy(_canvas.gameObject);
            }

            _canvas = null;
            _isPerforming.Dispose();
        }

        private void CreateCanvas()
        {
            var root = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = SortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
        }

        /// <summary>궁극기 컷인을 틀고 끝날 때까지 기다린다. 도는 동안 게임은 멈춰 있다.</summary>
        public async UniTask PlayUltimateCutsceneAsync(Hero hero, CancellationToken token)
        {
            var profile = _profiles.Get(hero.ID);

            _isPerforming.Value = true;

            var panel = Object.Instantiate(_ultimatePanelPrefab, _canvas.transform);
            await panel.PlayAsync(profile.Portrait, profile.DisplayName, hero.Combat.Ultimate.Name,
                profile.ThemeColor, token);
            Object.Destroy(panel.gameObject);

            _isPerforming.Value = false;
        }

        /// <summary>
        /// 저체력 경고. 화면에 미리 만들어 꺼 둔 채로, 지금 히어로의 체력 비율만 구독한다.
        /// 히어로가 죽고 새로 태어나도 그 히어로의 체력으로 갈아 문다 — 죽는 순간 경고도 같이 꺼진다.
        /// </summary>
        private void CreateLowHealthPanel()
        {
            _lowHealthPanel = Object.Instantiate(_lowHealthPanelPrefab, _canvas.transform);
            _lowHealthPanel.SetVisible(false);

            _heroManager.Spawned
                .Subscribe(this, (hero, self) => self.WatchHealth(hero))
                .RegisterTo(LifeToken);
        }

        private void WatchHealth(Character hero)
        {
            hero.CurrentHP
                .CombineLatest(hero.CurrentStats, (hp, stats) => hp > 0 && hp / stats.Get(StatType.MaxHP) <= LowHealthRatio)
                .DistinctUntilChanged()
                .Subscribe(this, (isLow, self) => self._lowHealthPanel.SetVisible(isLow))
                .RegisterTo(hero.destroyCancellationToken);
        }

        /// <summary>웨이브 시작 알림. 1.5초 떠 있다가 사라진다. 게임은 멈추지 않는다.</summary>
        private async UniTaskVoid PlayWaveStartAsync(int stage, int wave)
        {
            var panel = Object.Instantiate(_waveStartPanelPrefab, _canvas.transform);
            panel.Init(_waveManager.GetLabel(stage, wave));

            // 창이 열려 게임이 멈춰 있어도 알림은 제 시간에 사라진다. 패널 연출도 unscaled 로 돈다.
            await UniTask.Delay(TimeSpan.FromSeconds(1.5f), DelayType.UnscaledDeltaTime, cancellationToken: LifeToken);

            Object.Destroy(panel.gameObject);
        }
    }
}
