using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Sayne.Editor
{
    public static class BattleHUDPreview
    {
        public static void BuildAndCapture()
        {
            BattlePhaseUIBuilder.Build();
            Capture();
        }

        [MenuItem("★Sayne★/UI/전투 HUD 미리보기 저장", false, 130)]
        public static void Capture()
        {
            var output = Path.GetFullPath("HUDPreviews");
            Directory.CreateDirectory(output);
            Render(output, 1920, 1080, false);
            Render(output, 2340, 1080, false);
            Render(output, 1920, 1080, true);
            Debug.Log($"Battle HUD validation passed. Previews: {output}");
        }

        private static void Render(string output, int width, int height, bool isEquipment)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var previousPipeline = GraphicsSettings.defaultRenderPipeline;
            var previousQualityPipeline = QualitySettings.renderPipeline;
            var previousTarget = RenderTexture.active;
            var target = new RenderTexture(width, height, 24);
            Texture2D capture = null;
            try
            {
                GraphicsSettings.defaultRenderPipeline = null;
                QualitySettings.renderPipeline = null;
                var cameraObject = new GameObject("HUD Preview Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.transform.position = new Vector3(0f, 0f, -100f);
                camera.orthographic = true;
                camera.orthographicSize = 1920f * height / width * 0.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.18f, 0.20f);
                camera.targetTexture = target;
                var canvasObject = new GameObject("HUD Preview Canvas", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920f, 1920f * height / width);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/UI/BattlePhaseUIPanel.prefab");
                var panel = Object.Instantiate(prefab, canvas.transform, false);
                var safeArea = panel.GetComponentInChildren<BattleHUDSafeArea>();
                safeArea.enabled = false;
                var safeRect = (RectTransform)safeArea.transform;
                safeRect.anchorMin = Vector2.zero;
                safeRect.anchorMax = Vector2.one;
                safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;
                if (width > 2000)
                {
                    safeRect.anchorMin = new Vector2(80f / width, 24f / height);
                    safeRect.anchorMax = new Vector2(1f - 80f / width, 1f);
                    var ultimate = panel.GetComponentsInChildren<SkillButtonWidget>()[1];
                    ultimate.SetCooldownRatio(0.65f);
                    ultimate.SetCooldownRemain(5.4f);
                }
                ValidateBindings(panel);
                Canvas.ForceUpdateCanvases();
                ValidateLayout(safeRect);
                if (isEquipment) panel.GetComponentInChildren<EquipmentWindow>(true).gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                foreach (var text in panel.GetComponentsInChildren<TextMeshProUGUI>()) text.ForceMeshUpdate();
                camera.Render();
                RenderTexture.active = target;
                capture = new Texture2D(width, height, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                capture.Apply();
                var suffix = isEquipment ? "-equipment" : string.Empty;
                File.WriteAllBytes(Path.Combine(output, $"battle-hud-{width}x{height}{suffix}.png"), capture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (capture != null) Object.DestroyImmediate(capture);
                target.Release();
                Object.DestroyImmediate(target);
                EditorSceneManager.ClosePreviewScene(scene);
                GraphicsSettings.defaultRenderPipeline = previousPipeline;
                QualitySettings.renderPipeline = previousQualityPipeline;
            }
        }

        private static void ValidateBindings(GameObject root)
        {
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) throw new InvalidOperationException("HUD contains a missing script.");
                if (component.GetType().Namespace != "Sayne") continue;
                var properties = new SerializedObject(component).GetIterator();
                while (properties.NextVisible(true))
                {
                    if (properties.propertyType == SerializedPropertyType.ObjectReference && properties.objectReferenceValue == null)
                        throw new InvalidOperationException($"Missing HUD binding: {component.name}.{properties.propertyPath}");
                }
            }
            foreach (var skill in root.GetComponentsInChildren<SkillButtonWidget>())
            {
                var icon = skill.transform.Find("Icon").GetComponent<Image>();
                if (icon.sprite == null) throw new InvalidOperationException("Missing skill icon.");
                var fill = skill.transform.Find("CooldownFill").GetComponent<RectTransform>();
                if (fill.anchorMin != Vector2.zero || fill.anchorMax != Vector2.one)
                    throw new InvalidOperationException("Cooldown does not resize with its button.");
            }
            var raycastTargets = 0;
            foreach (var graphic in root.GetComponentsInChildren<Graphic>())
                if (graphic.raycastTarget) raycastTargets++;
            if (raycastTargets != 4)
                throw new InvalidOperationException($"Expected joystick and three interactive HUD buttons, found {raycastTargets} raycast targets.");
        }

        private static void ValidateLayout(RectTransform safeArea)
        {
            var corners = new Vector3[4];
            for (var i = 0; i < safeArea.childCount; i++)
            {
                var rect = (RectTransform)safeArea.GetChild(i);
                if (!rect.gameObject.activeSelf || rect.name == "ReviveText") continue;
                rect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var local = safeArea.InverseTransformPoint(corner);
                    if (local.x < safeArea.rect.xMin - 1f || local.x > safeArea.rect.xMax + 1f ||
                        local.y < safeArea.rect.yMin - 1f || local.y > safeArea.rect.yMax + 1f)
                        throw new InvalidOperationException($"HUD exceeds safe area: {rect.name}");
                }
            }
        }
    }
}
