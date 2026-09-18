using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 지금 화면을 한 장 찍어 작게 줄여 둔다. 팝업이 이걸 흐림 셰이더로 늘여 깔면 뒤 배경이 뿌옇게 보인다.
    /// 매 프레임 흐리는 게 아니라 여는 순간 한 번만 찍는다 — 팝업이 열려 있는 동안 게임은 멈춰 있다.
    /// 찍은 그림은 하나를 돌려쓴다. 팝업은 한 번에 하나만 열린다.
    /// </summary>
    public class ScreenBlurManager : ManagerBase
    {
        /// <summary>반씩 몇 번 줄이나. 세 번이면 1/8 — 이중선형으로 줄이는 것만으로도 이미 한 번 번진다.</summary>
        private const int HalvingCount = 3;

        private RenderTexture _screen;
        private readonly RenderTexture[] _steps = new RenderTexture[HalvingCount];

        protected override void OnInit()
        {
        }

        protected override void OnRelease()
        {
            ReleaseTextures();
        }

        /// <summary>이번 프레임이 다 그려진 뒤의 화면. 부른 프레임의 화면이 찍힌다.</summary>
        public async UniTask<Texture> CaptureAsync(CancellationToken token)
        {
            await UniTask.WaitForEndOfFrame(token);

            PrepareTextures(Screen.width, Screen.height);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(_screen);

            // 화면 캡처는 텍스처 원점이 위인 그래픽 API(Metal·DX·Vulkan)에서 위아래가 뒤집혀 나온다. 첫 번째 축소에서 바로잡는다.
            var isFlipped = SystemInfo.graphicsUVStartsAtTop;
            Graphics.Blit(_screen, _steps[0], new Vector2(1f, isFlipped ? -1f : 1f), new Vector2(0f, isFlipped ? 1f : 0f));

            for (var i = 1; i < HalvingCount; i++)
            {
                Graphics.Blit(_steps[i - 1], _steps[i]);
            }

            return _steps[HalvingCount - 1];
        }

        /// <summary>화면 크기가 그대로면 있던 걸 쓴다. 회전 등으로 바뀌었으면 새로 만든다.</summary>
        private void PrepareTextures(int width, int height)
        {
            if (_screen != null && _screen.width == width && _screen.height == height)
            {
                return;
            }

            ReleaseTextures();

            _screen = new RenderTexture(width, height, 0);

            for (var i = 0; i < HalvingCount; i++)
            {
                var scale = 1 << (i + 1);
                _steps[i] = new RenderTexture(Mathf.Max(1, width / scale), Mathf.Max(1, height / scale), 0)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
        }

        private void ReleaseTextures()
        {
            if (_screen == null)
            {
                return;
            }

            _screen.Release();
            Object.Destroy(_screen);

            for (var i = 0; i < HalvingCount; i++)
            {
                _steps[i].Release();
                Object.Destroy(_steps[i]);
                _steps[i] = null;
            }

            _screen = null;
        }
    }
}
