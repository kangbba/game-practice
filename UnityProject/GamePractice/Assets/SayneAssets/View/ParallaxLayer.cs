using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 카메라 이동을 일부만 따라가서 멀리 있는 것처럼 보이게 하는 배경 레이어.
    /// 1 이면 카메라에 완전히 붙어 화면에서 안 움직이고(= 무한히 멀다), 0 이면 월드에 박힌다.
    /// 세로는 보통 0 이다 — 지평선이 지면 끝에서 떨어지면 산이 공중에 뜬 것처럼 보인다.
    /// </summary>
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Vector2 _followRatio = new Vector2(0.85f, 0f);

        private Camera _camera;
        private Vector3 _anchor;
        private Vector3 _cameraAnchor;

        private void OnEnable()
        {
            _camera = Camera.main;
            _anchor = transform.position;
            _cameraAnchor = _camera.transform.position;
        }

        private void LateUpdate()
        {
            var delta = _camera.transform.position - _cameraAnchor;
            transform.position = _anchor + new Vector3(delta.x * _followRatio.x, delta.y * _followRatio.y, 0f);
        }
    }
}
