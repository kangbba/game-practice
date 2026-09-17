using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>스크린 스페이스 캔버스를 만들고, 페이즈 전용 UI 프리팹을 붙여 PhaseUIManager에 등록한다.</summary>
    public class ScreenUIManager : ManagerBase
    {
        private const string RootName = "ScreenUIRoot";
        private const string EventSystemName = "EventSystem";

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private readonly PauseManager _pauseManager;
        private readonly PhaseUIManager _phaseUIManager;
        private readonly HeroManager _heroManager;
        private readonly WaveManager _waveManager;
        private readonly CurrencyManager _currencyManager;
        private readonly GrowthManager _growthManager;
        private readonly IAssets<CharacterProfile> _profiles;
        private readonly BattlePhaseUIPanel _battlePanelPrefab;

        private Canvas _canvas;

        /// <summary>전투 HUD. OnInit 이후 접근 가능.</summary>
        public BattlePhaseUIPanel BattlePanel { get; private set; }

        public ScreenUIManager(PauseManager pauseManager, PhaseUIManager phaseUIManager, HeroManager heroManager,
            WaveManager waveManager, CurrencyManager currencyManager, GrowthManager growthManager,
            IAssets<CharacterProfile> profiles, BattlePhaseUIPanel battlePanelPrefab)
        {
            _pauseManager = pauseManager;
            _phaseUIManager = phaseUIManager;
            _heroManager = heroManager;
            _waveManager = waveManager;
            _currencyManager = currencyManager;
            _growthManager = growthManager;
            _profiles = profiles;
            _battlePanelPrefab = battlePanelPrefab;
        }

        protected override void OnInit()
        {
            var root = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            if (EventSystem.current == null)
            {
                new GameObject(EventSystemName, typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            BattlePanel = Object.Instantiate(_battlePanelPrefab, _canvas.transform);
            BattlePanel.Bind(_heroManager, _waveManager, _currencyManager, _growthManager, _profiles);
            _phaseUIManager.RegisterView(BattlePanel);

            // 창이 하나라도 열려 있는 동안 게임은 멈춘다.
            foreach (var popup in BattlePanel.Popups)
            {
                _pauseManager.PauseWhile(popup.IsOpen);
            }
        }

        protected override void OnRelease()
        {
            if (_canvas != null)
            {
                Object.Destroy(_canvas.gameObject);
            }

            _canvas = null;
            BattlePanel = null;
        }
    }
}
