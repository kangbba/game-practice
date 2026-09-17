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
                EquipmentCatalogBuilder.BuildBodyVisuals();
                EquipmentCatalogBuilder.BakeHeroes();
                AddressablesSetup.Setup();
                return;
            }

            const string icons = "tmp/equipment/icons.request";
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating &&
                !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(icons))
            {
                File.Delete(icons);
                CaptureIcons();
                return;
            }

            const string request = "tmp/equipment/capture.request";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
            File.Delete(request);
            Capture();
        }

        /// <summary>게임이 쓰는 아이콘 무대로 장비 프리팹 전부를 찍어 저장한다. 장비창에 뜰 아이콘이 이것과 같다.</summary>
        private static void CaptureIcons()
        {
            Directory.CreateDirectory($"{Output}/Icons");
            var stage = new EquipmentIconStage();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { EquipmentCatalogBuilder.Root }))
            {
                var visual = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (visual.GetComponentInChildren<SpriteRenderer>(true) == null) continue;
                var icon = stage.Shoot(visual);
                File.WriteAllBytes($"{Output}/Icons/{visual.name}.png", icon.texture.EncodeToPNG());
                Object.DestroyImmediate(icon.texture);
                Object.DestroyImmediate(icon);
            }

            stage.Dispose();
            Debug.Log($"EquipmentFitPreview: 아이콘 저장 {Path.GetFullPath(Output)}/Icons");
        }

        [MenuItem("★Sayne★/영웅/장비 착용 미리보기 저장", false, 102)]
        public static void Capture()
        {
            Directory.CreateDirectory(Output);
            EquipmentCatalogBuilder.BuildVisual("IronHelm", EquipmentSlot.Helmet);
            var heroes = new[] { "Kage", "Aldric", "Nyx" };
            foreach (var hero in heroes)
            {
                Render(hero, null);
                foreach (var owner in heroes) Render(hero, owner);
            }
            Debug.Log($"EquipmentFitPreview: {Path.GetFullPath(Output)}");
        }

        /// <summary>outfitOwner 의 신발·몸통과 철투구를 입혀 찍는다. null 이면 구워진 기본 차림 그대로.</summary>
        private static void Render(string hero, string outfitOwner)
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
                HeroActionAnimationBuilder.DressPreview(graphic, hero);
                if (outfitOwner != null)
                {
                    var skin = graphic.GetComponent<CharacterSkin>();
                    var chest = outfitOwner == "Aldric" ? "AldricCoat" : outfitOwner == "Kage" ? "KageArmor" : "NyxDress";
                    skin.Wear(EquipmentSlot.Boots, AssetDatabase.LoadAssetAtPath<GameObject>(
                        $"Assets/Game/Equipment/Boots/{outfitOwner}Boots.prefab"));
                    skin.Wear(EquipmentSlot.Chest, AssetDatabase.LoadAssetAtPath<GameObject>(
                        $"Assets/Game/Equipment/Chest/{chest}.prefab"));
                    if (outfitOwner == hero) skin.Wear(EquipmentSlot.Helmet,
                        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Equipment/Helmet/IronHelm.prefab"));
                }
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
                File.WriteAllBytes($"{Output}/{hero}-{(outfitOwner == null ? "base" : $"wears-{outfitOwner}")}.png", capture.EncodeToPNG());
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
