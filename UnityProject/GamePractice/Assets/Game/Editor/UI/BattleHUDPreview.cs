using System;
using System.Collections.Generic;
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
        /// <summary>열린 에디터에 UI 빌드·캡처를 시키는 문. 이 파일이 생기면 한 번 돌고 지운다.</summary>
        private const string CaptureRequest = "tmp/hud/build.request";

        [InitializeOnLoadMethod]
        private static void WatchRequest()
        {
            EditorApplication.update -= CheckRequest;
            EditorApplication.update += CheckRequest;
        }

        private static void CheckRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(CaptureRequest)) return;
            File.Delete(CaptureRequest);
            // UI 프리팹 전부(HUD·팝업, 튜토리얼·말풍선)를 굽고, 어드레서블에 올린 뒤 찍는다. 튜토리얼 빌더가 등록까지 한다.
            BattlePhaseUIBuilder.Build();
            TutorialWidgetBuilder.Build();
            Capture();
        }

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
            RenderQuestStates(output);
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
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/UI/BattlePanel.prefab");
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
                    ultimate.Preview("궁극기", cooldownRatio: 0.65f, cooldownRemain: 5.4f);
                }
                ValidateBindings(panel);
                ValidateRaycasts(panel);
                Canvas.ForceUpdateCanvases();
                ValidateLayout(safeRect);
                if (isEquipment)
                {
                    // 팝업은 HUD 밖 독립 프리팹이다. 게임처럼 HUD 위에 화면을 꽉 채워 띄운다.
                    var popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/UI/Popup/EquipmentWindow.prefab");
                    var window = Object.Instantiate(popupPrefab, canvas.transform, false).GetComponent<EquipmentWindow>();
                    ValidateBindings(window.gameObject);
                    PreviewEquipment(window);
                }
                Canvas.ForceUpdateCanvases();
                foreach (var text in canvas.GetComponentsInChildren<TextMeshProUGUI>()) text.ForceMeshUpdate();
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

        /// <summary>퀘스트 위젯 하나를 여러 상태로 세로로 늘어놓고 찍는다. 가장 긴 제목·큰 수치·완료·전부 끝남.</summary>
        private static void RenderQuestStates(string output)
        {
            const int width = 460;
            const int rowHeight = 170;
            var states = new (int index, int progress)[] { (2, 4), (7, 5), (6, 100), (QuestPlans.Count, 0) };
            var height = rowHeight * states.Length;

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
                var cameraObject = new GameObject("Quest Preview Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.transform.position = new Vector3(0f, 0f, -100f);
                camera.orthographic = true;
                camera.orthographicSize = height * 0.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.18f, 0.20f);
                camera.targetTexture = target;
                var canvasObject = new GameObject("Quest Preview Canvas", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(width, height);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/UI/Widgets/QuestWidget.prefab");
                for (var i = 0; i < states.Length; i++)
                {
                    var widget = Object.Instantiate(prefab, canvas.transform, false).GetComponent<QuestWidget>();
                    var rect = (RectTransform)widget.transform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(40f, -20f - i * rowHeight);

                    var quest = QuestPlans.Get(states[i].index);
                    var isComplete = quest != null && states[i].progress >= quest.Goal;
                    widget.Preview(quest, states[i].index, states[i].progress, isComplete);
                }

                Canvas.ForceUpdateCanvases();
                foreach (var text in canvas.GetComponentsInChildren<TextMeshProUGUI>()) text.ForceMeshUpdate();
                camera.Render();
                RenderTexture.active = target;
                capture = new Texture2D(width, height, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                capture.Apply();
                File.WriteAllBytes(Path.Combine(output, "quest-widget-states.png"), capture.EncodeToPNG());
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

        /// <summary>
        /// 장비창에 설계값 에셋 전부를 후보로 넣고 찍는다 — 가방이 거의 찬 상태다.
        /// 드레스를 입은 채 미늘 갑옷을 고른 장면이라 체력은 오르고(초록) 이동속도는 내리는(빨강) 비교가 함께 나온다.
        /// 아이콘은 게임과 같이 장비 프리팹을 아이콘 무대에서 찍은 것이다.
        /// </summary>
        private static void PreviewEquipment(EquipmentWindow window)
        {
            // 게임과 같은 아이콘 무대로 찍는다. 찍은 그림은 미리보기 창이 살아 있는 동안 쓰이므로 여기서 치우지 않는다.
            var icons = new Dictionary<string, Sprite>();
            var slots = new Dictionary<string, EquipmentSlot>();
            var stage = new EquipmentIconStage();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Equipment" }))
            {
                var visual = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (!visual.TryGetComponent<IEquipment>(out var equipment)) continue;
                slots[visual.name] = equipment.Slot;
                if (visual.GetComponentInChildren<SpriteRenderer>(true) != null) icons[visual.name] = stage.Shoot(visual);
            }

            stage.Dispose();

            var items = new List<(string, EquipmentSlot, string, Sprite, string, CharacterStats)>();

            foreach (var guid in AssetDatabase.FindAssets("t:EquipmentPlan", new[] { "Assets/Game/Equipment" }))
            {
                var plan = AssetDatabase.LoadAssetAtPath<EquipmentPlan>(AssetDatabase.GUIDToAssetPath(guid));

                // 맨손은 가방에 드는 아이템이 아니다.
                if (plan.EquipmentID == EquipmentID.Weapon.BareHands) continue;

                icons.TryGetValue(plan.EquipmentID, out var icon);
                items.Add((plan.EquipmentID, slots[plan.EquipmentID], plan.DisplayName, icon, plan.Description, plan.Stats));
            }

            var equipped = new Dictionary<EquipmentSlot, string>
            {
                [EquipmentSlot.MainHand] = "Sword",
                [EquipmentSlot.Chest] = EquipmentID.Armor.NyxDress,
                [EquipmentSlot.Boots] = EquipmentID.Armor.AldricBoots,
            };

            window.Preview(items, equipped, new CharacterStats(maxHP: 120, moveSpeed: 4f, attackPower: 10), EquipmentID.Armor.KageArmor);
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
        }

        /// <summary>HUD 에서 입력을 받는 건 조이스틱과 버튼 넷뿐이다. 장식이 입력을 가로채면 여기서 걸린다.</summary>
        private static void ValidateRaycasts(GameObject root)
        {
            var raycastTargets = 0;
            foreach (var graphic in root.GetComponentsInChildren<Graphic>())
                if (graphic.raycastTarget) raycastTargets++;
            // 조이스틱 + 스킬·궁극기·장비·성장 버튼.
            if (raycastTargets != 5)
                throw new InvalidOperationException($"Expected joystick and four interactive HUD buttons, found {raycastTargets} raycast targets.");
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
