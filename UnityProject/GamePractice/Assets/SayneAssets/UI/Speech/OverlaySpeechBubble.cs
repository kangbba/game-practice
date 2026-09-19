using System;
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
    /// 두 가지로 튼다 — 눌러서 넘기는 대사(PlayAsync)와, 게임을 멈추지 않고 알아서 사라지는 혼잣말(SayAsync).
    /// 한 번 말하고 치워지는 일회용이다. 만들고 치우는 건 만든 쪽 일이다.
    /// </summary>
    public class OverlaySpeechBubble : MonoBehaviour
    {
        /// <summary>풍선이 화면 가장자리에서 떨어져 있는 최소 거리. 캔버스 기준 해상도 단위.</summary>
        private const float ScreenMargin = 24f;

        /// <summary>혼잣말: 다 찍힌 뒤 머무는 시간. 글이 길수록 조금 더 머문다.</summary>
        private const float SayHoldSeconds = 1.2f;
        private const float SayHoldSecondsPerChar = 0.05f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Button _tapBtn;
        [SerializeField] private RectTransform _follower;
        [SerializeField] private TextPlayer _player;

        /// <summary>말하는 이 얼굴 칸. 안 꽂은 프리팹은 얼굴 없이 글만 쓴다. 꽂았으면 얼굴을 받았을 때만 켠다.</summary>
        [SerializeField] private Image _portrait;

        private Camera _camera;
        private Transform _target;

        /// <summary>캐릭터 발에서 머리까지의 월드 오프셋.</summary>
        private Vector3 _worldOffset;

        /// <summary>머리 지점에서 화면상으로 더 띄우는 값. 캔버스 기준 해상도 단위.</summary>
        private Vector2 _screenOffset;

        private RectTransform _bubbleRect;

        /// <summary>따라다니는 구독. 대상이 사라지면 끊고 그 자리에 남는다.</summary>
        private IDisposable _follow;

        /// <param name="portrait">말하는 이 얼굴. null 이면 칸을 끈다. 칸 없는 프리팹이면 쓰지 않는다.</param>
        public void Init(Camera camera, Transform target, Vector3 worldOffset, Vector2 screenOffset, Sprite portrait)
        {
            _bubbleRect = (RectTransform)_player.transform;
            _tapBtn.onClick.AddListener(_player.Advance);
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            ShowPortrait(portrait);

            _camera = camera;
            _target = target;
            _worldOffset = worldOffset;
            _screenOffset = screenOffset;

            Place();

            // 카메라와 같은 단계(PostLateUpdate)에서, 카메라보다 늦게 구독해 그 뒤에 돈다 —
            // 순서가 뒤바뀌면 카메라가 움직이는 동안 풍선이 몸에서 한 프레임 밀린다.
            _follow = Observable.EveryUpdate(UnityFrameProvider.PostLateUpdate)
                .Subscribe(this, (_, self) => self.Place())
                .AddTo(this);
        }

        /// <summary>대사를 튼다. 플레이어가 넘기면 끝난다. 끝나도 스스로 부서지지 않는다.</summary>
        public async UniTask PlayAsync(string text, CancellationToken token)
        {
            _group.alpha = 1f;
            _group.blocksRaycasts = true;

            await _player.PlayAsync(text, token);
        }

        /// <summary>
        /// 혼잣말로 튼다. 입력을 막지 않고, 다 찍히면 잠깐 머물렀다가 스스로 넘긴다 — 누르지 않아도 사라진다.
        /// 게임을 멈추지 않는 대사라 머무는 시간은 게임 시간으로 잰다. 끝나도 스스로 부서지지 않는다.
        /// </summary>
        public async UniTask SayAsync(string text, CancellationToken token)
        {
            _group.alpha = 1f;
            _group.blocksRaycasts = false;

            var playing = _player.PlayAsync(text, token, showsNextMark: false);

            await UniTask.WaitUntil(_player, player => player.State.CurrentValue == TextPlayState.Waiting,
                cancellationToken: token);
            await UniTask.Delay(TimeSpan.FromSeconds(SayHoldSeconds + text.Length * SayHoldSecondsPerChar),
                cancellationToken: token);

            _player.Advance();
            await playing;
        }

        /// <summary>칸이 있는 프리팹이면 얼굴을 넣고, 얼굴이 없으면 칸을 끈다.</summary>
        private void ShowPortrait(Sprite portrait)
        {
            if (_portrait == null) return;

            _portrait.sprite = portrait;
            _portrait.gameObject.SetActive(portrait != null);
        }

        private void Place()
        {
            // 대상이 먼저 죽을 수 있다 — 말을 걸던 적이 화살에 맞아 사라지는 식으로.
            if (_target == null)
            {
                _follow.Dispose();
                return;
            }

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
