using R3;
using UnityEngine;

namespace Sayne
{
    public class CameraManager : ManagerBase
    {
        private const float FollowSpeed = 8f;

        private readonly HeroManager _heroManager;
        private readonly ReactiveProperty<Quaternion> _billboardRotation = new ReactiveProperty<Quaternion>();

        private Camera _camera;
        private Vector3 _followOffset;

        public Camera Camera => _camera;
        public ReadOnlyReactiveProperty<Quaternion> BillboardRotation => _billboardRotation;

        public CameraManager(HeroManager heroManager)
        {
            _heroManager = heroManager;
        }

        protected override void OnInit()
        {
            _camera = Camera.main;
            _followOffset = _camera.transform.position;
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
        }

        protected override void OnRelease()
        {
            _billboardRotation.Dispose();
            _camera = null;
        }

        public void UpdateFollow()
        {
            var heroes = _heroManager.CurrentHeroes;
            if (heroes.Count == 0)
            {
                return;
            }

            var destination = heroes[0].transform.position + _followOffset;
            _camera.transform.position = Vector3.Lerp(
                _camera.transform.position,
                destination,
                1f - Mathf.Exp(-FollowSpeed * Time.deltaTime));
        }

        public void UpdateBillboardRotation()
        {
            _billboardRotation.Value = CalculateBillboardRotation();
        }

        private Quaternion CalculateBillboardRotation()
        {
            return _camera.transform.rotation;
        }
    }
}
