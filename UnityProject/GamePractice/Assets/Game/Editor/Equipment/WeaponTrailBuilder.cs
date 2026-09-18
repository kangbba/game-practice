using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>영웅 무기 프리팹에 스윙 트레일을 심는다. 무기별 색·길이는 여기 표에서 정한다.</summary>
    internal static class WeaponTrailBuilder
    {
        private const string WeaponFolder = "Assets/Game/Equipment/Weapon";
        private const string TrailName = "Trail";

        /// <summary>날 끝에서 살짝 안쪽. 궤적이 날 끝을 따라가되 삐져나가 보이지 않는 지점이다.</summary>
        private const float TipRatio = 0.85f;

        private class TrailSpec
        {
            public string Prefab;
            public Color Color;
            public float Seconds;
            public float Width;
        }

        private static readonly TrailSpec[] Specs =
        {
            new TrailSpec { Prefab = "Scythe", Color = new Color(1f, 0.93f, 0.72f), Seconds = 0.16f, Width = 0.6f },
            new TrailSpec { Prefab = "Sword", Color = new Color(0.72f, 0.9f, 0.62f), Seconds = 0.2f, Width = 0.7f },
            new TrailSpec { Prefab = "Staff", Color = new Color(0.62f, 0.4f, 0.95f), Seconds = 0.18f, Width = 0.5f },
        };

        [MenuItem("★Sayne★/장비/무기 트레일 재빌드", false, 120)]
        private static void Build()
        {
            foreach (var spec in Specs)
            {
                Plant(spec);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"무기 트레일 재빌드 완료 — {Specs.Length}개");
        }

        private static void Plant(TrailSpec spec)
        {
            var path = $"{WeaponFolder}/{spec.Prefab}/{spec.Prefab}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var old = root.transform.Find(TrailName);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                // 무기 규격상 뿌리가 쥐는 점이고 끝은 +Y 위에 있다. 궤적은 그 선 위, 끝에서 살짝 안쪽에 둔다.
                var visual = root.GetComponent<Weapon>();
                var sprite = root.GetComponentInChildren<SpriteRenderer>();

                var child = new GameObject(TrailName);
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = root.transform.InverseTransformPoint(visual.Tip.position) * TipRatio;

                var trail = child.AddComponent<TrailRenderer>();
                Configure(trail, sprite, spec);

                var weaponTrail = child.AddComponent<WeaponTrail>();
                var serialized = new SerializedObject(weaponTrail);
                serialized.FindProperty("_trail").objectReferenceValue = trail;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Configure(TrailRenderer trail, SpriteRenderer sprite, TrailSpec spec)
        {
            trail.time = spec.Seconds;
            trail.startWidth = spec.Width;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.03f;
            trail.numCapVertices = 4;
            trail.alignment = LineAlignment.TransformZ;
            trail.emitting = false;
            trail.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            trail.sortingLayerID = sprite.sortingLayerID;
            trail.sortingOrder = sprite.sortingOrder - 1;
            trail.colorGradient = Fade(spec.Color);
        }

        /// <summary>같은 색으로 꼬리로 갈수록 사라지는 그라디언트.</summary>
        private static Gradient Fade(Color color)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }
    }
}
