using R3;
using UnityEngine;

namespace Sayne
{
    public class CameraManager : ManagerBase
    {
        private const float FollowSpeed = 8f;

        private readonly ReactiveProperty<Quaternion> _billboardRotation = new ReactiveProperty<Quaternion>();

        private Vector3 _initialPosition;
        private Vector3 _followOffset;
        private Transform _followTarget;

        public Camera Camera { get; private set; }
        public ReadOnlyReactiveProperty<Quaternion> BillboardRotation => _billboardRotation;

        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
        }

        protected override void OnInit()
        {
            Camera = Camera.main;
            _initialPosition = Camera.transform.position;
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
            if (Camera != null)
            {
                Camera.transform.position = _initialPosition;
            }

            _billboardRotation.Dispose();
            Camera = null;
        }

        private void UpdateFollow()
        {
            if (_followTarget == null)
            {
                return;
            }

            var destination = _followTarget.position + _followOffset;
            Camera.transform.position = Vector3.Lerp(
                Camera.transform.position,
                destination,
                1f - Mathf.Exp(-FollowSpeed * Time.deltaTime));
        }

        private void UpdateBillboardRotation()
        {
            _billboardRotation.Value = CalculateBillboardRotation();
        }

        private Quaternion CalculateBillboardRotation()
        {
            return Camera.transform.rotation;
        }
    }
}
