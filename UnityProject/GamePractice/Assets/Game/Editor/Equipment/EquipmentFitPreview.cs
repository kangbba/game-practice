using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Sayne.Editor
{
    /// <summary>실제 영웅 프리팹과 애니메이션으로 장비 착용 상태를 캡처한다.</summary>
    public static class EquipmentFitPreview
    {
        private const string Output = "Documentation/Equipment/Previews";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.update -= CheckRequest;
            EditorApplication.update += CheckRequest;
        }

        private static void CheckRequest()
        {
            const string bake = "tmp/equipment/bake.request";
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating &&
                !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(bake))
            {
                File.Delete(bake);
                EquipmentCatalogBuilder.BakeHeroes();
                return;
            }

            const string request = "tmp/equipment/capture.request";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
            File.Delete(request);
            Capture();
        }

        [MenuItem("★Sayne★/영웅/장비 착용 미리보기 저장", false, 102)]
        public static void Capture()
        {
            Directory.CreateDirectory(Output);
            EquipmentCatalogBuilder.BuildVisual("IronHelm", EquipmentSlot.Helmet);
            foreach (var hero in new[] { "Kage", "Aldric", "Nyx" })
            {
                Render(hero, false);
                Render(hero, true);
            }
            Debug.Log($"EquipmentFitPreview: {Path.GetFullPath(Output)}");
        }

        private static void Render(string hero, bool equipped)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            var qualityPipeline = QualitySettings.renderPipeline;
            var previousTarget = RenderTexture.active;
            var target = new RenderTexture(768, 768, 24);
            Texture2D capture = null;
            Material material = null;
            try
            {
                GraphicsSettings.defaultRenderPipeline = null;
                QualitySettings.renderPipeline = null;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/Characters/Heroes/{hero}/{hero}.prefab");
                var instance = Object.Instantiate(prefab);
                SceneManager.MoveGameObjectToScene(instance, scene);
                var graphic = instance.transform.Find("Graphic");
                foreach (var animator in instance.GetComponentsInChildren<Animator>()) animator.enabled = false;
                if (equipped) EquipmentCatalogBuilder.FitHero(instance);
                HeroActionAnimationBuilder.DressPreview(graphic, hero);
                if (equipped) graphic.GetComponent<CharacterSkin>().Wear(EquipmentSlot.Helmet,
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Equipment/Helmet/IronHelm.prefab"));
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/DarkFantasy2D/Animations/Heroes/{hero}/Idle.anim");
                if (clip != null) clip.SampleAnimation(graphic.gameObject, 0f);
                material = new Material(Shader.Find("Sprites/Default"));
                foreach (var sprite in instance.GetComponentsInChildren<SpriteRenderer>()) sprite.sharedMaterial = material;
                var cameraObject = new GameObject("Equipment Fit Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.orthographic = true;
                camera.orthographicSize = 1.65f;
                camera.transform.position = new Vector3(.1f, 1.15f, -10);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.15f, .18f, .22f, 1);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                capture = new Texture2D(768, 768, TextureFormat.RGBA32, false);
                capture.ReadPixels(new Rect(0, 0, 768, 768), 0, 0);
                capture.Apply();
                File.WriteAllBytes($"{Output}/{hero}-{(equipped ? "iron-helm" : "base")}.png", capture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (capture != null) Object.DestroyImmediate(capture);
                if (material != null) Object.DestroyImmediate(material);
                target.Release();
                Object.DestroyImmediate(target);
                EditorSceneManager.ClosePreviewScene(scene);
                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = qualityPipeline;
            }
        }
    }
}
