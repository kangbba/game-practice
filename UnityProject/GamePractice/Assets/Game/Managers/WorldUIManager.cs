using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    public class WorldUIManager : ManagerBase
    {
        private const string RootName = "WorldUIRoot";
        private const string DamageTextRootName = "DamageTextRoot";

        /// <summary>전투 HUD(sortingOrder 0)보다 뒤에 그려져야 데미지 숫자가 HUD 를 가리지 않는다.</summary>
        private const int DamageTextSortingOrder = -10;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        /// <summary>빌보드(=카메라) 기준 오프셋이라 y 가 화면상 '위'다. 머리 높이에 맞춰 눈으로 조정할 값.</summary>
        private static readonly Vector3 HPBarOffset = new Vector3(0f, 2.2f, 0f);

        /// <summary>가슴께에서 떠오르기 시작해야 HP 바와 겹치지 않는다. 스크린 변환 전의 월드 오프셋.</summary>
        private static readonly Vector3 DamageTextWorldOffset = new Vector3(0f, 1.4f, 0f);

        private readonly CameraManager _cameraManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly WorldHPBar _hpBarPrefab;
        private readonly DamageText _damageTextPrefab;

        private Canvas _damageTextCanvas;

        public Transform CurrentRoot { get; private set; }

        public WorldUIManager(CameraManager cameraManager, HeroManager heroManager, EnemyManager enemyManager,
            WorldHPBar hpBarPrefab, DamageText damageTextPrefab)
        {
            _cameraManager = cameraManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _hpBarPrefab = hpBarPrefab;
            _damageTextPrefab = damageTextPrefab;
        }

        protected override void OnInit()
        {
            CurrentRoot = new GameObject(RootName).transform;
            CreateDamageTextCanvas();

            _heroManager.Spawned
                .Merge(_enemyManager.Spawned)
                .Subscribe(this, (character, self) =>
                {
                    self.CreateHPBar(character);
                    self.BindDamageText(character);
                })
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            if (CurrentRoot != null)
            {
                Object.Destroy(CurrentRoot.gameObject);
            }

            if (_damageTextCanvas != null)
            {
                Object.Destroy(_damageTextCanvas.gameObject);
            }

            CurrentRoot = null;
            _damageTextCanvas = null;
        }

        /// <summary>데미지 숫자 전용 오버레이 캔버스. HUD 캔버스와 같은 기준 해상도로 맞춘다.</summary>
        private void CreateDamageTextCanvas()
        {
            var root = new GameObject(DamageTextRootName, typeof(Canvas), typeof(CanvasScaler));

            _damageTextCanvas = root.GetComponent<Canvas>();
            _damageTextCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _damageTextCanvas.sortingOrder = DamageTextSortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
        }

        private void BindDamageText(Character owner)
        {
            // Damaged 는 클램프 전 원본 데미지를 흘린다 — 남은 HP 가 10 이어도 100 으로 맞으면 100 이 뜬다.
            owner.Damaged
                .Subscribe((self: this, owner), (damage, state) =>
                    state.self.SpawnDamageText(state.owner, damage))
                .RegisterTo(LifeToken);
        }

        private void SpawnDamageText(Character owner, int damage)
        {
            var screenPoint = _cameraManager.Camera.WorldToScreenPoint(
                owner.transform.position + DamageTextWorldOffset);

            var canvasRect = (RectTransform)_damageTextCanvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out var localPoint);

            var damageText = Object.Instantiate(_damageTextPrefab, _damageTextCanvas.transform);
            damageText.RectTransform.anchoredPosition = localPoint;
            damageText.Show(damage.ToString());
        }

        private void CreateHPBar(Character owner)
        {
            var hpBar = Object.Instantiate(_hpBarPrefab, CurrentRoot);
            var currentHP = owner.CurrentHP
                .Select(hp => (float)hp)
                .ToReadOnlyReactiveProperty();

            // 최대치도 스탯에서 흘려보낸다 — 성장으로 MaxHP 가 오르면 바와 수치가 그 자리에서 맞춰진다.
            var maxHP = owner.CurrentStats
                .Select(stats => (float)stats.MaxHP)
                .ToReadOnlyReactiveProperty();

            hpBar.Attach(owner.transform, HPBarOffset, currentHP, maxHP);

            owner.Died
                .Subscribe((hpBar, currentHP, maxHP), (_, state) =>
                {
                    state.currentHP.Dispose();
                    state.maxHP.Dispose();
                    Object.Destroy(state.hpBar.gameObject);
                })
                .RegisterTo(LifeToken);
        }
    }
}
