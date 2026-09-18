using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Sayne.Editor
{
    /// <summary>
    /// 로딩 화면 프리팹을 만든다. 붉은 달 아래 성이 선 배경 위에 불티가 오르고,
    /// 왼쪽에 제목, 아래에 게이지·팁, 다 차면 그 자리에 "TAP TO START".
    /// 화면 전체가 탭 판이다 — 준비가 끝나면 어디를 눌러도(누르는 순간) 시작한다.
    /// </summary>
    public static class LoadingPanelBuilder
    {
        private const string Folder = "Assets/Game/Loading";
        private const string ArtFolder = Folder + "/Art";
        private const string PanelPath = Folder + "/LoadingPanel.prefab";

        /// <summary>제목·영문용 세리프. 로마 비문 같은 대문자라 판타지 제목에 어울린다. OFL.</summary>
        private const string DisplayFontPath = Folder + "/Fonts/Cinzel-SemiBold.ttf";

        /// <summary>한글 문구용. 에셋 팩에 들어 있던 나눔고딕 Bold 다.</summary>
        private const string BodyFontPath = "Assets/DarkFantasy2D/Art/Interface.ttf";

        /// <summary>열린 에디터에 빌드·캡처를 시키는 문. 이 파일이 생기면 한 번 돌고 지운다.</summary>
        private const string BuildRequest = "tmp/loading/build.request";

        private const float GaugeWidth = 760f;
        private const float GaugeHeight = 3f;

        private static readonly Color Ivory = new Color(0.97f, 0.91f, 0.84f);
        private static readonly Color Rose = new Color(0.93f, 0.62f, 0.56f);
        private static readonly Color Ember = new Color(1f, 0.52f, 0.26f);
        private static readonly Color Hairline = new Color(1f, 0.62f, 0.48f, 0.55f);
        private static readonly Color GaugeBack = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color GaugeFill = new Color(1f, 0.46f, 0.32f);

        private static TMP_FontAsset _display;
        private static TMP_FontAsset _body;
        private static Material _displayGlow;

        [InitializeOnLoadMethod]
        private static void WatchRequest()
        {
            EditorApplication.update -= CheckRequest;
            EditorApplication.update += CheckRequest;
        }

        private static void CheckRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(BuildRequest)) return;
            File.Delete(BuildRequest);
            Build();
        }

        public static void Build()
        {
            _display = FontAsset(DisplayFontPath, "Cinzel SDF");
            _body = FontAsset(BodyFontPath, "NanumGothicBold SDF");
            _displayGlow = GlowMaterial(_display);

            var background = ImportSprite("LoadingBackground", Vector4.zero, 2048);
            var ember = ImportSprite("Ember", Vector4.zero, 64);
            var shade = ImportSprite("Shade", Vector4.zero, 256);

            Compose(background, ember, shade);

            AddressablesSetup.Setup();
            CapturePreviews();

            Debug.Log("LoadingPanelBuilder: 로딩 화면 프리팹 빌드 완료");
        }

        private static void Compose(Sprite backgroundSprite, Sprite emberSprite, Sprite shadeSprite)
        {
            var art = BattleHUDArtBuilder.ArtFolder;
            var vignette = AssetDatabase.LoadAssetAtPath<Sprite>($"{art}/Frames/Vignette.png");

            // 뿌리가 곧 탭 받는 판이다. 검은 바탕은 배경이 화면비에 안 맞을 때 빈 곳을 메운다.
            var root = new GameObject("LoadingPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(LoadingPanel));
            root.layer = LayerMask.NameToLayer("UI");
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            var rootImage = Img(rootRect, Color.black, null);
            rootImage.raycastTarget = true;
            // 시작 탭은 PointerDown 으로 받는다. 받아 줄 항목은 준비가 끝난 뒤 패널이 직접 단다.
            var tapTrigger = root.AddComponent<EventTrigger>();

            // ---- 배경 ----

            // 화면비가 달라도 빈틈 없이 덮는다. 넘치는 쪽은 잘린다.
            var background = Rect(rootRect, "Background", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(1920f, 1080f));
            Img(background, Color.white, backgroundSprite);
            var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;

            var embers = Stretch(rootRect, "Embers", 0f);
            var emberTemplate = Img(Rect(embers, "Ember", new Vector2(0.5f, 0f), Vector2.one * 0.5f, Vector2.zero, new Vector2(20f, 20f)),
                Ember, emberSprite);

            // 가장자리를 눌러 시선을 가운데로 모으고, 아래는 한 번 더 어둡게 깔아 게이지·글자가 읽히게 한다.
            Img(Stretch(rootRect, "Vignette", -40f), new Color(0f, 0f, 0f, 0.75f), vignette);
            var bottomShade = Rect(rootRect, "BottomShade", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 420f));
            bottomShade.anchorMin = Vector2.zero;
            bottomShade.anchorMax = new Vector2(1f, 0f);
            Img(bottomShade, new Color(0.02f, 0f, 0.02f, 0.9f), shadeSprite);

            // ---- 제목 ----
            // 굵은 외곽선 대신 가는 세리프에 붉은 번짐만 얹는다. 글자 사이를 넓게 벌려 무게를 준다.

            var title = Rect(rootRect, "Title", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150f, 170f), new Vector2(900f, 260f));
            var titleGroup = title.gameObject.AddComponent<CanvasGroup>();

            var mainTitle = Text(Rect(title, "MainTitle", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 30f), new Vector2(900f, 120f)),
                "BLOOD MOON", 92f, Ivory, _display, HorizontalAlignmentOptions.Left);
            mainTitle.fontSharedMaterial = _displayGlow;
            mainTitle.characterSpacing = 14f;

            var divider = Rect(title, "Divider", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, -38f), new Vector2(420f, 1.5f));
            Img(divider, Hairline, null);

            var subTitle = Text(Rect(title, "SubTitle", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, -72f), new Vector2(900f, 36f)),
                "THE HUNT BEGINS AT NIGHTFALL", 22f, Rose, _display, HorizontalAlignmentOptions.Left);
            subTitle.characterSpacing = 36f;

            // ---- 게이지 ----
            // 머리카락처럼 가는 줄 하나. 위에 "~하는 중..." 과 퍼센트만 둔다.

            var gaugeRoot = Rect(rootRect, "Gauge", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(GaugeWidth, 60f));
            var gaugeGroup = gaugeRoot.gameObject.AddComponent<CanvasGroup>();

            var messageText = Text(Rect(gaugeRoot, "Message", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(600f, 32f)),
                "붉은 달을 띄우는 중..", 22f, new Color(1f, 1f, 1f, 0.82f), _body, HorizontalAlignmentOptions.Left);
            messageText.characterSpacing = 2f;
            var percentText = Text(Rect(gaugeRoot, "Percent", new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(160f, 32f)),
                "62%", 22f, Rose, _display, HorizontalAlignmentOptions.Right);

            var bar = Rect(gaugeRoot, "Bar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(GaugeWidth, GaugeHeight));
            Img(bar, GaugeBack, null);

            var fillRect = Rect(bar, "Fill", Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            fillRect.anchorMax = Vector2.one;
            Img(fillRect, GaugeFill, null);
            var fill = fillRect.gameObject.AddComponent<SlicedFillBar>();
            fill.FillAmount = 0.62f;

            // 채움 끝에 붙어 다니는 불씨. 줄이 가늘어도 어디까지 찼는지 눈에 걸린다.
            var head = Rect(bar, "Head", new Vector2(0.62f, 0.5f), Vector2.one * 0.5f, Vector2.zero, new Vector2(26f, 26f));
            Img(head, new Color(1f, 0.7f, 0.45f, 0.95f), emberSprite);

            // ---- 시작 ----

            var tap = Rect(rootRect, "TapToStart", new Vector2(0.5f, 0f), Vector2.one * 0.5f, new Vector2(0f, 110f), new Vector2(460f, 60f));
            var tapGroup = tap.gameObject.AddComponent<CanvasGroup>();
            tapGroup.alpha = 0f;
            var tapText = Text(Stretch(tap, "Label", 0f), "TAP TO START", 32f, Ivory, _display);
            tapText.fontSharedMaterial = _displayGlow;
            tapText.characterSpacing = 40f;
            Img(Rect(tap, "LineLeft", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(110f, 1.5f)),
                Hairline, null);
            Img(Rect(tap, "LineRight", new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(110f, 1.5f)),
                Hairline, null);

            var versionText = Text(Rect(rootRect, "Version", Vector2.right, Vector2.right, new Vector2(-32f, 24f), new Vector2(240f, 28f)),
                "v0.1", 16f, new Color(1f, 1f, 1f, 0.4f), _display, HorizontalAlignmentOptions.Right);
            versionText.characterSpacing = 8f;

            var so = new SerializedObject(root.GetComponent<LoadingPanel>());
            so.FindProperty("_background").objectReferenceValue = background;
            so.FindProperty("_emberRoot").objectReferenceValue = embers;
            so.FindProperty("_emberTemplate").objectReferenceValue = emberTemplate;
            so.FindProperty("_titleGroup").objectReferenceValue = titleGroup;
            so.FindProperty("_titleDivider").objectReferenceValue = divider;
            so.FindProperty("_gaugeGroup").objectReferenceValue = gaugeGroup;
            so.FindProperty("_gaugeFill").objectReferenceValue = fill;
            so.FindProperty("_gaugeHead").objectReferenceValue = head;
            so.FindProperty("_messageText").objectReferenceValue = messageText;
            so.FindProperty("_percentText").objectReferenceValue = percentText;
            so.FindProperty("_tapGroup").objectReferenceValue = tapGroup;
            so.FindProperty("_tapTrigger").objectReferenceValue = tapTrigger;
            so.FindProperty("_versionText").objectReferenceValue = versionText;
            so.FindProperty("_rootGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
            Object.DestroyImmediate(root);
        }

        // ---- 미리보기 ----

        /// <summary>로딩 중(게이지 62%)과 준비 완료(TAP TO START) 두 장. 에디터에선 Awake 가 안 돌아 프리팹에 저장된 모습 그대로 찍힌다.</summary>
        private static void CapturePreviews()
        {
            var output = Path.GetFullPath("HUDPreviews");
            Directory.CreateDirectory(output);

            Capture(Path.Combine(output, "loading-1920x1080.png"), 1920, 1080, _ => { });
            Capture(Path.Combine(output, "loading-2340x1080.png"), 2340, 1080, _ => { });
            Capture(Path.Combine(output, "loading-ready-1920x1080.png"), 1920, 1080, instance =>
            {
                instance.transform.Find("Gauge").GetComponent<CanvasGroup>().alpha = 0f;
                instance.transform.Find("TapToStart").GetComponent<CanvasGroup>().alpha = 1f;
            });
        }

        private static void Capture(string outputPath, int width, int height, System.Action<GameObject> fill)
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

                // 게임 캔버스와 같은 폭 맞춤 — 기준 폭 1920 에 높이만 화면비를 따른다.
                var canvasHeight = 1920f * height / width;

                var cameraObject = new GameObject("Preview Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.transform.position = new Vector3(0f, 0f, -100f);
                camera.orthographic = true;
                camera.orthographicSize = canvasHeight * 0.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.magenta;
                camera.targetTexture = target;

                var canvasObject = new GameObject("Preview Canvas", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920f, canvasHeight);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
                var instance = Object.Instantiate(prefab, canvas.transform, false);
                fill(instance);

                // 에디터에선 AspectRatioFitter 가 스스로 안 돈다. 한 번 손으로 맞춘다.
                var fitter = instance.GetComponentInChildren<AspectRatioFitter>();
                fitter.enabled = false;
                fitter.enabled = true;

                Canvas.ForceUpdateCanvases();
                foreach (var text in instance.GetComponentsInChildren<TextMeshProUGUI>()) text.ForceMeshUpdate();
                camera.Render();

                RenderTexture.active = target;
                capture = new Texture2D(width, height, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
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

        private static Sprite ImportSprite(string name, Vector4 border, int maxSize)
        {
            var path = $"{ArtFolder}/{name}.png";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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

        private static RectTransform Stretch(RectTransform parent, string name, float inset)
        {
            var rt = Rect(parent, name, Vector2.zero, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.one * inset;
            rt.offsetMax = Vector2.one * -inset;
            return rt;
        }

        private static Image Img(RectTransform rt, Color color, Sprite sprite)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// 원본 폰트에서 TMP 폰트 에셋을 한 번 만들어 둔다. 글자는 쓰일 때 채워지는 동적 방식이라 한글도 통째로 굽지 않는다.
        /// 이미 있으면 그대로 쓴다.
        /// </summary>
        private static TMP_FontAsset FontAsset(string sourcePath, string assetName)
        {
            var path = $"{Folder}/Fonts/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            var asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic);
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);

            asset.atlasTextures[0].name = $"{assetName} Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = $"{assetName} Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>글자 뒤로 붉은 빛이 옅게 번지는 재질. 외곽선 없이 밤하늘에서 글자를 띄운다.</summary>
        private static Material GlowMaterial(TMP_FontAsset font)
        {
            var path = $"{Folder}/Fonts/{font.name} Glow.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(font.material);
                AssetDatabase.CreateAsset(material, path);
            }

            material.CopyPropertiesFromMaterial(font.material);
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", new Color(0.85f, 0.12f, 0.10f, 0.55f));
            material.SetFloat("_UnderlaySoftness", 1f);
            material.SetFloat("_UnderlayDilate", 0.1f);
            material.SetFloat("_UnderlayOffsetX", 0f);
            material.SetFloat("_UnderlayOffsetY", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static TextMeshProUGUI Text(RectTransform rt, string text, float size, Color color, TMP_FontAsset font,
            HorizontalAlignmentOptions horizontal = HorizontalAlignmentOptions.Center)
        {
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.horizontalAlignment = horizontal;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;

            // 자간을 넓게 벌리는 글이라 커닝 쌍(TO·AT 등)이 끼면 간격이 들쭉날쭉해진다. 고른 간격이 낫다.
            // 목록에서 kern 만 빼면 TMP 가 옛 설정을 옮기면서 도로 넣는다 — 빈 목록을 통째로 준다.
            tmp.fontFeatures = new System.Collections.Generic.List<OTL_FeatureTag>();
            return tmp;
        }
    }
}
