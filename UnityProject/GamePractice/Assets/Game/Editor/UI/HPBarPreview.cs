using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sayne.Editor
{
    /// <summary>
    /// 머리 위 HP 바 프리팹(영웅·보스)을 초상화를 끼운 채로 찍어 HUDPreviews 에 저장한다. 에셋은 건드리지 않는다.
    /// 바가 작아서 크게 키워 찍고, 가운데 세로선을 그어 가로 가운데 정렬을 눈으로 볼 수 있게 한다.
    /// </summary>
    public static class HPBarPreview
    {
        private const int Width = 1200;
        private const int Height = 500;
        private const float Zoom = 4f;

        private static readonly (string prefab, string portrait, string output)[] Targets =
        {
            ("Assets/Game/UI/HeroOverlayHPBar.prefab", "Assets/Game/Characters/Heroes/Kage/KagePortrait.png", "hpbar-hero.png"),
            ("Assets/Game/UI/BossOverlayHPBar.prefab", "Assets/Game/Characters/Enemies/OgreBoss/OgreBossPortrait.png", "hpbar-boss.png"),
        };

        public static void Capture()
        {
            var output = Path.GetFullPath("HUDPreviews");
            Directory.CreateDirectory(output);

            foreach (var (prefab, portrait, file) in Targets)
            {
                Render(prefab, AssetDatabase.LoadAssetAtPath<Sprite>(portrait), Path.Combine(output, file));
            }
        }

        private static void Render(string prefabPath, Sprite portrait, string outputPath)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var previousPipeline = GraphicsSettings.defaultRenderPipeline;
            var previousQualityPipeline = QualitySettings.renderPipeline;
            var previousTarget = RenderTexture.active;
            var target = new RenderTexture(Width, Height, 24);
            Texture2D capture = null;
            try
            {
                GraphicsSettings.defaultRenderPipeline = null;
                QualitySettings.renderPipeline = null;

                var cameraObject = new GameObject("Preview Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.transform.position = new Vector3(0f, 0f, -100f);
                camera.orthographic = true;
                camera.orthographicSize = Height * 0.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.30f, 0.42f, 0.40f);
                camera.targetTexture = target;

                var canvasObject = new GameObject("Preview Canvas", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(Width, Height);

                // 가로 가운데 표시선. 바 뿌리(따라가는 지점)가 이 선 위에 온다.
                var guide = new GameObject("Center", typeof(RectTransform), typeof(Image));
                guide.transform.SetParent(canvas.transform, false);
                ((RectTransform)guide.transform).sizeDelta = new Vector2(2f, Height);
                guide.GetComponent<Image>().color = new Color(1f, 0.3f, 0.3f, 0.8f);

                var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), canvas.transform, false);
                instance.transform.localScale = Vector3.one * Zoom;
                guide.transform.SetAsLastSibling();

                var bar = new SerializedObject(instance.GetComponent<OverlayHPBar>());
                var image = (Image)bar.FindProperty("_portrait").objectReferenceValue;
                if (image != null) image.sprite = portrait;

                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture.active = target;
                capture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                capture.Apply();
                File.WriteAllBytes(outputPath, capture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (capture != null) Object.DestroyImmediate(capture);
                EditorSceneManager.ClosePreviewScene(scene);
                target.Release();
                Object.DestroyImmediate(target);
                GraphicsSettings.defaultRenderPipeline = previousPipeline;
                QualitySettings.renderPipeline = previousQualityPipeline;
            }
        }
    }
}
