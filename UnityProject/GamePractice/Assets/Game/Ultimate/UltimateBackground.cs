using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 궁극기 백그라운드. 카메라 자식으로 붙어 화면을 꽉 채우는 판 한 장이다.
    /// 그려지는 순서는 거리가 아니라 정렬 레이어가 정한다 — 맵·평소 캐릭터 위, 무대 위 캐릭터 아래.
    /// 어떤 색으로 깔리는지는 여기서 정하고, 언제 몇 초에 걸쳐 깔지는 연출(UltimateDirector)이 정한다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(SkyCard))]
    public sealed class UltimateBackground : MonoBehaviour
    {
        /// <summary>다 깔렸을 때의 색. 알파 244 라 맵이 아주 희미하게만 비친다.</summary>
        private static readonly Color ShownColor = new Color32(0x0E, 0x00, 0x24, 244);

        private SpriteRenderer _renderer;

        /// <summary>카메라 자식으로 만든다. 처음엔 꺼져 있다.</summary>
        public static UltimateBackground Create(Camera camera)
        {
            var texture = Texture2D.whiteTexture;
            var card = new GameObject(nameof(UltimateBackground), typeof(SpriteRenderer), typeof(SkyCard));
            card.transform.SetParent(camera.transform, false);

            var background = card.AddComponent<UltimateBackground>();
            background._renderer = card.GetComponent<SpriteRenderer>();
            background._renderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), texture.width);
            background._renderer.sortingLayerName = SortingLayers.UltimateBackground;
            background._renderer.color = WithAlpha(0f);
            background._renderer.enabled = false;

            return background;
        }

        /// <summary>켜고 어둡게 깐다.</summary>
        public UniTask ShowAsync(float seconds, CancellationToken token)
        {
            _renderer.enabled = true;
            return FadeAsync(ShownColor.a, seconds, token);
        }

        /// <summary>걷고 나서 끈다.</summary>
        public async UniTask HideAsync(float seconds, CancellationToken token)
        {
            await FadeAsync(0f, seconds, token);
            _renderer.enabled = false;
        }

        private UniTask FadeAsync(float alpha, float seconds, CancellationToken token)
        {
            var done = new UniTaskCompletionSource();

            _renderer.DOFade(alpha, seconds)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject)
                .OnComplete(() => done.TrySetResult());

            return done.Task.AttachExternalCancellation(token);
        }

        private static Color WithAlpha(float alpha)
        {
            var color = ShownColor;
            color.a = alpha;
            return color;
        }
    }
}
