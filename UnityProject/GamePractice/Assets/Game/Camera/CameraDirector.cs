using R3;

namespace Sayne
{
    /// <summary>카메라가 누구를 찍을지 정하는 정책. 지금은 "스폰된 히어로를 따른다" 하나뿐이다.</summary>
    public class CameraDirector : ManagerBase
    {
        private readonly CameraManager _cameraManager;
        private readonly HeroManager _heroManager;

        public CameraDirector(CameraManager cameraManager, HeroManager heroManager)
        {
            _cameraManager = cameraManager;
            _heroManager = heroManager;
        }

        protected override void OnInit()
        {
            _heroManager.Spawned
                .Subscribe(this, (character, self) =>
                {
                    if (character is Hero hero)
                    {
                        self._cameraManager.SetFollowTarget(hero.transform);
                    }
                })
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
        }
    }
}
