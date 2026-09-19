using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sayne.Editor
{
    /// <summary>
    /// 대사 프리팹 두 개를 만든다 — 화면 아래 초상화 대사(SpeechBubbleWidget), 캐릭터 머리 위 말풍선(OverlaySpeechBubble).
    /// 머리 위 말풍선은 눌러서 넘기는 대사와 알아서 사라지는 혼잣말을 같이 맡는다.
    /// 그림은 SayneAssets/UI/Speech/Sprites 의 PNG 를 그대로 쓰고, 여기서는 임포트 설정만 맞춘다.
    /// 끝에 어드레서블 등록과 미리보기 저장까지 한 번에 돌린다.
    /// </summary>
    public static class SpeechBubbleBuilder
    {
        /// <summary>머리 위 말풍선 왼쪽의 초상화 한 변. 풍선 높이(124) 안에 들어간다.</summary>
        private const float OverlayPortraitSize = 96f;

        private const string Folder = "Assets/SayneAssets/UI/Speech";
        private const string SpriteFolder = Folder + "/Sprites";
        private const string FontPath = "Assets/Fonts/TMP/SB_Aggro_Bold SDF.asset";
        private const string PreviewPortraitPath = "Assets/Game/Characters/Heroes/Kage/KagePortrait.png";

        private const float BubbleBorder = 44f;

        private static readonly Color Ink = new Color(0.086f, 0.125f, 0.227f);
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.45f);

        private static TMP_FontAsset _font;
        private static Sprite _bubble;
        private static Sprite _tail;
        private static Sprite _disc;
        private static Sprite _ring;
        private static Sprite _nextMark;
        private static Sprite _portraitMask;

        public static void Build()
        {
            LoadArt();

            BuildSpeechBubbleWidget();
            BuildOverlaySpeechBubble();

            AddressablesSetup.Setup();
            CapturePreviews();

            Debug.Log("SpeechBubbleBuilder: SpeechBubbleWidget·OverlaySpeechBubble 프리팹 빌드 완료");
        }

        private static void LoadArt()
        {
            _bubble = ImportSprite("Bubble", Vector4.one * BubbleBorder);
            _tail = ImportSprite("Tail", Vector4.zero);
            _disc = ImportSprite("PortraitDisc", Vector4.zero);
            _ring = ImportSprite("PortraitRing", Vector4.zero);
            _nextMark = ImportSprite("NextMark", Vector4.zero);
            _portraitMask = ImportSprite("PortraitMask", Vector4.zero);
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        }

        // ---- 프리팹 ----

        private static void BuildSpeechBubbleWidget()
        {
            var root = FullScreenRoot("SpeechBubbleWidget");
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var tapBtn = TapCatcher(root, "Dim", DimColor);

            var box = Rect(root, "Box", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1320f, 240f));

            var portraitRoot = Rect(box, "PortraitRoot", new Vector2(0f, 0.5f), Vector2.one * 0.5f, new Vector2(120f, 0f), new Vector2(240f, 240f));
            Img(Stretch(portraitRoot, "Disc"), _disc);
            var mask = Rect(portraitRoot, "Mask", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(216f, 216f));
            Img(mask, _portraitMask);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var portrait = Img(Stretch(mask, "Portrait"), null);
            portrait.preserveAspect = true;
            Img(Stretch(portraitRoot, "Ring"), _ring);

            var bubble = Bubble(box, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(290f, 0f), new Vector2(1030f, 220f),
                new Vector2(0f, 0.5f), 0f, new Vector2(48f, 28f), new Vector2(64f, 28f), 38f, HorizontalAlignmentOptions.Left,
                new Vector2(-54f, 40f), 1f, false);

            var widget = root.gameObject.AddComponent<SpeechBubbleWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_group").objectReferenceValue = group;
            so.FindProperty("_tapBtn").objectReferenceValue = tapBtn;
            so.FindProperty("_portraitRoot").objectReferenceValue = portraitRoot;
            so.FindProperty("_portrait").objectReferenceValue = portrait;
            so.FindProperty("_player").objectReferenceValue = bubble;
            so.ApplyModifiedPropertiesWithoutUndo();

            Save(root.gameObject);
        }

        private static void BuildOverlaySpeechBubble()
        {
            var root = FullScreenRoot("OverlaySpeechBubble");
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var tapBtn = TapCatcher(root, "TapCatcher", Color.clear);

            // 꼬리 끝이 Follower 의 원점에 오도록 풍선을 꼬리 길이만큼 띄운다.
            var follower = Rect(root, "Follower", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            // 왼쪽에 말하는 이 얼굴이 들어가므로 글은 그만큼 오른쪽에서 시작한다.
            var bubble = Bubble(follower, Vector2.one * 0.5f, new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(640f, 124f),
                new Vector2(0.5f, 0f), 90f, new Vector2(OverlayPortraitSize + 40f, 24f), new Vector2(52f, 24f), 30f,
                HorizontalAlignmentOptions.Left, new Vector2(-30f, 22f), 0.75f, true);

            var portraitRoot = Rect((RectTransform)bubble.transform, "Portrait", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(18f, 0f), Vector2.one * OverlayPortraitSize);
            var portrait = portraitRoot.gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;

            var overlay = root.gameObject.AddComponent<OverlaySpeechBubble>();
            var so = new SerializedObject(overlay);
            so.FindProperty("_group").objectReferenceValue = group;
            so.FindProperty("_tapBtn").objectReferenceValue = tapBtn;
            so.FindProperty("_follower").objectReferenceValue = follower;
            so.FindProperty("_player").objectReferenceValue = bubble;
            so.FindProperty("_portrait").objectReferenceValue = portrait;
            so.ApplyModifiedPropertiesWithoutUndo();

            Save(root.gameObject);
        }

        /// <param name="tailAnchor">꼬리가 붙는 풍선 가장자리. 꼬리 그림은 끝이 왼쪽을 보므로 아래로 내리려면 90도 돌린다.</param>
        /// <param name="padMin">글 상자 여백(왼쪽, 아래).</param>
        /// <param name="padMax">글 상자 여백(오른쪽, 위). 오른쪽은 넘김 표시 자리만큼 더 준다.</param>
        private static TextPlayer Bubble(RectTransform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
            Vector2 tailAnchor, float tailRotation, Vector2 padMin, Vector2 padMax, float fontSize,
            HorizontalAlignmentOptions alignment, Vector2 nextMarkPos, float nextMarkScale, bool isWidthFitted)
        {
            var body = Rect(parent, "Bubble", anchor, pivot, pos, size);
            Img(body, _bubble).type = Image.Type.Sliced;

            // 꼬리 밑동 10px 이 풍선 안으로 들어가 테두리를 덮는다.
            var tail = Rect(body, "Tail", tailAnchor, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(56f, 64f));
            tail.localRotation = Quaternion.Euler(0f, 0f, tailRotation);
            tail.anchoredPosition = tail.localRotation * new Vector3(10f, 0f, 0f);
            Img(tail, _tail);

            var textRT = Stretch(body, "Text");
            textRT.offsetMin = padMin;
            textRT.offsetMax = -padMax;
            var text = textRT.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.fontSize = fontSize;
            text.color = Ink;
            text.horizontalAlignment = alignment;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = 12f;
            text.raycastTarget = false;
            text.text = "여기에 대사가 들어갑니다.";

            var nextMark = Rect(body, "NextMark", new Vector2(1f, 0f), Vector2.one * 0.5f, nextMarkPos, new Vector2(36f, 28f) * nextMarkScale);
            Img(nextMark, _nextMark);

            var bubble = body.gameObject.AddComponent<TextPlayer>();
            var so = new SerializedObject(bubble);
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_text").objectReferenceValue = text;
            so.FindProperty("_nextMark").objectReferenceValue = nextMark;
            so.FindProperty("_isWidthFitted").boolValue = isWidthFitted;
            so.ApplyModifiedPropertiesWithoutUndo();

            return bubble;
        }

        // ---- 미리보기 ----

        private static void CapturePreviews()
        {
            var output = Path.GetFullPath("HUDPreviews");
            Directory.CreateDirectory(output);

            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(PreviewPortraitPath);
            Capture($"{Folder}/SpeechBubbleWidget.prefab", Path.Combine(output, "tutorial-widget.png"), instance =>
            {
                instance.transform.Find("Box/PortraitRoot/Mask/Portrait").GetComponent<Image>().sprite = portrait;
                instance.GetComponentInChildren<TextMeshProUGUI>().text =
                    "왼쪽 아래 조이스틱을 끌어서 움직여 보세요. 적에게 가까이 가면 자동으로 공격합니다.";
            });
            Capture($"{Folder}/OverlaySpeechBubble.prefab", Path.Combine(output, "tutorial-overlay-bubble.png"), instance =>
            {
                instance.transform.Find("Follower/Bubble/Portrait").GetComponent<Image>().sprite = portrait;
                instance.GetComponentInChildren<TextMeshProUGUI>().text = "저 고블린부터 잡자!";
            });
        }

        private static void Capture(string prefabPath, string outputPath, System.Action<GameObject> fill)
        {
            const int Width = 1920;
            const int Height = 1080;

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

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                var instance = Object.Instantiate(prefab, canvas.transform, false);
                fill(instance);

                Canvas.ForceUpdateCanvases();
                foreach (var text in instance.GetComponentsInChildren<TextMeshProUGUI>()) text.ForceMeshUpdate();
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
                // 카메라가 먼저 사라져야 한다 — 찍는 중인 렌더텍스처를 반납하면 유니티가 경고를 뱉는다.
                EditorSceneManager.ClosePreviewScene(scene);
                target.Release();
                Object.DestroyImmediate(target);
                GraphicsSettings.defaultRenderPipeline = previousPipeline;
                QualitySettings.renderPipeline = previousQualityPipeline;
            }
        }

        // ---- 공용 헬퍼 ----

        private static Sprite ImportSprite(string name, Vector4 border)
        {
            var path = $"{SpriteFolder}/{name}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static RectTransform FullScreenRoot(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>화면 전체를 덮어 아래 UI 의 입력을 막고, 눌리면 대사를 넘기는 버튼.</summary>
        private static Button TapCatcher(RectTransform parent, string name, Color color)
        {
            var image = Img(Stretch(parent, name), null);
            image.color = color;
            image.raycastTarget = true;
            var btn = image.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            return btn;
        }

        private static RectTransform Rect(RectTransform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static RectTransform Stretch(RectTransform parent, string name)
        {
            var rt = Rect(parent, name, Vector2.zero, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static Image Img(RectTransform rt, Sprite sprite)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static void Save(GameObject temp)
        {
            PrefabUtility.SaveAsPrefabAsset(temp, $"{Folder}/{temp.name}.prefab");
            Object.DestroyImmediate(temp);
        }
    }
}
