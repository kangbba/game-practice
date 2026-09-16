using R3;
using UnityEngine;

namespace Sayne
{
    public class CameraManager : ManagerBase
    {
        private const float FollowSpeed = 8f;

        private readonly ReactiveProperty<Quaternion> _billboardRotation = new ReactiveProperty<Quaternion>();

        private Camera _camera;
        private Vector3 _initialPosition;
        private Vector3 _followOffset;
        private Transform _followTarget;

        public Camera Camera => _camera;
        public ReadOnlyReactiveProperty<Quaternion> BillboardRotation => _billboardRotation;

        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
        }

        protected override void OnInit()
        {
            _camera = Camera.main;
            _initialPosition = _camera.transform.position;
            _followOffset = _initialPosition;
            _billboardRotation.Value = CalculateBillboardRotation();

            _billboardRotation
                .Subscribe(rotation =>
                {
                    foreach (var billboard in Billboard.Actives)
                    {
                        billboard.Apply(rotation);
                    }
                })
                .RegisterTo(LifeToken);

            Billboard.Registered
                .Subscribe(this, (billboard, self) => billboard.Apply(self._billboardRotation.CurrentValue))
                .RegisterTo(LifeToken);

            Observable.EveryUpdate(UnityFrameProvider.PostLateUpdate)
                .Subscribe(this, (_, self) =>
                {
                    self.UpdateFollow();
                    self.UpdateBillboardRotation();
                })
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            // 카메라는 씬 소유라 파괴하지 않는다. 대신 원위치로 되돌려서, 재생성 시 오프셋이 다시 옳게 잡히게 한다.
            if (_camera != null)
            {
                _camera.transform.position = _initialPosition;
            }

            _billboardRotation.Dispose();
            _camera = null;
        }

        private void UpdateFollow()
        {
            if (_followTarget == null)
            {
                return;
            }

            var destination = _followTarget.position + _followOffset;
            _camera.transform.position = Vector3.Lerp(
                _camera.transform.position,
                destination,
                1f - Mathf.Exp(-FollowSpeed * Time.deltaTime));
        }

        private void UpdateBillboardRotation()
        {
            _billboardRotation.Value = CalculateBillboardRotation();
        }

        private Quaternion CalculateBillboardRotation()
        {
            return _camera.transform.rotation;
        }
    }
}
