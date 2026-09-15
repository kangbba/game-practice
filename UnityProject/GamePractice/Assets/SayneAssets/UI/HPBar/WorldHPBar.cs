using R3;
using UnityEngine;

namespace Sayne
{
    public sealed class WorldHPBar : HPBar
    {
        private Transform _target;
        private Vector3 _offset;
        private float _followSpeed = HPBarStyle.Default.FollowSpeed;

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

            Vector3 destination = _target.position + _offset;
            transform.position = _followSpeed <= 0f
                ? destination
                : Vector3.Lerp(transform.position, destination, 1f - Mathf.Exp(-_followSpeed * Time.deltaTime));
        }

        private void Snap()
        {
            transform.position = _target.position + _offset;
        }
    }
}
