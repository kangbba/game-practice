using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne.Editor
{
    /// <summary>
    /// 전투 HUD 를 만든다. 데이터를 가지는 UI 는 전용 위젯 프리팹으로 먼저 만들고,
    /// BattlePhaseUIPanel 프리팹은 위젯들을 중첩 프리팹으로 배치만 한다.
    /// 전부 표시용이라 레이캐스트를 꺼서 플로팅 조이스틱 입력을 막지 않는다.
    /// </summary>
    public static class BattlePhaseUIBuilder
    {
        private const string PanelPath = "Assets/Game/UI/BattlePhaseUIPanel.prefab";
        private const string WidgetFolder = "Assets/Game/UI/Widgets";
        private const string FontPath = "Assets/Fonts/TMP/GothicA1-Regular SDF.asset";

        private static readonly Color PanelDark = new Color(0f, 0f, 0.05f, 0.55f);
        private static readonly Color PanelDarker = new Color(0f, 0f, 0.05f, 0.75f);
        private static readonly Color TextWhite = new Color(0.95f, 0.95f, 0.97f);
        private static readonly Color TextGray = new Color(0.65f, 0.65f, 0.7f);
        private static readonly Color Gold = new Color(1f, 0.8f, 0.25f);
        private static readonly Color GemBlue = new Color(0.45f, 0.75f, 1f);
        private static readonly Color HPRed = new Color(0.85f, 0.2f, 0.2f);
        private static readonly Color EXPGreen = new Color(0.4f, 0.8f, 0.3f);
        private static readonly Color StageBlue = new Color(0.3f, 0.75f, 1f);
        private static readonly Color GuideGreen = new Color(0.45f, 0.8f, 0.3f);
        private static readonly Color SkillRed = new Color(0.35f, 0.05f, 0.08f, 0.9f);
        private static readonly Color SpeedNavy = new Color(0.1f, 0.15f, 0.35f, 0.9f);

        private static TMP_FontAsset _font;
        private static Sprite _rounded;
        private static Sprite _circle;

        [MenuItem("★Sayne★/전투 HUD 빌드")]
        public static void Build()
        {
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

            Debug.Log("BattlePhaseUIBuilder: 위젯 프리팹 10종 + 전투 HUD 패널 재구성 완료");
        }

        // ---- 위젯 프리팹 ----

        private static GameObject BuildHeroStatusWidget()
        {
            var root = WidgetRoot("HeroStatusWidget", new Vector2(440f, 120f));

            var portrait = Rect(root, "Portrait", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(110f, 110f));
            Img(portrait, new Color(0.25f, 0.22f, 0.3f, 0.9f), _circle);
            var levelText = Text(
                Rect(portrait, "LevelText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(110f, 28f)),
                "Lv.0", 22f, TextWhite);

            var nameText = Text(
                Rect(root, "NameText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(124f, -4f), new Vector2(280f, 34f)),
                "이름없음", 26f, TextWhite, HorizontalAlignmentOptions.Left);

            var (hpFill, hpLabel) = Bar(root, "HPBar", new Vector2(124f, -44f), new Vector2(300f, 30f), HPRed, true);
            var (expFill, _) = Bar(root, "EXPBar", new Vector2(124f, -80f), new Vector2(300f, 16f), EXPGreen, false);

            var widget = root.gameObject.AddComponent<HeroStatusWidget>();
            var so = new SerializedObject(widget);
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
            var root = WidgetRoot("CurrencyWidget", new Vector2(200f, 42f));
            Img(root, PanelDarker, _rounded);

            var amountText = Text(
                Rect(root, "AmountText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 42f)),
                "0", 24f, TextWhite);

            var widget = root.gameObject.AddComponent<CurrencyWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_amountText").objectReferenceValue = amountText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildGuideWidget()
        {
            var root = WidgetRoot("GuideWidget", new Vector2(380f, 130f));
            Img(root, new Color(0.2f, 0.15f, 0.3f, 0.7f), _rounded);

            var titleText = Text(
                Rect(root, "TitleText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -6f), new Vector2(200f, 28f)),
                "가이드", 20f, TextGray, HorizontalAlignmentOptions.Left);

            var reward = Rect(root, "RewardIcon", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 12f), new Vector2(80f, 80f));
            Img(reward, PanelDarker, _rounded);
            var rewardText = Text(
                Rect(reward, "RewardText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(80f, 26f)),
                "0", 20f, GemBlue);

            var descText = Text(
                Rect(root, "DescText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(106f, -38f), new Vector2(264f, 30f)),
                "-", 22f, TextWhite, HorizontalAlignmentOptions.Left);

            var (progressFill, progressLabel) = Bar(root, "ProgressBar", new Vector2(106f, -76f), new Vector2(220f, 28f), GuideGreen, true);

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
            var root = WidgetRoot("StageWidget", new Vector2(480f, 140f), new Vector2(0.5f, 1f));

            var stageText = Text(
                Rect(root, "StageText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(300f, 44f)),
                "1-1 단계", 36f, StageBlue);

            var (progressFill, _) = Bar(root, "ProgressBar", new Vector2(0f, -52f), new Vector2(480f, 28f), StageBlue, false);

            var killPill = Rect(root, "KillPill", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(180f, 40f));
            Img(killPill, PanelDarker, _rounded);
            var killLabel = Text(
                Rect(killPill, "KillLabel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 40f)),
                "0/0", 24f, TextWhite);

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
            var root = WidgetRoot("IconMenuWidget", new Vector2(110f, 132f), new Vector2(0.5f, 1f));

            var icon = Rect(root, "Icon", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(94f, 94f));
            Img(icon, PanelDark, _rounded);

            var label = Text(
                Rect(root, "Label", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(110f, 30f)),
                "-", 22f, TextWhite);

            var widget = root.gameObject.AddComponent<IconMenuWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildCircleButtonWidget()
        {
            var root = WidgetRoot("CircleButtonWidget", new Vector2(150f, 150f), new Vector2(0.5f, 0.5f));
            var background = Img(root, PanelDarker, _circle);

            var label = Text(
                Rect(root, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 40f)),
                "-", 26f, TextWhite);

            var widget = root.gameObject.AddComponent<CircleButtonWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_background").objectReferenceValue = background;
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildSkillButtonWidget()
        {
            var root = WidgetRoot("SkillButtonWidget", new Vector2(150f, 150f), new Vector2(0.5f, 0.5f));
            var background = Img(root, SkillRed, _circle);
            background.raycastTarget = true;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            var label = Text(
                Rect(root, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 40f)),
                "-", 26f, TextWhite);

            var widget = root.gameObject.AddComponent<SkillButtonWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildEquipmentSlotWidget()
        {
            var root = WidgetRoot("EquipmentSlotWidget", new Vector2(100f, 140f), new Vector2(0.5f, 0.5f));
            var bg = Img(root, PanelDarker, _rounded);
            bg.raycastTarget = true;

            var slotName = Text(
                Rect(root, "SlotName", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(140f, 24f)),
                "-", 18f, TextGray);

            var iconRT = Rect(root, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(64f, 64f));
            var icon = Img(iconRT, Color.white);
            icon.enabled = false;

            var itemName = Text(
                Rect(root, "ItemName", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(140f, 24f)),
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
            var bg = Img(root, PanelDark, _rounded);
            bg.raycastTarget = true;
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

            var iconRT = Rect(root, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(56f, 56f));
            var icon = Img(iconRT, Color.white);
            icon.enabled = false;

            var label = Text(
                Rect(root, "Label", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(100f, 22f)),
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
            var bg = Img(root, new Color(0.05f, 0.05f, 0.1f, 0.95f), _rounded);
            bg.raycastTarget = true;

            Text(Rect(root, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -10f), new Vector2(200f, 40f)),
                "장비", 30f, TextWhite, HorizontalAlignmentOptions.Left);
            var closeButton = MakeButton(root, "CloseButton", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-12f, -12f), new Vector2(52f, 52f), "X");

            // 인체 배치: 위 투구, 중앙 몸통, 좌/우 팔. 크기는 부위 비례. 기능은 오른팔(무기)만, 나머지는 자리.
            var helmet = PlaceSlot(slotPrefab, root, new Vector2(190f, -70f), new Vector2(90f, 90f), "투구");
            var body = PlaceSlot(slotPrefab, root, new Vector2(190f, -175f), new Vector2(150f, 180f), "몸통");
            var leftArm = PlaceSlot(slotPrefab, root, new Vector2(78f, -165f), new Vector2(80f, 150f), "왼팔");
            var rightArm = PlaceSlot(slotPrefab, root, new Vector2(302f, -165f), new Vector2(80f, 150f), "오른팔");

            Text(Rect(root, "CandidatesLabel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420f, -64f), new Vector2(200f, 30f)),
                "후보", 22f, TextGray, HorizontalAlignmentOptions.Left);

            var candidatesRoot = Rect(root, "Candidates", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(420f, -100f), new Vector2(380f, 360f));
            var grid = candidatesRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(100f, 100f);
            grid.spacing = new Vector2(12f, 12f);

            var applyButton = MakeButton(root, "ApplyButton", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 24f), new Vector2(170f, 64f), "적용");

            var window = root.gameObject.AddComponent<EquipmentWindow>();
            var so = new SerializedObject(window);
            so.FindProperty("_weaponSlot").objectReferenceValue = rightArm;
            so.FindProperty("_candidatesRoot").objectReferenceValue = candidatesRoot;
            so.FindProperty("_candidatePrefab").objectReferenceValue = candidatePrefab.GetComponent<EquipmentCandidateWidget>();
            so.FindProperty("_applyButton").objectReferenceValue = applyButton;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static EquipmentSlotWidget PlaceSlot(GameObject slotPrefab, RectTransform parent, Vector2 pos, Vector2 size,
            string slotName)
        {
            var slot = Place<EquipmentSlotWidget>(slotPrefab, parent, new Vector2(0f, 1f), new Vector2(0.5f, 1f), pos);
            ((RectTransform)slot.transform).sizeDelta = size;
            slot.SetSlotName(slotName);
            slot.SetItem("없음", null);
            return slot;
        }

        private static Button MakeButton(RectTransform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos,
            Vector2 size, string label)
        {
            var rt = Rect(parent, name, anchor, pivot, pos, size);
            var image = Img(rt, PanelDark, _rounded);
            image.raycastTarget = true;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text(Rect(rt, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size),
                label, 24f, TextWhite);

            return button;
        }

        // ---- 패널 조립 ----

        private static void ComposePanel(GameObject heroStatusPrefab, GameObject currencyPrefab, GameObject guidePrefab,
            GameObject stagePrefab, GameObject iconMenuPrefab, GameObject circleButtonPrefab, GameObject skillButtonPrefab,
            GameObject equipmentWindowPrefab)
        {
            var panelRoot = PrefabUtility.LoadPrefabContents(PanelPath);

            for (var i = panelRoot.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(panelRoot.transform.GetChild(i).gameObject);
            }

            var root = (RectTransform)panelRoot.transform;

            var joystick = BuildJoystick(root);

            // 좌상단
            var heroStatus = Place<HeroStatusWidget>(heroStatusPrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f));
            heroStatus.SetName("이름없음");
            heroStatus.SetLevel(0);
            heroStatus.SetHP(5000, 5000);
            heroStatus.SetEXPRatio(0.05f);

            var gold = Place<CurrencyWidget>(currencyPrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -152f));
            gold.SetAmount(1004);
            gold.SetColor(Gold);

            var gem = Place<CurrencyWidget>(currencyPrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(234f, -152f));
            gem.SetAmount(500);
            gem.SetColor(GemBlue);

            var guide = Place<GuideWidget>(guidePrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -212f));
            guide.SetGuide("가이드 3", "훈련 - 공격력 올려보기");
            guide.SetReward(500);
            guide.SetProgress(29, 3);

            // 중앙 상단
            var stage = Place<StageWidget>(stagePrefab, root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f));
            stage.SetStage("1-1 단계");
            stage.SetKills(25, 25);

            // 우상단 메뉴
            var topMenus = new[] { "더보기", "이벤트", "던전", "소환", "상점" };
            for (var i = 0; i < topMenus.Length; i++)
            {
                var menu = Place<IconMenuWidget>(iconMenuPrefab, root, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-24f - i * 118f, -24f));
                menu.SetLabel(topMenus[i]);
            }

            // 하단 바
            var bottomBar = Rect(root, "BottomBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            bottomBar.anchorMin = new Vector2(0f, 0f);
            bottomBar.anchorMax = new Vector2(1f, 0f);
            bottomBar.sizeDelta = new Vector2(0f, 150f);
            Img(bottomBar, new Color(0f, 0f, 0.05f, 0.35f));

            var bottomMenus = new[] { ("성장", -520f), ("장비", -260f), ("편성", 260f), ("심연 장비", 520f) };
            Button equipMenuButton = null;
            foreach (var (label, offsetX) in bottomMenus)
            {
                var menu = Place<IconMenuWidget>(iconMenuPrefab, bottomBar, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f),
                    new Vector2(offsetX, 70f));
                menu.SetLabel(label);

                if (label == "장비")
                {
                    var icon = menu.GetComponentInChildren<Image>();
                    icon.raycastTarget = true;
                    equipMenuButton = menu.gameObject.AddComponent<Button>();
                    equipMenuButton.targetGraphic = icon;
                }
            }

            var centerSlot = Place<CircleButtonWidget>(circleButtonPrefab, bottomBar, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 8f));
            ((RectTransform)centerSlot.transform).sizeDelta = new Vector2(130f, 130f);
            centerSlot.SetText("0");

            // 우하단 — 공격·궁극기는 실제 버튼, 배속은 자리만.
            var attackButton = Place<SkillButtonWidget>(skillButtonPrefab, root, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-40f, 170f));
            attackButton.SetText("공격");

            var ultimateButton = Place<SkillButtonWidget>(skillButtonPrefab, root, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-60f, 340f));
            ((RectTransform)ultimateButton.transform).sizeDelta = new Vector2(112f, 112f);
            ultimateButton.SetText("궁극기");
            ultimateButton.GetComponent<Image>().color = new Color(0.3f, 0.1f, 0.4f, 0.9f);

            var speed = Place<CircleButtonWidget>(circleButtonPrefab, root, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-70f, 474f));
            ((RectTransform)speed.transform).sizeDelta = new Vector2(90f, 90f);
            speed.SetText("1.0");
            speed.SetBackgroundColor(SpeedNavy);

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
            so.FindProperty("_attackButton").objectReferenceValue = attackButton;
            so.FindProperty("_ultimateButton").objectReferenceValue = ultimateButton;
            so.FindProperty("_equipMenuButton").objectReferenceValue = equipMenuButton;
            so.FindProperty("_equipmentWindow").objectReferenceValue = equipmentWindow;
            so.ApplyModifiedPropertiesWithoutUndo();

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
            var nickname = Rect(root, "NicknameInput", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 36f), new Vector2(430f, 66f));
            Img(nickname, PanelDarker, _rounded);
            Text(Rect(nickname, "Placeholder", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(390f, 34f)),
                "닉네임을 설정 하세요", 24f, TextGray, HorizontalAlignmentOptions.Left);

            var pet = Rect(root, "PetIcon", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 440f), new Vector2(88f, 88f));
            Img(pet, PanelDark, _circle);
            Text(Rect(pet, "PetLabel", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(120f, 28f)),
                "길라잡이", 20f, TextWhite);

            for (var i = 0; i < 3; i++)
            {
                var button = Rect(root, $"SideButton_{i}", new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(24f, 350f - i * 84f), new Vector2(64f, 64f));
                Img(button, PanelDark, _rounded);
            }
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
            image.type = sprite == _rounded ? Image.Type.Sliced : Image.Type.Simple;
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
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>배경 + 채움 + (선택) 라벨로 이루어진 게이지 바.</summary>
        private static (Image fill, TextMeshProUGUI label) Bar(RectTransform parent, string name, Vector2 pos, Vector2 size,
            Color fillColor, bool withLabel)
        {
            var bg = Rect(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);
            Img(bg, PanelDarker, _rounded);

            var fillRT = Rect(bg, "Fill", new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(3f, 3f);
            fillRT.offsetMax = new Vector2(-3f, -3f);
            var fill = Img(fillRT, fillColor, _rounded);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;

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
