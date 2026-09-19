using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Sayne
{
    /// <summary>
    /// 전투 HUD 의 주인. 한 벌뿐이라 게임 내내 사는 화면 캔버스를 만들고 그 위에 BattlePanel 을 올린다.
    /// HUD 가 무엇을 보여 줄지는 모른다 — 매니저를 이어 주는 건 조립하는 쪽(GameManager)이 한다.
    /// 캐릭터마다 딸리는 HP 바·데미지 숫자는 CharacterUIManager 가 맡는다.
    /// </summary>
    public class ScreenUIManager : ManagerBase
    {
        private const string RootName = "ScreenUIRoot";

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private readonly BattlePanel _battlePanelPrefab;

        private Canvas _canvas;

        /// <summary>전투 HUD. OnInit 이후 접근 가능.</summary>
        public BattlePanel BattlePanel { get; private set; }

        public ScreenUIManager(BattlePanel battlePanelPrefab)
        {
            _battlePanelPrefab = battlePanelPrefab;
        }

        protected override void OnInit()
        {
            var root = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 0;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            BattlePanel = Object.Instantiate(_battlePanelPrefab, _canvas.transform);
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
