using System.Collections.Generic;
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

        /// <summary>인물들을 담을 때 화면 가장자리에 남기는 여유. 1.2 면 화면의 1/1.2 안에 들어온다.</summary>
        private const float FramePadding = 1.2f;

        private readonly ReactiveProperty<Quaternion> _billboardRotation = new ReactiveProperty<Quaternion>();

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private float _initialFieldOfView;
        private CameraView _view;
        private Transform _followTarget;

        /// <summary>흔들림을 뺀 카메라 자리. 쫓아가기는 이 자리로 하고, 화면에는 여기에 흔들림을 얹어 놓는다 — 흔들림이 쫓아가기에 섞여 번지지 않게.</summary>
        private Vector3 _steadyPosition;

        private float _shakeStrength;
        private float _shakeSeconds;
        private float _shakeRemain;

        /// <summary>한꺼번에 화면에 담을 인물들. 있으면 따라갈 대상 대신 이들을 담는 자리로 간다.</summary>
        private IReadOnlyList<Character> _framedSubjects;

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
            _framedSubjects = null;
        }

        /// <summary>
        /// 화면을 흔든다. 세기(월드 단위)에서 시작해 seconds 동안 0 으로 잦아든다.
        /// 흔드는 중에 또 부르면 남은 흔들림과 새 흔들림 중 센 쪽을 잇는다 — 연타가 쌓여 폭주하지 않는다.
        /// </summary>
        public void Shake(float strength, float seconds)
        {
            var remaining = _shakeRemain > 0f ? _shakeStrength * _shakeRemain / _shakeSeconds : 0f;
            if (strength < remaining)
            {
                return;
            }

            _shakeStrength = strength;
            _shakeSeconds = seconds;
            _shakeRemain = seconds;
        }

        /// <summary>
        /// 보는 방식을 갈아 끼우되, 한 사람을 쫓는 대신 이 인물들이 전부 화면에 들어오는 자리로 간다.
        /// 각도·시야각은 view 그대로 쓰고 중심과 거리만 매 프레임 계산한다. 거리는 view 의 거리보다 가까워지지 않는다.
        /// </summary>
        public void Frame(CameraView view, IReadOnlyList<Character> subjects)
        {
            _view = view;
            _framedSubjects = subjects;
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
            _steadyPosition = _view.Offset;
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
            if (_followTarget == null && _framedSubjects == null)
            {
                return;
            }

            var step = 1f - Mathf.Exp(-FollowSpeed * Time.deltaTime);
            var transform = Camera.transform;
            var goal = _framedSubjects != null ? GetFramePosition() : _followTarget.position + _view.Offset;

            _steadyPosition = Vector3.Lerp(_steadyPosition, goal, step);

            transform.SetPositionAndRotation(
                _steadyPosition + GetShakeOffset(),
                Quaternion.Slerp(transform.rotation, _view.Rotation, step));
            Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView, _view.FieldOfView, step);
        }

        /// <summary>이번 프레임의 흔들림. 남은 시간에 비례해 약해지고, 다 잦아들면 0 이다.</summary>
        private Vector3 GetShakeOffset()
        {
            if (_shakeRemain <= 0f)
            {
                return Vector3.zero;
            }

            _shakeRemain -= Time.deltaTime;
            var strength = _shakeStrength * Mathf.Max(_shakeRemain, 0f) / _shakeSeconds;

            // 화면 안에서만 흔든다 — 앞뒤로 흔들면 줌처럼 보인다.
            var shake = Random.insideUnitCircle * strength;
            return Camera.transform.right * shake.x + Camera.transform.up * shake.y;
        }

        /// <summary>
        /// 인물 전부의 발밑과 머리 꼭대기가 화면 안에 드는 카메라 자리. 카메라 방향으로 돌려 본 좌표에서
        /// 상하좌우 끝의 한가운데를 중심으로 삼고, 모든 점이 시야각 안에 들 만큼 뒤로 물러난다.
        /// </summary>
        private Vector3 GetFramePosition()
        {
            var rotation = _view.Rotation;
            var toCamera = Quaternion.Inverse(rotation);

            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var subject in _framedSubjects)
            {
                foreach (var point in new[] { subject.transform.position, subject.TopPoint })
                {
                    var local = toCamera * point;
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }
            }

            var center = rotation * ((min + max) * 0.5f);

            var halfHeight = Mathf.Tan(_view.FieldOfView * 0.5f * Mathf.Deg2Rad);
            var halfWidth = halfHeight * Camera.aspect;
            var distance = _view.Offset.magnitude;

            foreach (var subject in _framedSubjects)
            {
                foreach (var point in new[] { subject.transform.position, subject.TopPoint })
                {
                    var local = toCamera * (point - center);
                    distance = Mathf.Max(distance,
                        Mathf.Abs(local.y) * FramePadding / halfHeight - local.z,
                        Mathf.Abs(local.x) * FramePadding / halfWidth - local.z);
                }
            }

            return center - rotation * Vector3.forward * distance;
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
