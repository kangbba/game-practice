using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 지평선 배경을 4겹으로 깐다.
    ///
    ///   Sky          카메라 자식, 화면을 꽉 채우는 그라디언트   order -1000  (안 움직임 = 무한히 먼 하늘)
    ///   RidgeFar     먼 능선, 카메라 가로 이동의 85% 추종        order -400
    ///   RidgeNear    가까운 능선, 70% 추종                       order -350
    ///   Haze         지면 먼 쪽을 하늘색으로 녹이는 띠           order -50   (프롭보다 위)
    ///
    /// 카메라가 65.6도 내려보고 세로 반FOV 가 30도라 진짜 지평선은 화면에 절대 안 들어온다.
    /// 그래서 하늘은 스카이박스가 아니라 카메라에 붙은 카드로, 지평선은 지면 끝에 얹은 능선으로 만든다.
    /// </summary>
    public static class BackdropBuilder
    {
        private const string MapPath = "Assets/Game/Maps/WorldMap.prefab";

        /// <summary>지면의 위쪽 끝. 스프라이트 48x24 에 Graphic 스케일 2 가 걸려 96x48 이라 여기가 +24 다.</summary>
        private const float GroundTopY = 24f;

        // 끝난 일회성 작업이라 메뉴에서 내렸다. 다시 돌릴 일이 생기면 MenuItem 을 잠깐 붙인다.
        public static void Build()
        {
            BackdropArtBuilder.Build();
            BuildSky();
            BuildMapLayers();

            Debug.Log("BackdropBuilder: 하늘 카드 + 능선 2겹 + 안개 배치 완료");
        }

        private static void BuildSky()
        {
            var camera = Camera.main;
            var previous = camera.transform.Find("Sky");
            if (previous != null) Object.DestroyImmediate(previous.gameObject);

            var sky = new GameObject("Sky", typeof(SpriteRenderer), typeof(SkyCard));
            sky.transform.SetParent(camera.transform, false);

            var renderer = sky.GetComponent<SpriteRenderer>();
            renderer.sprite = Art("Sky");
            renderer.sortingOrder = -1000;

            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
        }

        private static void BuildMapLayers()
        {
            var root = PrefabUtility.LoadPrefabContents(MapPath);

            Layer(root, "RidgeFar", new Vector3(0f, GroundTopY - 0.2f, 0f), Vector3.one, -400, new Vector2(0.85f, 0f));
            Layer(root, "RidgeNear", new Vector3(0f, GroundTopY - 0.3f, 0f), Vector3.one, -350, new Vector2(0.7f, 0f));
            Layer(root, "Haze", new Vector3(0f, GroundTopY - 3.4f, 0f), new Vector3(16f, 0.85f, 1f), -50, Vector2.zero);

            PrefabUtility.SaveAsPrefabAsset(root, MapPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        /// <summary>같은 이름이 있으면 지우고 새로 만든다. 여러 번 눌러도 겹치지 않는다.</summary>
        private static void Layer(GameObject root, string name, Vector3 position, Vector3 scale, int sortingOrder,
            Vector2 followRatio)
        {
            var previous = root.transform.Find(name);
            if (previous != null) Object.DestroyImmediate(previous.gameObject);

            var layer = new GameObject(name, typeof(SpriteRenderer));
            layer.transform.SetParent(root.transform, false);
            layer.transform.localPosition = position;
            layer.transform.localScale = scale;

            var renderer = layer.GetComponent<SpriteRenderer>();
            renderer.sprite = Art(name);
            renderer.sortingOrder = sortingOrder;

            if (followRatio == Vector2.zero) return;

            var parallax = layer.AddComponent<ParallaxLayer>();
            var so = new SerializedObject(parallax);
            so.FindProperty("_followRatio").vector2Value = followRatio;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite Art(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BackdropArtBuilder.ArtFolder}/{name}.png");
            if (sprite == null) throw new System.InvalidOperationException($"Backdrop sprite missing: {name}");
            return sprite;
        }
    }
}
