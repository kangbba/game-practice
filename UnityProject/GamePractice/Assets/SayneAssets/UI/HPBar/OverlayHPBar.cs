using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 오버레이 캔버스 위에서 캐릭터 머리를 따라다니는 HP 바.
    /// 월드의 머리 지점을 스크린으로 옮긴 뒤 픽셀 오프셋을 더한다 — 거리와 상관없이 크기와 머리 위 간격이 일정하다.
    /// </summary>
    public sealed class OverlayHPBar : HPBar
    {
        /// <summary>카메라 뒤로 넘어간 동안 꺼둘 그림 루트. 본체를 끄면 LateUpdate 가 멈춰 다시 켤 수 없다.</summary>
        [SerializeField] private GameObject _graphic;

        private Camera _camera;
        private Transform _target;

        /// <summary>
        /// 캐릭터 발에서 머리 위까지의 월드 벡터. 그림이 카메라 쪽으로 누워 있어서 y 만이 아니라 z 도 들어 있다 —
        /// 재는 쪽이 그림 꼭대기를 통째로 재서 넘겨주므로 여기선 더하기만 한다.
        /// </summary>
        private Vector3 _worldOffset;

        /// <summary>머리 지점에서 화면상으로 더 띄우는 값. 캔버스 기준 해상도 단위.</summary>
        private Vector2 _screenOffset;

        private float _followSpeed = HPBarStyle.Default.FollowSpeed;

        /// <summary>월드에서 따라가는 지점. 스크린에서 보간하면 카메라가 움직일 때 바가 캐릭터에서 밀린다.</summary>
        private Vector3 _anchor;

        private RectTransform RectTransform => (RectTransform)transform;

        private Vector3 Destination => _target.position + _worldOffset;

        public void Attach(Camera camera, Transform target, Vector3 worldOffset, Vector2 screenOffset,
            ReadOnlyReactiveProperty<float> currentHP, ReadOnlyReactiveProperty<float> maxHP)
        {
            _camera = camera;
            _target = target;
            _worldOffset = worldOffset;
            _screenOffset = screenOffset;
            _followSpeed = HPBarStyle.Default.FollowSpeed;

            _anchor = Destination;
            Place();

            Bind(currentHP, maxHP);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (_target == null) return;

            var destination = Destination;
            _anchor = _followSpeed <= 0f
                ? destination
                : Vector3.Lerp(_anchor, destination, 1f - Mathf.Exp(-_followSpeed * Time.deltaTime));

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
    }
}
