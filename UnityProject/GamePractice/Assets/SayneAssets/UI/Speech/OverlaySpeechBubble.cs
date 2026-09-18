using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 오버레이 캔버스 위에서 캐릭터 머리를 따라다니는 말풍선. OverlayHPBar 와 같은 방식으로 붙인다 —
    /// 월드의 머리 지점을 스크린으로 옮긴 뒤 픽셀 오프셋을 더하므로 거리와 상관없이 크기가 일정하다.
    /// 본체는 화면 전체를 덮은 채 입력만 받고, 실제로 움직이는 건 안쪽의 _follower 다. 꼬리 끝이 _follower 의 원점이다.
    /// 대상이 화면 밖이어도 풍선은 가장자리에 붙어 남는다 — 게임을 멈춰 놓고 읽히는 대사라 안 보이면 아무것도 못 한다.
    /// </summary>
    public class OverlaySpeechBubble : MonoBehaviour
    {
        /// <summary>풍선이 화면 가장자리에서 떨어져 있는 최소 거리. 캔버스 기준 해상도 단위.</summary>
        private const float ScreenMargin = 24f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Button _tapBtn;
        [SerializeField] private RectTransform _follower;
        [SerializeField] private TextPlayer _player;

        private Camera _camera;
        private Transform _target;

        /// <summary>캐릭터 발에서 머리까지의 월드 오프셋.</summary>
        private Vector3 _worldOffset;

        /// <summary>머리 지점에서 화면상으로 더 띄우는 값. 캔버스 기준 해상도 단위.</summary>
        private Vector2 _screenOffset;

        private RectTransform _bubbleRect;

        private void Awake()
        {
            _bubbleRect = (RectTransform)_player.transform;
            _tapBtn.onClick.AddListener(_player.Advance);
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        public void Attach(Camera camera, Transform target, Vector3 worldOffset, Vector2 screenOffset)
        {
            _camera = camera;
            _target = target;
            _worldOffset = worldOffset;
            _screenOffset = screenOffset;

            Place();

            // 카메라와 같은 단계(PostLateUpdate)에서, 카메라보다 늦게 구독해 그 뒤에 돈다 —
            // 순서가 뒤바뀌면 카메라가 움직이는 동안 풍선이 몸에서 한 프레임 밀린다.
            Observable.EveryUpdate(UnityFrameProvider.PostLateUpdate)
                .Subscribe(this, (_, self) => self.Place())
                .AddTo(this);
        }

        public async UniTask PlayAsync(string text, CancellationToken token)
        {
            _group.alpha = 1f;
            _group.blocksRaycasts = true;

            await _player.PlayAsync(text, token);

            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        private void Place()
        {
            var screenPoint = _camera.WorldToScreenPoint(_target.position + _worldOffset);

            // 카메라 뒤의 점은 화면 좌표가 뒤집혀 엉뚱한 곳에 찍힌다. 부호를 되돌려 원래 방향의 화면 밖으로 보낸 뒤 가장자리로 민다.
            if (screenPoint.z < 0f)
            {
                screenPoint = -screenPoint;
            }

            var parent = (RectTransform)_follower.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out var localPoint);
            _follower.anchoredPosition = KeepOnScreen(localPoint + _screenOffset, parent.rect);
        }

        /// <summary>풍선 전체가 화면 안에 남도록 따라가는 지점을 민다. 풍선은 _follower 원점에서 떨어져 있으므로 그 간격까지 셈에 넣는다.</summary>
        private Vector2 KeepOnScreen(Vector2 point, Rect limit)
        {
            var bubble = _bubbleRect.rect;
            var offset = _bubbleRect.anchoredPosition;

            var min = new Vector2(limit.xMin + ScreenMargin - (offset.x + bubble.xMin),
                limit.yMin + ScreenMargin - (offset.y + bubble.yMin));
            var max = new Vector2(limit.xMax - ScreenMargin - (offset.x + bubble.xMax),
                limit.yMax - ScreenMargin - (offset.y + bubble.yMax));

            return new Vector2(Mathf.Clamp(point.x, min.x, max.x), Mathf.Clamp(point.y, min.y, max.y));
        }
    }
}
