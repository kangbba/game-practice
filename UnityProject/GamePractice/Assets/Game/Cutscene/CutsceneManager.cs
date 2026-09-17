using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 궁극기를 쓰는 순간 컷인을 튼다. 컷인이 도는 동안 게임은 멈춘다 — 그 연결은 "IsPlaying 인 동안 중단" 한 줄이다.
    /// 전투 쪽은 이 매니저를 모른다. 궁극기가 나갔다는 신호를 듣고 끼어들 뿐이라, 시전 모션은 첫 프레임에서 얼었다가 컷인이 끝나면 이어진다.
    /// </summary>
    public class CutsceneManager : ManagerBase
    {
        private const string RootName = "CutsceneRoot";

        /// <summary>전투 HUD(0)와 창들 위. 컷인 동안은 아래 UI 가 눌리면 안 되므로 레이캐스트도 여기서 막는다.</summary>
        private const int SortingOrder = 100;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private readonly PauseManager _pauseManager;
        private readonly HeroManager _heroManager;
        private readonly IAssets<CharacterProfile> _profiles;
        private readonly UltimateCutscenePanel _panelPrefab;

        private readonly ReactiveProperty<bool> _isPlaying = new ReactiveProperty<bool>(false);

        private Canvas _canvas;

        public ReadOnlyReactiveProperty<bool> IsPlaying => _isPlaying;

        public CutsceneManager(PauseManager pauseManager, HeroManager heroManager,
            IAssets<CharacterProfile> profiles, UltimateCutscenePanel panelPrefab)
        {
            _pauseManager = pauseManager;
            _heroManager = heroManager;
            _profiles = profiles;
            _panelPrefab = panelPrefab;
        }

        protected override void OnInit()
        {
            CreateCanvas();

            _pauseManager.PauseWhile(_isPlaying);

            _heroManager.Spawned
                .Subscribe(this, (character, self) =>
                {
                    if (character is Hero hero)
                    {
                        self.BindHero(hero);
                    }
                })
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            if (_canvas != null)
            {
                Object.Destroy(_canvas.gameObject);
            }

            _canvas = null;
            _isPlaying.Dispose();
        }

        private void CreateCanvas()
        {
            var root = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = SortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
        }

        private void BindHero(Hero hero)
        {
            hero.Combat.Attacked
                .Where(hero, (attack, owner) => attack == owner.Combat.Ultimate)
                .Subscribe((self: this, hero), (_, state) => state.self.PlayUltimateAsync(state.hero).Forget())
                .RegisterTo(hero.destroyCancellationToken);
        }

        private async UniTaskVoid PlayUltimateAsync(Hero hero)
        {
            var profile = _profiles.Get(hero.ID);

            _isPlaying.Value = true;

            var panel = Object.Instantiate(_panelPrefab, _canvas.transform);
            await panel.PlayAsync(profile.Portrait, profile.DisplayName, hero.Combat.Ultimate.Name,
                profile.ThemeColor, LifeToken);
            Object.Destroy(panel.gameObject);

            _isPlaying.Value = false;
        }
    }
}
