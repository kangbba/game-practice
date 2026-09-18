using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 투사체 프리팹을 코드로 짓는다. 알갱이 하나에 궤적을 달아, 날아가는 게 눈에 보이게 한다.
    /// 투사체는 전부 이 자리에서 만든다 — 궤적 없는 투사체는 두지 않는다.
    /// </summary>
    public static class ProjectileBuilder
    {
        private const string ArrowPath = "Assets/Game/Equipment/Weapon/Projectiles/Arrow.prefab";
        private const string ArrowSpritePath = "Assets/Game/Equipment/Weapon/Projectiles/Arrow.png";

        /// <summary>화살 그림 크기. 1.28 유닛짜리 스프라이트라 이 배율이면 히어로 키의 절반쯤 된다.</summary>
        private const float ArrowScale = 1.4f;

        /// <summary>궤적이 남아 있는 시간. 길면 화면이 지저분해진다.</summary>
        private const float TrailSeconds = 0.16f;

        private const float TrailWidth = 0.28f;

        /// <summary>적(20)보다 앞. 궤적은 알갱이 바로 뒤에 깔린다.</summary>
        private const int SpriteOrder = 20;
        private const int TrailOrder = 19;

        public static void Build()
        {
            BuildArrow();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("ProjectileBuilder: 투사체 프리팹 재구성 완료");
        }

        private static void BuildArrow()
        {
            var root = new GameObject("Arrow", typeof(SpriteRenderer), typeof(Projectile));
            root.transform.localScale = Vector3.one * ArrowScale;

            var sprite = root.GetComponent<SpriteRenderer>();
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowSpritePath);
            sprite.sortingOrder = SpriteOrder;

            // 궤적은 촉 끝에서 나온다. 화살은 진행 방향으로 +X 를 향해 돌아간다.
            var bounds = sprite.sprite.bounds;
            var tip = new GameObject("Tip", typeof(TrailRenderer));
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = new Vector3(bounds.max.x, bounds.center.y, 0f);

            var trail = tip.GetComponent<TrailRenderer>();
            StyleTrail(trail, new Color(1f, 0.92f, 0.62f));

            var so = new SerializedObject(root.GetComponent<Projectile>());
            so.FindProperty("_trail").objectReferenceValue = trail;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ArrowPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>꼬리로 갈수록 가늘어지고 투명해진다. 색은 알갱이와 같은 계열로 둔다.</summary>
        private static void StyleTrail(TrailRenderer trail, Color color)
        {
            trail.time = TrailSeconds;
            trail.widthMultiplier = TrailWidth;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.numCapVertices = 2;
            trail.minVertexDistance = 0.02f;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.sortingOrder = TrailOrder;
            trail.receiveShadows = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            trail.material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            trail.colorGradient = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                alphaKeys = new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) },
            };

            // 쏠 때 Projectile 이 켠다. 프리팹 상태로는 꺼 둔다.
            trail.emitting = true;
        }
    }
}
