using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne.Editor
{
    /// <summary>
    /// 전투 HUD 를 만든다. 데이터를 가지는 UI 는 전용 위젯 프리팹으로 먼저 만들고,
    /// BattlePhaseUIPanel 프리팹은 위젯들을 중첩 프리팹으로 배치만 한다.
    /// 장식은 레이캐스트를 끄고 실제 버튼만 입력을 받는다.
    /// </summary>
    public static class BattlePhaseUIBuilder
    {
        private const string PanelPath = "Assets/Game/UI/BattlePhaseUIPanel.prefab";
        private const string ResultPanelPath = "Assets/Game/UI/ResultPhaseUIPanel.prefab";
        private const string WidgetFolder = "Assets/Game/UI/Widgets";
        private const string FontPath = "Assets/Fonts/TMP/SB_Aggro_Bold SDF.asset";

        private static readonly Color PanelDark = new Color(0.08f, 0.12f, 0.20f, 0.94f);
        private static readonly Color PanelDarker = new Color(0.035f, 0.055f, 0.10f, 0.96f);
        private static readonly Color TextWhite = new Color(0.95f, 0.95f, 0.97f);
        private static readonly Color TextGray = new Color(0.65f, 0.65f, 0.7f);
        private static readonly Color Gold = new Color(0.94f, 0.78f, 0.46f);
        private static readonly Color GemBlue = new Color(0.45f, 0.75f, 1f);
        private static readonly Color HPRed = new Color(0.85f, 0.2f, 0.2f);
        private static readonly Color EXPGreen = new Color(0.4f, 0.8f, 0.3f);
        private static readonly Color StageBlue = new Color(0.25f, 0.86f, 0.96f);
        private static readonly Color GuideGreen = new Color(0.45f, 0.8f, 0.3f);

        private static TMP_FontAsset _font;
        private static Sprite _rounded;
        private static Sprite _circle;
        private static Sprite _panel;
        private static Sprite _medallion;
        private static Sprite _ring;
        private static Sprite _gauge;

        [MenuItem("★Sayne★/1. 전투 HUD 빌드")]
        public static void Build()
        {
            BattleHUDArtBuilder.Build();
            _panel = Art("Frames/Panel");
            _medallion = Art("Frames/Medallion");
            _ring = Art("Frames/Ring");
            _gauge = Art("Frames/Gauge");
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            _rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            _circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            if (!AssetDatabase.IsValidFolder(WidgetFolder))
            {
                AssetDatabase.CreateFolder("Assets/Game/UI", "Widgets");
            }

            var heroStatusPrefab = BuildHeroStatusWidget();
            var currencyPrefab = BuildCurrencyWidget();
            var guidePrefab = BuildGuideWidget();
            var stagePrefab = BuildStageWidget();
            var iconMenuPrefab = BuildIconMenuWidget();
            var circleButtonPrefab = BuildCircleButtonWidget();
            var skillButtonPrefab = BuildSkillButtonWidget();
            var equipSlotPrefab = BuildEquipmentSlotWidget();
            var equipCandidatePrefab = BuildEquipmentCandidateWidget();
            var equipWindowPrefab = BuildEquipmentWindow(equipSlotPrefab, equipCandidatePrefab);

            ComposePanel(heroStatusPrefab, currencyPrefab, guidePrefab, stagePrefab, iconMenuPrefab, circleButtonPrefab,
                skillButtonPrefab, equipWindowPrefab);

            ComposeResultPanel();

            Debug.Log("BattlePhaseUIBuilder: 위젯 프리팹 10종 + 전투 HUD 패널 재구성 완료");
        }

        // ---- 위젯 프리팹 ----

        private static GameObject BuildHeroStatusWidget()
        {
            var root = WidgetRoot("HeroStatusWidget", new Vector2(420f, 124f));
            Img(root, Color.white, _panel);
            var portrait = Rect(root, "Portrait", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(108f, 108f));
            Img(portrait, Color.white, _medallion);
            var clip = Rect(portrait, "PortraitMask", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(94f, 94f));
            Img(clip, Color.white, _circle);
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var portraitIcon = Icon(clip, "HeroPortrait", "Portraits/HeroPortrait", Vector2.one * 0.5f, Vector2.zero, new Vector2(100f, 100f));
            var levelPlate = Rect(portrait, "LevelPlate", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -1f), new Vector2(76f, 26f));
            Img(levelPlate, Color.white, _panel);
            var levelText = Text(Stretch(levelPlate, "LevelText", 2f), "Lv.0", 18f, Gold);
            var nameText = Text(
                Rect(root, "NameText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(132f, -10f), new Vector2(260f, 30f)),
                "이름없음", 25f, TextWhite, HorizontalAlignmentOptions.Left);
            var (hpFill, hpLabel) = Bar(root, "HPBar", new Vector2(132f, -48f), new Vector2(270f, 30f), HPRed, true);
            var (expFill, _) = Bar(root, "EXPBar", new Vector2(166f, -92f), new Vector2(236f, 10f), StageBlue, false);
            Text(Rect(root, "EXPLabel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(132f, -86f), new Vector2(32f, 22f)), "EXP", 13f, Gold);

            var widget = root.gameObject.AddComponent<HeroStatusWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_portrait").objectReferenceValue = portraitIcon;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_levelText").objectReferenceValue = levelText;
            so.FindProperty("_hpFill").objectReferenceValue = hpFill;
            so.FindProperty("_hpLabel").objectReferenceValue = hpLabel;
            so.FindProperty("_expFill").objectReferenceValue = expFill;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildCurrencyWidget()
        {
            var root = WidgetRoot("CurrencyWidget", new Vector2(204f, 46f));
            Img(root, Color.white, _panel);
            Icon(root, "Icon", "Icons/Coin", new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(36f, 36f));
            var amountText = Text(
                Rect(root, "AmountText", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(18f, 0f), new Vector2(144f, 38f)),
                "0", 23f, TextWhite);

            var widget = root.gameObject.AddComponent<CurrencyWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_amountText").objectReferenceValue = amountText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildGuideWidget()
        {
            var root = WidgetRoot("GuideWidget", new Vector2(380f, 130f));
            Img(root, Color.white, _panel);

            var titleText = Text(
                Rect(root, "TitleText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -6f), new Vector2(200f, 28f)),
                "가이드", 18f, Gold, HorizontalAlignmentOptions.Left);

            var reward = Rect(root, "RewardIcon", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 12f), new Vector2(80f, 80f));
            Img(reward, Color.white, _panel);
            Icon(reward, "Gem", "Icons/Gem", new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(42f, 46f));
            var rewardText = Text(
                Rect(reward, "RewardText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(80f, 26f)),
                "0", 20f, GemBlue);

            var descText = Text(
                Rect(root, "DescText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(106f, -38f), new Vector2(264f, 30f)),
                "-", 20f, TextWhite, HorizontalAlignmentOptions.Left);

            var (progressFill, progressLabel) = Bar(root, "ProgressBar", new Vector2(106f, -82f), new Vector2(254f, 22f), GuideGreen, true);

            var widget = root.gameObject.AddComponent<GuideWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_descText").objectReferenceValue = descText;
            so.FindProperty("_rewardText").objectReferenceValue = rewardText;
            so.FindProperty("_progressFill").objectReferenceValue = progressFill;
            so.FindProperty("_progressLabel").objectReferenceValue = progressLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildStageWidget()
        {
            var root = WidgetRoot("StageWidget", new Vector2(420f, 126f), new Vector2(0.5f, 1f));
            var titlePlate = Rect(root, "StagePlate", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(250f, 48f));
            Img(titlePlate, Color.white, _panel);
            var stageText = Text(Stretch(titlePlate, "StageText", 4f), "1-1 단계", 28f, Gold);
            var (progressFill, _) = Bar(root, "ProgressBar", new Vector2(0f, -56f), new Vector2(420f, 20f), StageBlue, false);
            var killPill = Rect(root, "KillPill", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(140f, 36f));
            Img(killPill, Color.white, _panel);
            Icon(killPill, "Skull", "Icons/Skull", new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(24f, 24f));
            var killLabel = Text(
                Rect(killPill, "KillLabel", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(17f, 0f), new Vector2(90f, 32f)),
                "0/0", 21f, TextWhite);

            var widget = root.gameObject.AddComponent<StageWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_stageText").objectReferenceValue = stageText;
            so.FindProperty("_progressFill").objectReferenceValue = progressFill;
            so.FindProperty("_killLabel").objectReferenceValue = killLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildIconMenuWidget()
        {
            var root = WidgetRoot("IconMenuWidget", new Vector2(80f, 104f), new Vector2(0.5f, 1f));
            Img(root, Color.white, _panel);
            Icon(root, "Icon", "Icons/Shield", new Vector2(0.5f, 1f), new Vector2(0f, -37f), new Vector2(54f, 54f));
            var label = Text(
                Rect(root, "Label", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(78f, 26f)),
                "-", 18f, TextWhite);

            var widget = root.gameObject.AddComponent<IconMenuWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildCircleButtonWidget()
        {
            var root = WidgetRoot("CircleButtonWidget", new Vector2(150f, 150f), new Vector2(0.5f, 0.5f));
            var background = Img(root, Color.white, _medallion);

            var label = Text(
                Stretch(root, "Label", 8f),
                "-", 23f, TextWhite);

            var widget = root.gameObject.AddComponent<CircleButtonWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_background").objectReferenceValue = background;
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildSkillButtonWidget()
        {
            var root = WidgetRoot("SkillButtonWidget", new Vector2(148f, 148f), Vector2.one * 0.5f);
            var background = Img(root, Color.white, _medallion);
            background.raycastTarget = true;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            StyleButton(button);
            var icon = Img(Stretch(root, "Icon", 25f), Color.white, Art("Icons/SlashSkill"));
            icon.preserveAspect = true;
            var cooldownFill = Img(Stretch(root, "CooldownFill", 7f), new Color(0.01f, 0.02f, 0.06f, 0.82f), _circle);
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Radial360;
            cooldownFill.fillOrigin = (int)Image.Origin360.Top;
            cooldownFill.fillClockwise = false;
            cooldownFill.fillAmount = 0f;
            Img(Stretch(root, "Rim", 0f), Color.white, _ring);
            var label = Text(
                Rect(root, "Label", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 13f), new Vector2(100f, 26f)),
                "공격", 19f, TextWhite);
            var cooldownText = Text(Stretch(root, "CooldownText", 14f), string.Empty, 32f, TextWhite);

            var widget = root.gameObject.AddComponent<SkillButtonWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_cooldownFill").objectReferenceValue = cooldownFill;
            so.FindProperty("_cooldownText").objectReferenceValue = cooldownText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildEquipmentSlotWidget()
        {
            var root = WidgetRoot("EquipmentSlotWidget", new Vector2(100f, 140f), new Vector2(0.5f, 0.5f));
            var bg = Img(root, Color.white, _panel);
            bg.raycastTarget = true;

            var slotName = Text(
                Rect(root, "SlotName", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(72f, 22f)),
                "-", 18f, TextGray);

            var iconRT = Rect(root, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(64f, 64f));
            iconRT.anchorMin = new Vector2(0.2f, 0.30f);
            iconRT.anchorMax = new Vector2(0.8f, 0.70f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
            var icon = Img(iconRT, Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;

            var itemName = Text(
                Rect(root, "ItemName", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(72f, 22f)),
                "없음", 18f, TextWhite);

            var widget = root.gameObject.AddComponent<EquipmentSlotWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_slotName").objectReferenceValue = slotName;
            so.FindProperty("_itemName").objectReferenceValue = itemName;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildEquipmentCandidateWidget()
        {
            var root = WidgetRoot("EquipmentCandidateWidget", new Vector2(100f, 100f), new Vector2(0.5f, 0.5f));
            var bg = Img(root, Color.white, _panel);
            bg.raycastTarget = true;
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

            var iconRT = Rect(root, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(44f, 44f));
            var icon = Img(iconRT, Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;

            var label = Text(
                Rect(root, "Label", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(68f, 20f)),
                "-", 16f, TextWhite);

            var widget = root.gameObject.AddComponent<EquipmentCandidateWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildEquipmentWindow(GameObject slotPrefab, GameObject candidatePrefab)
        {
            var root = WidgetRoot("EquipmentWindow", new Vector2(840f, 560f), new Vector2(0.5f, 0.5f));
            var bg = Img(root, Color.white, _panel);
            bg.raycastTarget = true;

            Text(Rect(root, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -10f), new Vector2(200f, 40f)),
                "장비", 30f, TextWhite, HorizontalAlignmentOptions.Left);
            var closeButton = MakeButton(root, "CloseButton", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-12f, -12f), new Vector2(52f, 52f), "X");

            // 인체 배치: 위 머리, 좌 머리카락, 우 스카프, 중앙 망토, 오른팔 무기. BodyPart 하나당 슬롯 하나다.
            var head = PlaceSlot(slotPrefab, root, BodyPart.Head, new Vector2(190f, -60f), new Vector2(90f, 90f));
            var hair = PlaceSlot(slotPrefab, root, BodyPart.Hair, new Vector2(78f, -60f), new Vector2(80f, 80f));
            var scarf = PlaceSlot(slotPrefab, root, BodyPart.Neck, new Vector2(302f, -60f), new Vector2(80f, 80f));
            var cape = PlaceSlot(slotPrefab, root, BodyPart.Back, new Vector2(190f, -165f), new Vector2(150f, 180f));
            var weapon = PlaceSlot(slotPrefab, root, BodyPart.RightHand, new Vector2(78f, -165f), new Vector2(80f, 150f));

            Text(Rect(root, "CandidatesLabel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420f, -64f), new Vector2(200f, 30f)),
                "후보", 22f, TextGray, HorizontalAlignmentOptions.Left);

            // 부위별 "벗기" 칸 다섯 개까지 얹히므로 칸을 줄여 4x5 로 깔린다.
            var candidatesRoot = Rect(root, "Candidates", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(420f, -100f), new Vector2(380f, 420f));
            var grid = candidatesRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(76f, 76f);
            grid.spacing = new Vector2(8f, 8f);

            var applyButton = MakeButton(root, "ApplyButton", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 24f), new Vector2(170f, 64f), "적용");

            var window = root.gameObject.AddComponent<EquipmentWindow>();
            var so = new SerializedObject(window);
            var slots = so.FindProperty("_slots");
            slots.arraySize = 5;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = weapon;
            slots.GetArrayElementAtIndex(1).objectReferenceValue = head;
            slots.GetArrayElementAtIndex(2).objectReferenceValue = hair;
            slots.GetArrayElementAtIndex(3).objectReferenceValue = cape;
            slots.GetArrayElementAtIndex(4).objectReferenceValue = scarf;
            so.FindProperty("_candidatesRoot").objectReferenceValue = candidatesRoot;
            so.FindProperty("_candidatePrefab").objectReferenceValue = candidatePrefab.GetComponent<EquipmentCandidateWidget>();
            so.FindProperty("_applyButton").objectReferenceValue = applyButton;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static EquipmentSlotWidget PlaceSlot(GameObject slotPrefab, RectTransform parent, BodyPart bodyPart,
            Vector2 pos, Vector2 size)
        {
            var slot = Place<EquipmentSlotWidget>(slotPrefab, parent, new Vector2(0f, 1f), new Vector2(0.5f, 1f), pos);
            ((RectTransform)slot.transform).sizeDelta = size;
            slot.Setup(bodyPart, BodyParts.DisplayName(bodyPart));
            slot.SetItem("없음", null);
            return slot;
        }

        private static Button MakeButton(RectTransform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos,
            Vector2 size, string label)
        {
            var rt = Rect(parent, name, anchor, pivot, pos, size);
            var image = Img(rt, Color.white, _panel);
            image.raycastTarget = true;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            StyleButton(button);

            Text(Rect(rt, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size),
                label, 24f, TextWhite);

            return button;
        }

        // ---- 패널 조립 ----

        /// <summary>정산 화면. 전투 HUD 와 같은 원리로 화면 전체를 덮고, PhaseUIManager 가 켜고 끈다.</summary>
        private static void ComposeResultPanel()
        {
            var panelRoot = new GameObject("ResultPhaseUIPanel", typeof(RectTransform), typeof(ResultPhaseUIPanel));
            var panelRect = (RectTransform)panelRoot.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var dim = Stretch(panelRect, "Dim", 0f);
            Img(dim, new Color(0f, 0f, 0.05f, 0.72f)).raycastTarget = true;

            var board = Rect(dim, "Board", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(720f, 420f));
            Img(board, new Color(0.06f, 0.06f, 0.12f, 0.96f), _panel);

            var titleText = Text(
                Rect(board, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(640f, 60f)),
                "웨이브 클리어", 46f, Gold);

            var killText = Text(
                Rect(board, "Kill", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 24f), new Vector2(640f, 44f)),
                "처치 0", 32f, TextWhite);

            var timeText = Text(
                Rect(board, "Time", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -28f), new Vector2(640f, 44f)),
                "소요 0.0초", 32f, TextGray);

            var goldText = Text(
                Rect(board, "Gold", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(640f, 40f)),
                "보유 골드 0", 28f, Gold);

            var panel = panelRoot.GetComponent<ResultPhaseUIPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_killText").objectReferenceValue = killText;
            so.FindProperty("_timeText").objectReferenceValue = timeText;
            so.FindProperty("_goldText").objectReferenceValue = goldText;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(panelRoot, ResultPanelPath);
            Object.DestroyImmediate(panelRoot);
        }

        private static void ComposePanel(GameObject heroStatusPrefab, GameObject currencyPrefab, GameObject guidePrefab,
            GameObject stagePrefab, GameObject iconMenuPrefab, GameObject circleButtonPrefab, GameObject skillButtonPrefab,
            GameObject equipmentWindowPrefab)
        {
            var panelRoot = PrefabUtility.LoadPrefabContents(PanelPath);

            for (var i = panelRoot.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(panelRoot.transform.GetChild(i).gameObject);
            }

            var panelRect = (RectTransform)panelRoot.transform;
            var joystick = BuildJoystick(panelRect);
            var root = Stretch(panelRect, "SafeArea", 0f);
            root.gameObject.AddComponent<BattleHUDSafeArea>();

            // 좌상단
            var heroStatus = Place<HeroStatusWidget>(heroStatusPrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f));
            heroStatus.SetName("이름없음");
            heroStatus.SetLevel(0);
            heroStatus.SetHP(5000, 5000);
            heroStatus.SetEXPRatio(0.35f);

            var gold = Place<CurrencyWidget>(currencyPrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -158f));
            gold.SetAmount(1004);
            gold.SetColor(Gold);

            var gem = Place<CurrencyWidget>(currencyPrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(240f, -158f));
            gem.SetAmount(500);
            gem.SetColor(GemBlue);
            gem.transform.Find("Icon").GetComponent<Image>().sprite = Art("Icons/Gem");

            var guide = Place<GuideWidget>(guidePrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -218f));
            guide.SetGuide("성장 가이드 03", "훈련 · 공격력 올리기");
            guide.SetReward(500);
            guide.SetProgress(1, 3);

            // 중앙 상단
            var stage = Place<StageWidget>(stagePrefab, root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f));
            stage.SetStage("1-1 단계");
            stage.SetKills(8, 25);

            // 우상단 메뉴
            var topMenus = new[] { ("더보기", "Settings"), ("이벤트", "Event"), ("던전", "Dungeon"), ("소환", "Summon"), ("상점", "Shop") };
            for (var i = 0; i < topMenus.Length; i++)
            {
                var menu = Place<IconMenuWidget>(iconMenuPrefab, root, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-24f - i * 88f, -24f));
                menu.SetLabel(topMenus[i].Item1);
                SetMenuIcon(menu, topMenus[i].Item2, true);
            }

            // 하단 중앙에 모으고 좌우 조작 공간을 비운다.
            var bottomBar = Rect(root, "BottomBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(620f, 124f));
            Img(bottomBar, Color.white, _panel);
            var bottomMenus = new[] { ("성장", "HealSkill", -246f), ("장비", "Shield", -132f), ("편성", "HeroPortrait", 132f), ("심연 장비", "Chest", 246f) };
            Button equipMenuButton = null;
            foreach (var (label, iconName, offsetX) in bottomMenus)
            {
                var menu = Place<IconMenuWidget>(iconMenuPrefab, bottomBar, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(offsetX, 0f));
                ((RectTransform)menu.transform).sizeDelta = new Vector2(100f, 104f);
                menu.SetLabel(label);
                SetMenuIcon(menu, iconName, label != "장비");
                if (label == "장비")
                {
                    var icon = menu.GetComponent<Image>();
                    icon.raycastTarget = true;
                    equipMenuButton = menu.gameObject.AddComponent<Button>();
                    equipMenuButton.targetGraphic = icon;
                    StyleButton(equipMenuButton);
                }
            }
            var centerSlot = Place<CircleButtonWidget>(circleButtonPrefab, bottomBar, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 14f));
            ((RectTransform)centerSlot.transform).sizeDelta = new Vector2(104f, 104f);
            centerSlot.SetText(string.Empty);
            Icon((RectTransform)centerSlot.transform, "Crest", "Icons/Summon", Vector2.one * 0.5f, new Vector2(0f, 8f), new Vector2(62f, 62f));
            Text(Rect((RectTransform)centerSlot.transform, "Caption", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(88f, 24f)), "전투", 18f, Gold);
            var skillButton = Place<SkillButtonWidget>(skillButtonPrefab, root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 28f));
            skillButton.SetText("스킬");
            var ultimateButton = Place<SkillButtonWidget>(skillButtonPrefab, root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-192f, 52f));
            ((RectTransform)ultimateButton.transform).sizeDelta = new Vector2(112f, 112f);
            ultimateButton.SetText("궁극기");
            ultimateButton.transform.Find("Icon").GetComponent<Image>().sprite = Art("Icons/Ultimate");
            var speed = Place<CircleButtonWidget>(circleButtonPrefab, root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-58f, 196f));
            ((RectTransform)speed.transform).sizeDelta = new Vector2(82f, 82f);
            speed.SetText("1.0×");
            BuildBottomLeftDecorations(root);

            // 장비창 — 기본은 닫힘. 장비 메뉴 버튼으로 연다.
            var equipmentWindow = Place<EquipmentWindow>(equipmentWindowPrefab, root,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f));
            equipmentWindow.gameObject.SetActive(false);

            // 부활 텍스트
            var reviveText = Text(
                Rect(root, "ReviveText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(500f, 50f)),
                string.Empty, 36f, TextWhite);

            var panel = panelRoot.GetComponent<BattlePhaseUIPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_reviveText").objectReferenceValue = reviveText;
            so.FindProperty("_joystick").objectReferenceValue = joystick;
            so.FindProperty("_heroStatus").objectReferenceValue = heroStatus;
            so.FindProperty("_goldWidget").objectReferenceValue = gold;
            so.FindProperty("_gemWidget").objectReferenceValue = gem;
            so.FindProperty("_stageWidget").objectReferenceValue = stage;
            so.FindProperty("_guideWidget").objectReferenceValue = guide;
            so.FindProperty("_skillButton").objectReferenceValue = skillButton;
            so.FindProperty("_ultimateButton").objectReferenceValue = ultimateButton;
            so.FindProperty("_equipMenuButton").objectReferenceValue = equipMenuButton;
            so.FindProperty("_equipmentWindow").objectReferenceValue = equipmentWindow;
            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (var component in panelRoot.GetComponentsInChildren<Component>(true))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(component))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(panelRoot, PanelPath);
            PrefabUtility.UnloadPrefabContents(panelRoot);
        }

        private static FloatingJoystick BuildJoystick(RectTransform root)
        {
            var touchArea = Rect(root, "JoystickTouchArea", Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            touchArea.anchorMin = Vector2.zero;
            touchArea.anchorMax = Vector2.one;
            var touchImage = Img(touchArea, new Color(1f, 1f, 1f, 0f));
            touchImage.raycastTarget = true;

            var joystick = touchArea.gameObject.AddComponent<FloatingJoystick>();

            var baseRT = Rect(touchArea, "JoystickBase", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 240f));
            Img(baseRT, new Color(1f, 1f, 1f, 0.35f), _circle);
            var knobRT = Rect(baseRT, "JoystickKnob", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
            Img(knobRT, new Color(1f, 1f, 1f, 0.8f), _circle);
            baseRT.gameObject.SetActive(false);

            var so = new SerializedObject(joystick);
            so.FindProperty("_base").objectReferenceValue = baseRT;
            so.FindProperty("_knob").objectReferenceValue = knobRT;
            so.ApplyModifiedPropertiesWithoutUndo();

            return joystick;
        }

        private static void BuildBottomLeftDecorations(RectTransform root)
        {
            var nickname = Rect(root, "NicknameInput", Vector2.zero, Vector2.zero, new Vector2(24f, 24f), new Vector2(330f, 54f));
            Img(nickname, Color.white, _panel);
            Icon(nickname, "Chat", "Icons/Chat", new Vector2(0f, 0.5f), new Vector2(29f, 0f), new Vector2(30f, 30f));
            Text(Rect(nickname, "Placeholder", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(260f, 32f)),
                "닉네임을 설정하세요", 20f, TextGray, HorizontalAlignmentOptions.Left);
            var pet = Rect(root, "PetIcon", Vector2.zero, Vector2.zero, new Vector2(24f, 304f), new Vector2(68f, 68f));
            Img(pet, Color.white, _medallion);
            Icon(pet, "Guide", "Icons/Skull", Vector2.one * 0.5f, Vector2.zero, new Vector2(42f, 42f));
            Text(Rect(pet, "PetLabel", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(90f, 24f)), "길라잡이", 16f, TextWhite);
            var icons = new[] { "Shield", "Hourglass", "Chat" };
            for (var i = 0; i < icons.Length; i++)
            {
                var item = Rect(root, $"SideButton_{i}", Vector2.zero, Vector2.zero, new Vector2(24f, 214f - i * 62f), new Vector2(50f, 50f));
                Img(item, Color.white, _panel);
                Icon(item, "Icon", $"Icons/{icons[i]}", Vector2.one * 0.5f, Vector2.zero, new Vector2(30f, 30f));
            }
        }

        private static Sprite Art(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BattleHUDArtBuilder.ArtFolder}/{name}.png");
            if (sprite == null) throw new System.InvalidOperationException($"HUD sprite missing: {name}");
            return sprite;
        }

        private static RectTransform Stretch(RectTransform parent, string name, float inset)
        {
            var rt = Rect(parent, name, Vector2.zero, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.one * inset;
            rt.offsetMax = Vector2.one * -inset;
            return rt;
        }

        private static Image Icon(RectTransform parent, string name, string asset, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var image = Img(Rect(parent, name, anchor, Vector2.one * 0.5f, pos, size), Color.white, Art(asset));
            image.preserveAspect = true;
            return image;
        }

        private static void SetMenuIcon(IconMenuWidget menu, string iconName, bool isLocked)
        {
            var icon = menu.transform.Find("Icon").GetComponent<Image>();
            icon.sprite = Art(iconName == "HeroPortrait" ? "Portraits/HeroPortrait" : $"Icons/{iconName}");
            if (!isLocked) return;
            icon.color = new Color(0.64f, 0.68f, 0.80f, 0.8f);
            Icon((RectTransform)menu.transform, "Lock", "Icons/Lock", Vector2.one, new Vector2(-12f, -12f), new Vector2(22f, 22f));
        }

        private static void StyleButton(Button btn)
        {
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.85f, 0.94f, 1f);
            colors.pressedColor = new Color(0.58f, 0.72f, 0.86f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        // ---- 공용 헬퍼 ----

        private static RectTransform WidgetRoot(string name, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.pivot = pivot ?? new Vector2(0f, 1f);
            return rt;
        }

        private static GameObject SaveWidget(GameObject temp)
        {
            var path = $"{WidgetFolder}/{temp.name}.prefab";
            foreach (var component in temp.GetComponentsInChildren<Component>(true))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        private static T Place<T>(GameObject prefab, RectTransform parent, Vector2 anchor, Vector2 pivot, Vector2 pos)
            where T : Component
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            var rt = (RectTransform)instance.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            return instance.GetComponent<T>();
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

        private static Image Img(RectTransform rt, Color color, Sprite sprite = null)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = sprite == _rounded || sprite == _panel ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI Text(RectTransform rt, string text, float size, Color color,
            HorizontalAlignmentOptions horizontal = HorizontalAlignmentOptions.Center)
        {
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = _font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.horizontalAlignment = horizontal;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>배경 + 채움 + (선택) 라벨로 이루어진 게이지 바.</summary>
        /// <remarks>
        /// 채움은 Image.Type.Filled 가 아니라 9슬라이스 이미지의 폭을 줄이는 방식이다.
        /// Filled 는 스프라이트를 통째로 늘린 뒤 잘라내서 양끝 모양이 뭉개진다.
        /// 여백은 FillArea 가 가지고 Fill 은 그 안을 0~1 로 채운다.
        /// </remarks>
        private static (SlicedFillBar fill, TextMeshProUGUI label) Bar(RectTransform parent, string name, Vector2 pos, Vector2 size,
            Color fillColor, bool withLabel)
        {
            var bg = Rect(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);
            Img(bg, Color.white, _panel);

            var area = Stretch(bg, "FillArea", 3f);

            var fillRT = Rect(area, "Fill", Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            var image = Img(fillRT, fillColor, _gauge);
            image.type = Image.Type.Sliced;
            var fill = fillRT.gameObject.AddComponent<SlicedFillBar>();
            fill.FillAmount = 1f;

            TextMeshProUGUI label = null;
            if (withLabel)
            {
                label = Text(Rect(bg, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size),
                    string.Empty, size.y * 0.6f, TextWhite);
            }

            return (fill, label);
        }
    }
}
