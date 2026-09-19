using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Sayne
{
    /// <summary>캐릭터 UI 매니저가 찍어내는 프리팹. 쓰임새마다 프리팹이 따로다 — 모양은 프리팹이 정하고 여기선 골라 쓰기만 한다.</summary>
    public interface ICharacterUIAssets
    {
        OverlayHPBar HeroOverlayHPBarPrefab { get; }
        OverlayHPBar BossOverlayHPBarPrefab { get; }
        WorldHPBar EnemyWorldHPBarPrefab { get; }
        DamageText DamageTextPrefab { get; }
    }

    /// <summary>
    /// 캐릭터마다 딸려 태어나고 죽는 UI 의 주인 — HP 바와 데미지 숫자.
    /// 히어로·적이 스폰되면 붙이고, 죽거나 물러나면 치운다. 머리를 따라가는 건 바 자신이 하고, 여기는 만들고 치우기만 한다.
    /// </summary>
    public class CharacterUIManager : ManagerBase
    {
        private const string HPBarRootName = "HPBarRoot";
        private const string WorldHPBarRootName = "WorldHPBarCanvas";
        private const string UltimateWorldHPBarRootName = "UltimateWorldHPBarCanvas";
        private const string DamageTextRootName = "DamageTextRoot";

        /// <summary>전투 HUD(0)보다 뒤에 그려져야 데미지 숫자가 HUD 를 가리지 않는다.</summary>
        private const int DamageTextSortingOrder = -10;

        /// <summary>데미지 숫자보다도 뒤 — 숫자가 바 위로 떠올라야 읽힌다.</summary>
        private const int HPBarSortingOrder = -20;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        // HP 바는 둘로 나뉜다. 영웅과 보스는 머리 위 UI(오버레이), 일반 적은 발밑 월드.

        /// <summary>발밑 바: 발에서 화면 아래쪽으로 내리는 월드 거리.</summary>
        private const float FootHPBarGap = 0.2f;

        /// <summary>발밑 바: 프리팹 1픽셀이 월드 몇 단위인가. 160픽셀 폭이면 2.24 단위다.</summary>
        private const float FootHPBarScale = 0.014f;

        /// <summary>발밑 바는 적 레이어에서 몸보다 위에 그린다 — 무리 속에서도 바가 몸에 묻히지 않는다.</summary>
        private const int FootHPBarSortingOrder = 100;

        /// <summary>머리 꼭대기에서 데미지 숫자가 뜨기 시작하는 곳까지의 간격. HP 바보다 위라야 바에 안 가린다.</summary>
        private const float DamageTextHeadGap = 0.55f;

        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly UltimateDirector _ultimateDirector;
        private readonly TutorialManager _tutorialManager;
        private readonly IAssets<CharacterProfile> _profiles;
        private readonly ICharacterUIAssets _assets;

        private Canvas _hpBarCanvas;
        private Canvas _worldHPBarCanvas;

        /// <summary>
        /// 궁극기 무대에 오른 적의 발밑 바가 옮겨 가는 캔버스. 평소 캔버스는 적 레이어라 궁극기 백그라운드에 덮인다 —
        /// 몸이 무대 레이어(UltimateEnemy)로 올라갈 때 바도 같이 올라와야 보인다.
        /// </summary>
        private Canvas _ultimateWorldHPBarCanvas;

        private Canvas _damageTextCanvas;

        /// <summary>UI 라 컨텍스트를 받는다. 프리팹만은 전투 에셋에서 쓰는 것만 받는다.</summary>
        public CharacterUIManager(GameContext game, BattleContext battle, ICharacterUIAssets assets)
        {
            _heroManager = battle.HeroManager;
            _enemyManager = battle.EnemyManager;
            _ultimateDirector = battle.UltimateDirector;
            _tutorialManager = game.TutorialManager;
            _profiles = game.Assets.Profiles;
            _assets = assets;
        }

        protected override void OnInit()
        {
            _hpBarCanvas = CreateCanvas(HPBarRootName, HPBarSortingOrder);
            _worldHPBarCanvas = CreateWorldCanvas(WorldHPBarRootName, SortingLayers.Enemy, FootHPBarSortingOrder);
            _ultimateWorldHPBarCanvas = CreateWorldCanvas(UltimateWorldHPBarRootName, SortingLayers.UltimateEnemy,
                FootHPBarSortingOrder);
            _damageTextCanvas = CreateCanvas(DamageTextRootName, DamageTextSortingOrder);

            _heroManager.Spawned
                .Merge(_enemyManager.Spawned)
                .Subscribe(this, (character, self) => self.AttachCharacterUI(character))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            Destroy(_hpBarCanvas);
            Destroy(_worldHPBarCanvas);
            Destroy(_ultimateWorldHPBarCanvas);
            Destroy(_damageTextCanvas);

            _hpBarCanvas = null;
            _worldHPBarCanvas = null;
            _ultimateWorldHPBarCanvas = null;
            _damageTextCanvas = null;
        }

        /// <summary>캐릭터 하나에 딸리는 UI. 키는 스폰 때 한 번만 묻는다 — 매 프레임 물으면 모션에 따라 바가 출렁인다.</summary>
        private void AttachCharacterUI(Character character)
        {
            var height = character.GetHeight();

            CreateHPBar(character, height);
            BindDamageText(character, height);
        }

        /// <summary>월드에 뜨는 UI 를 모으는 캔버스. 자식은 각자 월드 위치에 서고, 그리는 순서는 정렬 레이어가 정한다.</summary>
        private static Canvas CreateWorldCanvas(string name, string sortingLayer, int sortingOrder)
        {
            var canvas = new GameObject(name, typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = sortingLayer;
            canvas.sortingOrder = sortingOrder;

            return canvas;
        }

        /// <summary>누를 것이 없는 화면 캔버스라 레이캐스터를 달지 않는다.</summary>
        private static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var root = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            return canvas;
        }

        private static void Destroy(Canvas canvas)
        {
            if (canvas != null)
            {
                Object.Destroy(canvas.gameObject);
            }
        }

        private void CreateHPBar(Character owner, float height)
        {
            var currentHP = owner.CurrentHP
                .Select(hp => (float)hp)
                .ToReadOnlyReactiveProperty();

            // 최대치도 스탯에서 흘려보낸다 — 성장으로 MaxHP 가 오르면 바와 수치가 그 자리에서 맞춰진다.
            var maxHP = owner.CurrentStats
                .Select(stats => stats.Get(StatType.MaxHP))
                .ToReadOnlyReactiveProperty();

            // 영웅과 보스는 머리 위 UI 에 초상화를 넘기고, 일반 적은 발밑 월드에 초상화 없이.
            var camera = GameCamera.Camera;

            // 두 바는 붙는 곳도 따라가는 방식도 달라 공통 조상이 없다. 여기서 쓰는 건 몸통과 수명뿐이다.
            MonoBehaviour hpBar;

            if (owner is Hero || owner is Enemy { IsBoss: true })
            {
                var prefab = owner is Hero ? _assets.HeroOverlayHPBarPrefab : _assets.BossOverlayHPBarPrefab;
                var overlay = Object.Instantiate(prefab, _hpBarCanvas.transform);
                // 머리 위 자리는 말풍선과 같다(HeadAnchor). 말풍선이 떠 있는 동안은 아래에서 비켜 준다.
                overlay.Init(camera, owner.transform, HeadAnchor.WorldOffset(camera, height), HeadAnchor.ScreenOffset,
                    _profiles.Get(owner.ID).Portrait, currentHP, maxHP);
                hpBar = overlay;
            }
            else
            {
                var world = Object.Instantiate(_assets.EnemyWorldHPBarPrefab, _worldHPBarCanvas.transform);
                world.Init(camera, owner.transform, -camera.transform.up * FootHPBarGap, FootHPBarScale,
                    null, currentHP, maxHP);
                hpBar = world;

                // 몸이 궁극기 무대에 오르내릴 때 바도 무대 레이어 캔버스로 같이 오르내린다. 자리는 바가 매 프레임 스스로 잡는다.
                owner.IsOnUltimateStage
                    .Subscribe((self: this, world), (onStage, state) => state.world.transform.SetParent(
                        (onStage ? state.self._ultimateWorldHPBarCanvas : state.self._worldHPBarCanvas).transform, true))
                    .RegisterTo(world.destroyCancellationToken);
            }

            // 궁극기 연출(복귀까지) 동안은 무대에 오른 캐릭터의 바만 보인다. 무대 밖 몸은 배경에 덮였는데 바만 뜨면 안 된다.
            // 머리 위에 말풍선이 떠 있는 동안도 숨는다 — 둘은 같은 자리를 쓰므로 번갈아 보인다.
            _ultimateDirector.IsPlaying
                .CombineLatest(owner.IsOnUltimateStage, _tutorialManager.IsSpeaking(owner),
                    (playing, onStage, speaking) => (!playing || onStage) && !speaking)
                .Subscribe(hpBar, (visible, bar) => bar.gameObject.SetActive(visible))
                .RegisterTo(hpBar.destroyCancellationToken);

            // 바의 수명은 만든 쪽이 쥔다. 몸이 죽는 순간 치우고, 죽지 않고 물러나도(영웅 교체) 그 자리에서 같이 치운다.
            // 몸이 치워지는 것(판 비우기)도 받는다 — 어느 쪽이 먼저 와도 한 번만 돈다.
            var reap = Disposable.Create((hpBar, currentHP, maxHP),
                state => DestroyHPBar(state.hpBar, state.currentHP, state.maxHP));

            owner.Died
                .Merge(_heroManager.Destroyed.Where(owner, (character, self) => character == self))
                .Subscribe(reap, (_, disposable) => disposable.Dispose())
                .RegisterTo(owner.destroyCancellationToken);

            reap.RegisterTo(owner.destroyCancellationToken);
        }

        /// <summary>바를 치운다. 만든 게 여기라 없애는 것도 여기서만 한다 — 바는 스스로 사라지지 않는다.</summary>
        private static void DestroyHPBar(MonoBehaviour hpBar, IDisposable currentHP, IDisposable maxHP)
        {
            currentHP.Dispose();
            maxHP.Dispose();

            // 캔버스가 먼저 내려갔으면 몸은 이미 없다.
            if (hpBar != null)
            {
                Object.Destroy(hpBar.gameObject);
            }
        }

        private void BindDamageText(Character owner, float height)
        {
            var offset = HeadOffset(height + DamageTextHeadGap);

            // Damaged 는 클램프 전 원본 데미지를 흘린다 — 남은 HP 가 10 이어도 100 으로 맞으면 100 이 뜬다.
            owner.Damaged
                .Subscribe((self: this, owner, offset), (damage, state) =>
                    state.self.SpawnDamageText(state.owner, state.offset, damage))
                .RegisterTo(owner.destroyCancellationToken);
        }

        private void SpawnDamageText(Character owner, Vector3 offset, int damage)
        {
            var screenPoint = GameCamera.Camera.WorldToScreenPoint(owner.transform.position + offset);

            var canvasRect = (RectTransform)_damageTextCanvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out var localPoint);

            var damageText = Object.Instantiate(_assets.DamageTextPrefab, _damageTextCanvas.transform);
            damageText.RectTransform.anchoredPosition = localPoint;
            damageText.Show(damage.ToString());
        }

        /// <summary>바닥점에서 화면 위로 이만큼. 캐릭터는 제 키만 알려주고, 어느 쪽이 위인지는 여기서 정한다.</summary>
        private Vector3 HeadOffset(float height)
        {
            return GameCamera.Camera.transform.up * height;
        }
    }
}
