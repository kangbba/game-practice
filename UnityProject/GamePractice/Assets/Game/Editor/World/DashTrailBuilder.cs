using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 돌진 트레일 프리팹을 굽는다. 몸에 붙는 건 ParticleManager 가 돌진하는 동안만 한다 — 프리팹은 꼬리의 모양만 가진다.
    /// 파티클 폴더에 두므로 어드레서블 파티클 라벨이 폴더째 붙는다(AddressablesSetup).
    /// </summary>
    internal static class DashTrailBuilder
    {
        private const string PrefabPath = "Assets/Game/Particles/" + ParticleID.DashTrail + ".prefab";

        /// <summary>잘 보이는 하늘색. 머리 쪽은 거의 흰빛으로 밝고, 꼬리로 갈수록 짙어지며 사라진다.</summary>
        private static readonly Color HeadColor = new Color(0.72f, 0.95f, 1f);
        private static readonly Color TailColor = new Color(0.25f, 0.72f, 1f);

        /// <summary>꼬리가 남는 시간. 돌진은 짧아서 이보다 길면 지나간 길이 한참 남는다.</summary>
        private const float Seconds = 0.28f;

        /// <summary>머리 쪽 굵기(월드 단위). 몸통 폭만 하게 굵어야 "슉" 하고 지나간 게 보인다.</summary>
        private const float Width = 1.1f;

        /// <summary>영웅 몸 뒤에 깔린다. 몸 부위들의 정렬 순서보다 한참 낮다.</summary>
        private const int SortingOrder = -100;

        private static void Build()
        {
            var root = new GameObject(ParticleID.DashTrail);
            var trail = root.AddComponent<TrailRenderer>();

            trail.time = Seconds;
            trail.startWidth = Width;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.numCapVertices = 6;
            trail.numCornerVertices = 2;
            trail.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            trail.sortingLayerName = SortingLayers.Hero;
            trail.sortingOrder = SortingOrder;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(HeadColor, 0f), new GradientColorKey(TailColor, 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"돌진 트레일 굽기 완료 — {PrefabPath}");
        }
    }
}
