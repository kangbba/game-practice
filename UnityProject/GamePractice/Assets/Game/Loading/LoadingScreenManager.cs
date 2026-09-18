using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 로딩 화면의 주인. 다른 에셋보다 먼저, 혼자 자기 프리팹만 불러 띄운다 — 나머지 로드가 도는 동안 보여야 하기 때문이다.
    /// 그래서 에셋 창고(UIAssetManager)를 거치지 않고 제 손으로 불러 제 손으로 놓는다.
    /// </summary>
    public class LoadingScreenManager : ManagerBase
    {
        private const string RootName = "LoadingScreenRoot";

        /// <summary>무엇보다 위. 로딩 중에 뒤에서 조립되는 전투 HUD·팝업을 전부 덮는다.</summary>
        private const int SortingOrder = 1000;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private AsyncOperationHandle<GameObject> _handle;
        private Canvas _canvas;
        private LoadingPanel _panel;

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            // 탭하기 전에 게임이 꺼지면 화면이 아직 떠 있다.
            if (_canvas != null)
            {
                DestroyScreen();
            }
        }

        public async UniTask ShowAsync()
        {
            _handle = Addressables.LoadAssetAsync<GameObject>(AssetAddresses.LoadingPanel);
            var prefab = await _handle.ToUniTask(cancellationToken: LifeToken);

            var root = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = SortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            _panel = Object.Instantiate(prefab, _canvas.transform, false).GetComponent<LoadingPanel>();
        }

        /// <summary>로드가 얼마나 끝났는지. 0~1.</summary>
        public void SetProgress(float ratio)
        {
            _panel.SetProgress(ratio);
        }

        /// <summary>게이지가 다 차고, 시작 안내가 뜨고, 탭이 들어올 때까지.</summary>
        public UniTask WaitStartAsync(CancellationToken token)
        {
            return _panel.WaitStartAsync(token);
        }

        /// <summary>화면을 걷어낸다. 옅어지는 동안 뒤의 전투가 비쳐 보이고, 다 사라지면 치운다.</summary>
        public void Hide()
        {
            _panel.FadeOut().OnComplete(DestroyScreen);
        }

        private void DestroyScreen()
        {
            Object.Destroy(_canvas.gameObject);
            Addressables.Release(_handle);

            _canvas = null;
            _panel = null;
        }
    }
}
