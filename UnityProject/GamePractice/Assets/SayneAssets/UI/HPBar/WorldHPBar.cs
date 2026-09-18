using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 월드에 떠서 캐릭터 발밑을 따라다니는 HP 바. 월드 캔버스 하나에 모여 있고, 늘 카메라를 정면으로 본다.
    /// 월드에 있으니 멀면 작아지고 캐릭터와 함께 화면에서 움직인다 — 스크린에 붙는 OverlayHPBar 와 다른 점이다.
    /// </summary>
    public sealed class WorldHPBar : HPBar
    {
        private Transform _camera;
        private Transform _target;

        /// <summary>캐릭터 발에서 바까지의 월드 벡터.</summary>
        private Vector3 _worldOffset;

        /// <param name="scale">캔버스 1 단위(프리팹 픽셀)를 월드 몇 단위로 볼지. 바의 실제 크기가 이걸로 정해진다.</param>
        public void Attach(Camera camera, Transform target, Vector3 worldOffset, float scale,
            ReadOnlyReactiveProperty<float> currentHP, ReadOnlyReactiveProperty<float> maxHP)
        {
            _camera = camera.transform;
            _target = target;
            _worldOffset = worldOffset;
            transform.localScale = Vector3.one * scale;

            Place();

            Bind(currentHP, maxHP);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            Place();
        }

        private void Place()
        {
            transform.SetPositionAndRotation(_target.position + _worldOffset, _camera.rotation);
        }
    }
}
