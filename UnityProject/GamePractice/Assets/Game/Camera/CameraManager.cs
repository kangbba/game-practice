using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 카메라를 움직이는 유일한 곳. 대상을 지금 보는 방식(CameraView)대로 쫓는다.
    /// 자리·각도·시야각 모두 같은 원리로 다가간다 — 매 프레임 남은 거리의 일정 비율만큼. 대상이 움직여도,
    /// 보는 방식을 갈아 끼워도 같은 식으로 부드럽게 따라붙는다.
    /// </summary>
    public class CameraManager : ManagerBase
    {
        private const float FollowSpeed = 8f;

        private readonly ReactiveProperty<Quaternion> _billboardRotation = new ReactiveProperty<Quaternion>();

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private float _initialFieldOfView;
        private CameraView _view;
        private Transform _followTarget;

        public Camera Camera { get; private set; }
        public ReadOnlyReactiveProperty<Quaternion> BillboardRotation => _billboardRotation;

        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
        }

        /// <summary>보는 방식을 갈아 끼운다. 카메라는 그 자리로 끊지 않고 쫓아가는 원리 그대로 옮겨 간다.</summary>
        public void SetView(CameraView view)
        {
            _view = view;
        }

        protected override void OnInit()
        {
            Camera = Camera.main;
            _initialPosition = Camera.transform.position;
            _initialRotation = Camera.transform.rotation;
            _initialFieldOfView = Camera.fieldOfView;

            // 시작은 평소 전투 시점에 원점(히어로가 서는 자리)을 보는 자세로 바로 세운다.
            _view = CameraView.Battle;
            Camera.transform.SetPositionAndRotation(_view.Offset, _view.Rotation);
            Camera.fieldOfView = _view.FieldOfView;
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
            // 카메라는 씬 소유라 파괴하지 않는다. 대신 씬에 놓인 자세로 되돌려 둔다.
            if (Camera != null)
            {
                Camera.transform.SetPositionAndRotation(_initialPosition, _initialRotation);
                Camera.fieldOfView = _initialFieldOfView;
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

            var step = 1f - Mathf.Exp(-FollowSpeed * Time.deltaTime);
            var transform = Camera.transform;

            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, _followTarget.position + _view.Offset, step),
                Quaternion.Slerp(transform.rotation, _view.Rotation, step));
            Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView, _view.FieldOfView, step);
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
