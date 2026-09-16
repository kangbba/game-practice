using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 캐릭터 머리 위에 붙는 HP 바. 회전은 Billboard 가 카메라 회전으로 맞춰준다.
    /// 오프셋도 그 회전을 따라간다 — 월드 up 으로 띄우면 기울어진 카메라에서는
    /// 화면상 위가 아니라 카메라 쪽으로 딸려와서 머리 위를 벗어난다.
    /// </summary>
    public sealed class WorldHPBar : HPBar
    {
        private Transform _target;

        /// <summary>빌보드(=카메라) 기준 오프셋. y 가 화면상 '위'다.</summary>
        private Vector3 _offset;

        private float _followSpeed = HPBarStyle.Default.FollowSpeed;

        private Vector3 Destination => _target.position + transform.rotation * _offset;

        public void Attach(Transform target, Vector3 offset,
            ReadOnlyReactiveProperty<float> currentHP, float maxHP)
        {
            _target = target;
            _offset = offset;
            _followSpeed = HPBarStyle.Default.FollowSpeed;

            if (_target != null) Snap();

            Bind(currentHP, maxHP);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (_target == null) return;

            var destination = Destination;
            transform.position = _followSpeed <= 0f
                ? destination
                : Vector3.Lerp(transform.position, destination, 1f - Mathf.Exp(-_followSpeed * Time.deltaTime));
        }

        private void Snap()
        {
            transform.position = Destination;
        }
    }
}
