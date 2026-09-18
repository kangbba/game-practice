using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 화면 UI 전부의 주인. 캔버스를 만들고 그 위에 프리팹을 올린다.
    /// 두 갈래를 맡는다 — 한 벌뿐이라 계속 사는 전투 HUD 와, 캐릭터마다 딸려 태어나고 죽는 HP 바·데미지 숫자.
    /// 둘 다 스크린 캔버스 위에 있다. 머리를 따라가는 건 바 자신이 하고, 여기는 만들고 치우기만 한다.
    /// </summary>
    public class ScreenUIManager : ManagerBase
    {
        private const string RootName = "ScreenUIRoot";
        private const string HPBarRootName = "HPBarRoot";
        private const string WorldHPBarRootName = "WorldHPBarCanvas";
        private const string DamageTextRootName = "DamageTextRoot";
        private const string EventSystemName = "EventSystem";

        /// <summary>전투 HUD(0)보다 뒤에 그려져야 데미지 숫자가 HUD 를 가리지 않는다.</summary>
        private const int DamageTextSortingOrder = -10;

        /// <summary>데미지 숫자보다도 뒤 — 숫자가 바 위로 떠올라야 읽힌다.</summary>
        private const int HPBarSortingOrder = -20;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        // HP 바는 둘로 나뉜다. 영웅과 보스는 머리 위 UI(오버레이), 일반 적은 발밑 월드.

        /// <summary>머리 위 바: 머리 꼭대기에서 바까지 띄우는 간격.</summary>
        private const float HPBarHeadGap = 0.25f;

        /// <summary>머리 위 바: 머리 지점에서 화면상으로 더 띄우는 값. 기준 해상도 단위라 거리와 상관없이 간격이 같다.</summary>
        private static readonly Vector2 HeadHPBarScreenOffset = new Vector2(0f, 22f);

        /// <summary>발밑 바: 발에서 화면 아래쪽으로 내리는 월드 거리.</summary>
        private const float FootHPBarGap = 0.2f;

        /// <summary>발밑 바: 프리팹 1픽셀이 월드 몇 단위인가. 160픽셀 폭이면 0.8 단위다.</summary>
        private const float FootHPBarScale = 0.005f;

        /// <summary>발밑 바는 적 레이어에서 몸보다 위에 그린다 — 무리 속에서도 바가 몸에 묻히지 않는다.</summary>
        private const int FootHPBarSortingOrder = 100;

        /// <summary>머리 꼭대기에서 데미지 숫자가 뜨기 시작하는 곳까지의 간격. HP 바보다 위라야 바에 안 가린다.</summary>
        private const float DamageTextHeadGap = 0.55f;

        private readonly PauseManager _pauseManager;
        private readonly CameraManager _cameraManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly WaveManager _waveManager;
        private readonly QuestManager _questManager;
        private readonly CurrencyManager _currencyManager;
        private readonly GrowthManager _growthManager;
        private readonly EquipmentManager _equipmentManager;
        private readonly UltimateDirector _ultimateDirector;
        private readonly PopupManager _popupManager;
        private readonly IAssets<CharacterProfile> _profiles;
        private readonly IAssets<Hero> _heroAssets;
        private readonly BattlePanel _battlePanelPrefab;
        private readonly OverlayHPBar _hpBarPrefab;
        private readonly WorldHPBar _worldHPBarPrefab;
        private readonly DamageText _damageTextPrefab;

        private Canvas _canvas;
        private Canvas _hpBarCanvas;
        private Canvas _worldHPBarCanvas;
        private Canvas _damageTextCanvas;

        /// <summary>전투 HUD. OnInit 이후 접근 가능.</summary>
        public BattlePanel BattlePanel { get; private set; }

        public ScreenUIManager(PauseManager pauseManager, CameraManager cameraManager,
            HeroManager heroManager, EnemyManager enemyManager, WaveManager waveManager, QuestManager questManager,
            CurrencyManager currencyManager, GrowthManager growthManager, EquipmentManager equipmentManager,
            UltimateDirector ultimateDirector, PopupManager popupManager, IAssets<CharacterProfile> profiles, IAssets<Hero> heroAssets,
            BattlePanel battlePanelPrefab, OverlayHPBar hpBarPrefab, WorldHPBar worldHPBarPrefab, DamageText damageTextPrefab)
        {
            _pauseManager = pauseManager;
            _cameraManager = cameraManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _waveManager = waveManager;
            _questManager = questManager;
            _currencyManager = currencyManager;
            _growthManager = growthManager;
            _equipmentManager = equipmentManager;
            _ultimateDirector = ultimateDirector;
            _popupManager = popupManager;
            _profiles = profiles;
            _heroAssets = heroAssets;
            _battlePanelPrefab = battlePanelPrefab;
            _hpBarPrefab = hpBarPrefab;
            _worldHPBarPrefab = worldHPBarPrefab;
            _damageTextPrefab = damageTextPrefab;
        }

        protected override void OnInit()
        {
            _canvas = CreateCanvas(RootName, 0, true);
            _hpBarCanvas = CreateCanvas(HPBarRootName, HPBarSortingOrder, false);
            _worldHPBarCanvas = CreateWorldCanvas(WorldHPBarRootName, FootHPBarSortingOrder);
            _damageTextCanvas = CreateCanvas(DamageTextRootName, DamageTextSortingOrder, false);

            if (EventSystem.current == null)
            {
                new GameObject(EventSystemName, typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            BattlePanel = Object.Instantiate(_battlePanelPrefab, _canvas.transform);
            BattlePanel.Init(_heroManager, _waveManager, _questManager, _currencyManager, _growthManager,
                _profiles, _popupManager);

            // 전투 HUD 의 메뉴가 여는 창들. 만들고 여닫는 건 팝업 매니저, 무엇을 볼지는 여기서 넣어 준다.
            _popupManager.Create<GrowthWindow>(PopupType.Growth)
                .Init(_growthManager, _currencyManager, _heroManager, _heroAssets);
            _popupManager.Create<EquipmentWindow>(PopupType.Equipment)
                .Init(_equipmentManager, _heroManager, _heroAssets);

            _heroManager.Spawned
                .Merge(_enemyManager.Spawned)
                .Subscribe(this, (character, self) => self.AttachCharacterUI(character))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            Destroy(_canvas);
            Destroy(_hpBarCanvas);
            Destroy(_worldHPBarCanvas);
            Destroy(_damageTextCanvas);

            _canvas = null;
            _hpBarCanvas = null;
            _worldHPBarCanvas = null;
            _damageTextCanvas = null;
            BattlePanel = null;
        }

        /// <summary>캐릭터 하나에 딸리는 UI. 키는 스폰 때 한 번만 묻는다 — 매 프레임 물으면 모션에 따라 바가 출렁인다.</summary>
        private void AttachCharacterUI(Character character)
        {
            var height = character.GetHeight();

            CreateHPBar(character, height);
            BindDamageText(character, height);
        }

        /// <summary>월드에 뜨는 UI 를 모으는 캔버스. 자식은 각자 월드 위치에 서고, 그리는 순서는 정렬 레이어가 정한다.</summary>
        private static Canvas CreateWorldCanvas(string name, int sortingOrder)
        {
            var canvas = new GameObject(name, typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = SortingLayers.Enemy;
            canvas.sortingOrder = sortingOrder;

            return canvas;
        }

        private static Canvas CreateCanvas(string name, int sortingOrder, bool isInteractive)
        {
            var root = isInteractive
                ? new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                : new GameObject(name, typeof(Canvas), typeof(CanvasScaler));

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
                .Select(stats => (float)stats.MaxHP)
                .ToReadOnlyReactiveProperty();

            // 영웅과 보스는 머리 위 UI, 일반 적은 발밑 월드.
            var camera = _cameraManager.Camera;
            HPBar hpBar;

            if (owner is Hero || owner is Enemy { IsBoss: true })
            {
                var overlay = Object.Instantiate(_hpBarPrefab, _hpBarCanvas.transform);
                overlay.Attach(camera, owner.transform, HeadOffset(height + HPBarHeadGap), HeadHPBarScreenOffset,
                    currentHP, maxHP);
                hpBar = overlay;
            }
            else
            {
                var world = Object.Instantiate(_worldHPBarPrefab, _worldHPBarCanvas.transform);
                world.Attach(camera, owner.transform, -camera.transform.up * FootHPBarGap, FootHPBarScale,
                    currentHP, maxHP);
                hpBar = world;
            }

            // 궁극기 연출(복귀까지) 동안은 무대에 오른 캐릭터의 바만 보인다. 무대 밖 몸은 배경에 덮였는데 바만 뜨면 안 된다.
            _ultimateDirector.IsPlaying
                .CombineLatest(owner.IsOnUltimateStage, (playing, onStage) => !playing || onStage)
                .Subscribe(hpBar, (visible, bar) => bar.gameObject.SetActive(visible))
                .RegisterTo(hpBar.destroyCancellationToken);

            owner.Died
                .Subscribe((hpBar, currentHP, maxHP), (_, state) =>
                {
                    state.currentHP.Dispose();
                    state.maxHP.Dispose();
                    Object.Destroy(state.hpBar.gameObject);
                })
                .RegisterTo(LifeToken);
        }

        private void BindDamageText(Character owner, float height)
        {
            var offset = HeadOffset(height + DamageTextHeadGap);

            // Damaged 는 클램프 전 원본 데미지를 흘린다 — 남은 HP 가 10 이어도 100 으로 맞으면 100 이 뜬다.
            owner.Damaged
                .Subscribe((self: this, owner, offset), (damage, state) =>
                    state.self.SpawnDamageText(state.owner, state.offset, damage))
                .RegisterTo(LifeToken);
        }

        private void SpawnDamageText(Character owner, Vector3 offset, int damage)
        {
            var screenPoint = _cameraManager.Camera.WorldToScreenPoint(owner.transform.position + offset);

            var canvasRect = (RectTransform)_damageTextCanvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out var localPoint);

            var damageText = Object.Instantiate(_damageTextPrefab, _damageTextCanvas.transform);
            damageText.RectTransform.anchoredPosition = localPoint;
            damageText.Show(damage.ToString());
        }

        /// <summary>바닥점에서 화면 위로 이만큼. 캐릭터는 제 키만 알려주고, 어느 쪽이 위인지는 여기서 정한다.</summary>
        private Vector3 HeadOffset(float height)
        {
            return _cameraManager.Camera.transform.up * height;
        }
    }
}
