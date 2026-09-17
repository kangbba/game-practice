using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    public class WorldUIManager : ManagerBase
    {
        private const string HPBarRootName = "HPBarRoot";
        private const string DamageTextRootName = "DamageTextRoot";

        /// <summary>전투 HUD(sortingOrder 0)보다 뒤에 그려져야 데미지 숫자가 HUD 를 가리지 않는다.</summary>
        private const int DamageTextSortingOrder = -10;

        /// <summary>데미지 숫자보다도 뒤 — 숫자가 바 위로 떠올라야 읽힌다.</summary>
        private const int HPBarSortingOrder = -20;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        /// <summary>머리 그림의 꼭대기를 재는 뼈 이름. 영웅·적 모두 이 이름의 뼈를 가진다.</summary>
        private const string HeadBoneName = "Head";

        /// <summary>머리 꼭대기에서 바까지 띄우는 월드 간격.</summary>
        private const float HPBarHeadGap = 0.25f;

        /// <summary>머리 지점에서 화면상으로 더 띄우는 값. 기준 해상도 단위라 거리와 상관없이 간격이 같다.</summary>
        private static readonly Vector2 HPBarScreenOffset = new Vector2(0f, 0f);

        /// <summary>머리 꼭대기에서 데미지 숫자가 뜨기 시작하는 곳까지의 월드 간격. HP 바보다 위라야 바에 안 가린다.</summary>
        private const float DamageTextHeadGap = 0.55f;

        private readonly CameraManager _cameraManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly OverlayHPBar _hpBarPrefab;
        private readonly DamageText _damageTextPrefab;

        private Canvas _hpBarCanvas;
        private Canvas _damageTextCanvas;

        public WorldUIManager(CameraManager cameraManager, HeroManager heroManager, EnemyManager enemyManager,
            OverlayHPBar hpBarPrefab, DamageText damageTextPrefab)
        {
            _cameraManager = cameraManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _hpBarPrefab = hpBarPrefab;
            _damageTextPrefab = damageTextPrefab;
        }

        protected override void OnInit()
        {
            _hpBarCanvas = CreateOverlayCanvas(HPBarRootName, HPBarSortingOrder);
            _damageTextCanvas = CreateOverlayCanvas(DamageTextRootName, DamageTextSortingOrder);

            _heroManager.Spawned
                .Merge(_enemyManager.Spawned)
                .Subscribe(this, (character, self) =>
                {
                    // 머리 꼭대기는 스폰 때 한 번만 재서 HP 바와 데미지 숫자가 같이 쓴다.
                    var headTop = self.HeadTop(character);
                    self.CreateHPBar(character, headTop);
                    self.BindDamageText(character, headTop);
                })
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            if (_hpBarCanvas != null)
            {
                Object.Destroy(_hpBarCanvas.gameObject);
            }

            if (_damageTextCanvas != null)
            {
                Object.Destroy(_damageTextCanvas.gameObject);
            }

            _hpBarCanvas = null;
            _damageTextCanvas = null;
        }

        /// <summary>월드를 따라다니는 UI 전용 오버레이 캔버스. HUD 캔버스와 같은 기준 해상도로 맞춘다.</summary>
        private static Canvas CreateOverlayCanvas(string name, int sortingOrder)
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

        private void BindDamageText(Character owner, Vector3 headTop)
        {
            var offset = headTop + _cameraManager.Camera.transform.up * DamageTextHeadGap;

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

        /// <summary>
        /// 발에서 머리 그림 꼭대기까지의 월드 벡터. 캐릭터마다 키와 배율이 달라서 고정값을 못 쓴다.
        /// 그림이 카메라 쪽으로 누워 있어 꼭대기는 y 로만이 아니라 z 로도 물러나 있다 — 그래서 높이가 아니라 점을 잰다.
        /// 스폰 때 한 번만 잰다 — 매 프레임 재면 모션에 따라 바가 출렁인다.
        /// </summary>
        private Vector3 HeadTop(Character owner)
        {
            var up = _cameraManager.Camera.transform.up;
            var top = Vector3.zero;

            foreach (var bone in owner.GetComponentsInChildren<Transform>())
            {
                if (bone.name != HeadBoneName) continue;

                foreach (var sprite in bone.GetComponentsInChildren<SpriteRenderer>())
                {
                    var bounds = sprite.sprite.bounds;
                    var corner = sprite.transform.TransformPoint(new Vector3(bounds.center.x, bounds.max.y, 0f));
                    var offset = corner - owner.transform.position;
                    if (Vector3.Dot(up, offset) > Vector3.Dot(up, top)) top = offset;
                }
            }

            return top;
        }

        private void CreateHPBar(Character owner, Vector3 headTop)
        {
            var hpBar = Object.Instantiate(_hpBarPrefab, _hpBarCanvas.transform);
            var currentHP = owner.CurrentHP
                .Select(hp => (float)hp)
                .ToReadOnlyReactiveProperty();

            // 최대치도 스탯에서 흘려보낸다 — 성장으로 MaxHP 가 오르면 바와 수치가 그 자리에서 맞춰진다.
            var maxHP = owner.CurrentStats
                .Select(stats => (float)stats.MaxHP)
                .ToReadOnlyReactiveProperty();

            var offset = headTop + _cameraManager.Camera.transform.up * HPBarHeadGap;
            hpBar.Attach(_cameraManager.Camera, owner.transform, offset, HPBarScreenOffset, currentHP, maxHP);

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
