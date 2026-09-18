using R3;
using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 오버레이 캔버스(UI) 위에서 몸을 따라다니는 HP 바.
    /// 월드의 머리 지점을 스크린으로 옮긴 뒤 픽셀 오프셋을 더한다 — 거리와 상관없이 크기와 머리 위 간격이 일정하다.
    /// 무엇을 따라다닐지만 받고 그것만 한다 — 캐릭터도 죽음도 모르고, 치우는 건 만든 쪽 일이다.
    /// </summary>
    public sealed class OverlayHPBar : MonoBehaviour
    {
        [SerializeField] private SlicedFillBar _frontFill;
        [SerializeField] private SlicedFillBar _backFill;

        /// <summary>"현재/최대" 절대수치 라벨. 안 꽂으면 바만 그린다.</summary>
        [SerializeField] private TMP_Text _label;

        /// <summary>카메라 뒤로 넘어간 동안 꺼둘 그림 루트. 본체를 끄면 따라다니기가 멈춰 다시 켤 수 없다.</summary>
        [SerializeField] private GameObject _graphic;

        private HPBarCore _core;

        private Camera _camera;
        private Transform _target;

        /// <summary>
        /// 몸의 발에서 머리 위까지의 월드 벡터. 그림이 카메라 쪽으로 누워 있어서 y 만이 아니라 z 도 들어 있다 —
        /// 재는 쪽이 그림 꼭대기를 통째로 재서 넘겨주므로 여기선 더하기만 한다.
        /// </summary>
        private Vector3 _worldOffset;

        /// <summary>머리 지점에서 화면상으로 더 띄우는 값. 캔버스 기준 해상도 단위.</summary>
        private Vector2 _screenOffset;

        private readonly HPBarMotion _motion = HPBarMotion.Default;

        /// <summary>월드에서 따라가는 지점. 스크린에서 보간하면 카메라가 움직일 때 바가 몸에서 밀린다.</summary>
        private Vector3 _anchor;

        private HPBarCore Core => _core ??= new HPBarCore(_frontFill, _backFill, _label, _motion);

        private RectTransform RectTransform => (RectTransform)transform;

        private Vector3 Destination => _target.position + _worldOffset;

        public void Attach(Camera camera, Transform target, Vector3 worldOffset, Vector2 screenOffset,
            ReadOnlyReactiveProperty<float> currentHP, ReadOnlyReactiveProperty<float> maxHP)
        {
            _camera = camera;
            _target = target;
            _worldOffset = worldOffset;
            _screenOffset = screenOffset;

            // 첫 프레임부터 제자리에 서 있어야 한다. 루프를 기다리면 한 프레임 동안 원점에 찍힌다.
            _anchor = Destination;
            Place();

            Core.Bind(currentHP, maxHP);

            // 카메라와 같은 단계(PostLateUpdate)에서, 카메라보다 늦게 구독해 그 뒤에 돈다 —
            // 순서가 뒤바뀌면 카메라가 움직이는 동안 바가 몸에서 한 프레임 밀린다.
            Observable.EveryUpdate(UnityFrameProvider.PostLateUpdate)
                .Subscribe(this, (_, self) => self.Follow())
                .AddTo(this);
        }

        /// <summary>카메라가 자리를 잡은 뒤, 한 프레임분 몸을 따라간다.</summary>
        private void Follow()
        {
            Core.Advance(Time.deltaTime);

            var destination = Destination;
            _anchor = _motion.FollowSpeed <= 0f
                ? destination
                : Vector3.Lerp(_anchor, destination, 1f - Mathf.Exp(-_motion.FollowSpeed * Time.deltaTime));

            Place();
        }

        private void Place()
        {
            var screenPoint = _camera.WorldToScreenPoint(_anchor);

            // 카메라 뒤의 점은 화면 좌표가 뒤집혀 엉뚱한 곳에 찍힌다.
            var visible = screenPoint.z > 0f;
            _graphic.SetActive(visible);
            if (!visible) return;

            var parent = (RectTransform)transform.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out var localPoint);
            RectTransform.anchoredPosition = localPoint + _screenOffset;
        }

        private void OnDestroy()
        {
            Core.Dispose();
        }
    }
}
