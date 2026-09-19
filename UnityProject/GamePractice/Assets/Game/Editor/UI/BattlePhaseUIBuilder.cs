using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne.Editor
{
    /// <summary>
    /// 전투 HUD 를 만든다. 데이터를 가지는 UI 는 전용 위젯 프리팹으로 먼저 만들고,
    /// BattlePanel 프리팹은 위젯들을 중첩 프리팹으로 배치만 한다.
    /// 장식은 레이캐스트를 끄고 실제 버튼만 입력을 받는다.
    /// </summary>
    public static class BattlePhaseUIBuilder
    {
        private const string PanelPath = "Assets/Game/UI/BattlePanel.prefab";
        private const string WaveStartPanelPath = "Assets/Game/ScreenPerformance/WaveStartPanel.prefab";
        private const string LowHealthPanelPath = "Assets/Game/ScreenPerformance/LowHealthPanel.prefab";
        private const string WidgetFolder = "Assets/Game/UI/Widgets";
        private const string PopupFolder = "Assets/Game/UI/Popup";

        /// <summary>장비창과 그 부품들은 장비 기능 폴더 안에 산다. 장비를 들어내면 창도 같이 따라 나간다.</summary>
        private const string EquipmentUIFolder = "Assets/Game/Equipment/UI";

        private const string PopupBlurMaterialPath = PopupFolder + "/PopupBlur.mat";
        /// <summary>정산 화면에 늘어놓을 전리품 칸 수. 넘치면 "+N" 으로 접힌다.</summary>

        /// <summary>가방 한 칸. 6열 × 4행 = EquipmentWindow.BagCapacity 칸이 564×352 안에 딱 들어가는 크기다.</summary>
        private static readonly Vector2 BagCellSize = new Vector2(86f, 80f);
        private const int BagColumns = 6;

        /// <summary>성장창 항목 한 줄.</summary>
        private static readonly Vector2 GrowthRowSize = new Vector2(560f, 92f);

        /// <summary>편성창 파티 칸 하나. 넷이 20 간격으로 1200 폭 모달에 좌우 42 여백을 두고 들어간다.</summary>
        private static readonly Vector2 PartySlotSize = new Vector2(264f, 300f);

        /// <summary>편성창 영웅 명단 카드 한 장. 셋이 파티 칸 줄과 같은 폭(1116)을 채운다.</summary>
        private static readonly Vector2 FormationHeroSize = new Vector2(358f, 150f);

        /// <summary>영웅 프로필(이름·초상화·고유색). 히어로 폴더에 에셋 이름 = 히어로 ID 로 있다.</summary>
        private const string HeroesFolder = "Assets/Game/Characters/Heroes";

        private const string FontPath = "Assets/Fonts/TMP/SB_Aggro_Bold SDF.asset";

        /// <summary>장비창과 그 안의 위젯(슬롯·후보·스탯 줄)은 이 폰트를 쓴다.</summary>
        private const string EquipmentFontPath = "Assets/Game/Loading/Fonts/NanumGothicBold SDF.asset";

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

        /// <summary>궁극기 버튼 테두리 — 불붙은 금빛. 스킬 버튼과 한눈에 갈려야 한다.</summary>
        private static readonly Color UltimateRim = new Color(1f, 0.62f, 0.18f);

        /// <summary>스킬 버튼 테두리 — 차가운 하늘색.</summary>
        private static readonly Color SkillRim = new Color(0.45f, 0.78f, 1f);

        /// <summary>가젯 버튼 아이콘을 테두리에서 들이는 거리. 스킬 버튼(25)보다 좁다.</summary>
        private const float GadgetIconInset = 12f;

        /// <summary>가젯 버튼 테두리 — 옅은 청록. 셋 중 제일 가벼운 기술이라 제일 작고 제일 차분하다.</summary>
        private static readonly Color GadgetRim = new Color(0.5f, 0.92f, 0.76f);
        /// <summary>흐린 화면 위의 암막. 흐림이 이미 뒤를 눌러 주니 옅게만 깐다.</summary>
        private static readonly Color PopupDim = new Color(0f, 0f, 0.05f, 0.35f);

        /// <summary>흐린 화면 자체의 톤. 살짝 어둡고 푸르게.</summary>
        private static readonly Color PopupBlurTint = new Color(0.78f, 0.8f, 0.88f, 1f);

        private static TMP_FontAsset _font;
        private static Sprite _rounded;
        private static Sprite _circle;
        private static Sprite _panel;
        private static Sprite _medallion;
        private static Sprite _ring;
        private static Sprite _gauge;
        private static Sprite _glow;

        public static void Build()
        {
            BattleHUDArtBuilder.Build();
            LoadArt();

            if (!AssetDatabase.IsValidFolder(WidgetFolder))
            {
                AssetDatabase.CreateFolder("Assets/Game/UI", "Widgets");
            }

            if (!AssetDatabase.IsValidFolder(EquipmentUIFolder))
            {
                AssetDatabase.CreateFolder("Assets/Game/Equipment", "UI");
            }

            var heroProfilePrefab = BuildHeroProfileWidget();
            var currencyPrefab = BuildCurrencyWidget();
            var questPrefab = BuildQuestWidget();
            var stagePrefab = BuildStageWidget();
            var iconMenuPrefab = BuildIconMenuWidget();
            var circleButtonPrefab = BuildCircleButtonWidget();
            var skillButtonPrefab = BuildSkillButtonWidget();

            // 팝업은 HUD 에 들지 않는 독립 프리팹이다. PopupManager 가 종류(PopupType)에 짝지어 만든다.
            BuildEquipment();
            BuildGrowthWindow(BuildGrowthStatWidget());
            BuildFormationWindow(BuildFormationHeroWidget());

            ComposePanel(heroProfilePrefab, currencyPrefab, questPrefab, stagePrefab, iconMenuPrefab, circleButtonPrefab,
                skillButtonPrefab);

            ComposeWaveStartPanel();
            ComposeLowHealthPanel();

            Debug.Log("BattlePhaseUIBuilder: 위젯 프리팹 13종 + 전투 HUD·웨이브 시작·저체력 패널 재구성 완료");
        }

        /// <summary>장비창 묶음(위젯 셋 + 창)만 다시 만든다. 장비창만 손볼 때 HUD 전체를 다시 굽지 않으려고 따로 연다.</summary>
        public static void BuildEquipmentOnly()
        {
            LoadArt();
            BuildEquipment();
            Debug.Log("BattlePhaseUIBuilder: 장비창·장비 위젯 3종 재구성 완료");
        }

        private static void LoadArt()
        {
            _panel = Art("Frames/Panel");
            _medallion = Art("Frames/Medallion");
            _ring = Art("Frames/Ring");
            _gauge = Art("Frames/Gauge");
            _glow = Art("Frames/Glow");
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            _rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            _circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        }

        /// <summary>장비창 묶음만 폰트가 다르다. 그동안만 바꿔 끼우고 끝나면 되돌린다.</summary>
        private static void BuildEquipment()
        {
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EquipmentFontPath);
            var slotPrefab = BuildEquipmentSlotWidget();
            var candidatePrefab = BuildEquipmentCandidateWidget();
            var statRowPrefab = BuildEquipmentStatRowWidget();
            BuildEquipmentWindow(slotPrefab, candidatePrefab, statRowPrefab);
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        }

        // ---- 위젯 프리팹 ----

        private static GameObject BuildHeroProfileWidget()
        {
            var root = WidgetRoot("HeroProfileWidget", new Vector2(420f, 124f));
            Img(root, Color.white, _panel);
            var portrait = Rect(root, "Portrait", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(108f, 108f));
            Img(portrait, Color.white, _medallion);
            var clip = Rect(portrait, "PortraitMask", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(94f, 94f));
            Img(clip, Color.white, _circle);
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var portraitIcon = Icon(clip, "HeroPortrait", "Portraits/HeroPortrait", Vector2.one * 0.5f, new Vector2(0f, 20f), new Vector2(100f, 100f));
            var levelPlate = Rect(portrait, "LevelPlate", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -1f), new Vector2(76f, 26f));
            Img(levelPlate, Color.white, _panel);
            var levelText = Text(Stretch(levelPlate, "LevelText", 2f), "Lv.0", 18f, Gold);
            var nameText = Text(
                Rect(root, "NameText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(132f, -10f), new Vector2(260f, 30f)),
                "이름없음", 25f, TextWhite, HorizontalAlignmentOptions.Left);
            var (hpFill, hpLabel) = Bar(root, "HPBar", new Vector2(132f, -48f), new Vector2(270f, 30f), HPRed, true);
            var (expFill, _) = Bar(root, "EXPBar", new Vector2(166f, -92f), new Vector2(236f, 10f), StageBlue, false);
            Text(Rect(root, "EXPLabel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(132f, -86f), new Vector2(32f, 22f)), "EXP", 13f, Gold);

            var widget = root.gameObject.AddComponent<HeroProfileWidget>();
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

        /// <remarks>
        /// 폭은 위의 프로필·재화 줄과 같은 420 이다. 번호는 태그로 떼어 제목이 제 칸을 온전히 쓴다.
        /// 후광은 패널보다 먼저 그려야 뒤에 깔리므로 루트가 아니라 Frame 이 패널을 든다.
        /// </remarks>
        private static GameObject BuildQuestWidget()
        {
            const float columnX = 116f;
            const float columnWidth = 292f;

            var root = WidgetRoot("QuestWidget", new Vector2(420f, 120f));

            // 받을 때 박스가 가운데를 축으로 튕기도록 몸통을 가운데 피벗으로 한 겹 둔다. 루트 피벗은 배치하는 쪽 몫이다.
            var body = Stretch(root, "Body", 0f);

            var glow = Stretch(body, "ClaimGlow", -20f);
            var glowImage = Img(glow, Gold, _glow);
            glowImage.type = Image.Type.Sliced;
            glow.gameObject.SetActive(false);

            Img(Stretch(body, "Frame", 0f), Color.white, _panel);

            var content = Stretch(body, "Content", 0f);
            var contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            contentGroup.blocksRaycasts = false;

            var reward = Rect(content, "Reward", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(92f, 96f));
            Img(reward, Color.white, _panel);
            var rewardIcon = Icon(reward, "Coin", "Icons/Coin", new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(48f, 48f));
            var rewardText = FitText(Text(
                Rect(reward, "RewardText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(84f, 28f)),
                "0", 21f, Gold), 14f);

            var tag = Rect(content, "Tag", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(columnX, -12f), new Vector2(84f, 26f));
            Img(tag, Gold, _gauge).type = Image.Type.Sliced;
            var tagText = FitText(Text(Stretch(tag, "TagText", 2f), "퀘스트 1", 15f, PanelDarker), 11f);

            var titleText = FitText(Text(
                Rect(content, "TitleText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(columnX + 94f, -12f), new Vector2(columnWidth - 94f, 26f)),
                "-", 21f, TextWhite, HorizontalAlignmentOptions.Left), 15f);

            var descText = FitText(Text(
                Rect(content, "DescText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(columnX, -46f), new Vector2(columnWidth, 26f)),
                "-", 18f, new Color(0.78f, 0.81f, 0.88f), HorizontalAlignmentOptions.Left), 13f);

            var (progressFill, progressLabel) = Bar(content, "ProgressBar", new Vector2(columnX, -82f), new Vector2(columnWidth, 26f), GuideGreen, true);
            progressLabel.fontSize = 18f;
            progressLabel.outlineWidth = 0.25f;
            progressLabel.outlineColor = new Color32(8, 14, 26, 255);

            // 받기 덮개. 내용을 가리지 않고 진행바 자리에 금빛 버튼만 올린다 — 무엇을 끝냈는지가 그대로 보여야 한다.
            // 버튼은 꽉 찬 금색 게이지 꼴이고 현재/목표 수치를 그대로 들고 있다.
            // 누르는 건 박스 전체다. 덮개의 투명한 판이 버튼을 받는다.
            var overlay = Stretch(body, "ClaimOverlay", 0f);
            var overlayImage = Img(overlay, Color.clear);
            overlayImage.raycastTarget = true;
            var claimBtn = overlay.gameObject.AddComponent<Button>();
            claimBtn.targetGraphic = overlayImage;
            claimBtn.transition = Selectable.Transition.None;
            claimBtn.navigation = new Navigation { mode = Navigation.Mode.None };

            var pill = Rect(overlay, "ClaimPill", new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(columnX + columnWidth * 0.5f, -95f), new Vector2(columnWidth + 4f, 34f));
            Img(pill, Gold, _gauge).type = Image.Type.Sliced;
            pill.gameObject.AddComponent<RectMask2D>();
            var shine = Rect(pill, "Shine", new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-40f, 0f), new Vector2(34f, 70f));
            shine.localRotation = Quaternion.Euler(0f, 0f, -24f);
            Img(shine, new Color(1f, 1f, 1f, 0.55f));
            var claimText = FitText(Text(Stretch(pill, "ClaimText", 2f), "0/0  보상 받기", 18f, PanelDarker), 13f);

            overlay.gameObject.SetActive(false);

            // 받는 순간의 연출 부품. 평소엔 보이지 않는다.
            var flash = Stretch(body, "Flash", -20f);
            var flashImage = Img(flash, new Color(1f, 0.97f, 0.86f, 0f), _glow);
            flashImage.type = Image.Type.Sliced;

            var burst = Rect(body, "Burst", new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(58f, 12f), Vector2.zero);
            var coins = new Image[QuestWidget.BurstCoinCount];
            for (var i = 0; i < coins.Length; i++)
            {
                coins[i] = Icon(burst, $"Coin{i}", "Icons/Coin", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
                coins[i].gameObject.SetActive(false);
            }
            var gainText = Text(Rect(burst, "GainText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 40f)),
                "+0", 30f, Gold);
            gainText.outlineWidth = 0.22f;
            gainText.outlineColor = new Color32(40, 24, 8, 255);
            gainText.gameObject.SetActive(false);

            var widget = root.gameObject.AddComponent<QuestWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_tagText").objectReferenceValue = tagText;
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_descText").objectReferenceValue = descText;
            so.FindProperty("_rewardText").objectReferenceValue = rewardText;
            so.FindProperty("_progressFill").objectReferenceValue = progressFill;
            so.FindProperty("_progressLabel").objectReferenceValue = progressLabel;
            so.FindProperty("_claimOverlay").objectReferenceValue = overlay.gameObject;
            so.FindProperty("_claimBtn").objectReferenceValue = claimBtn;
            so.FindProperty("_claimGlow").objectReferenceValue = glowImage;
            so.FindProperty("_claimPill").objectReferenceValue = pill;
            so.FindProperty("_claimShine").objectReferenceValue = shine;
            so.FindProperty("_claimText").objectReferenceValue = claimText;
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_content").objectReferenceValue = contentGroup;
            so.FindProperty("_rewardIcon").objectReferenceValue = rewardIcon.rectTransform;
            so.FindProperty("_flash").objectReferenceValue = flashImage;
            so.FindProperty("_gainText").objectReferenceValue = gainText;
            var coinsProperty = so.FindProperty("_coins");
            coinsProperty.arraySize = coins.Length;
            for (var i = 0; i < coins.Length; i++)
            {
                coinsProperty.GetArrayElementAtIndex(i).objectReferenceValue = coins[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        private static GameObject BuildStageWidget()
        {
            var root = WidgetRoot("StageWidget", new Vector2(420f, 126f), new Vector2(0.5f, 1f));
            var titlePlate = Rect(root, "StagePlate", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(250f, 48f));
            Img(titlePlate, Color.white, _panel);
            var stageText = FitText(Text(Stretch(titlePlate, "StageText", 6f), "STAGE 1-1", 24f, Gold), 16f);
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
            // 기술 이름은 길어질 수 있다 — 버튼 폭만큼 넓게 두고 두 줄까지 접는다. 그래도 넘치면 글자가 줄어든다.
            // 두 줄이면 아이콘 아랫부분에 걸치므로 외곽선으로 읽히게 한다.
            var label = FitText(Text(
                Rect(root, "Label", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(136f, 46f)),
                "공격", 19f, TextWhite), 13f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.verticalAlignment = VerticalAlignmentOptions.Bottom;
            label.lineSpacing = -12f;
            label.outlineWidth = 0.22f;
            label.outlineColor = new Color32(8, 14, 26, 255);
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

        /// <summary>
        /// 가젯 버튼. 무게가 가젯 &lt; 스킬 &lt; 궁극기라 크기도 그 순서다 — 스킬 버튼 왼쪽에 제일 작게, 스킬과 같은 높이 가운데에 선다.
        /// </summary>
        private static SkillButtonWidget PlaceGadgetButton(GameObject skillButtonPrefab, RectTransform root)
        {
            var gadgetButton = Place<SkillButtonWidget>(skillButtonPrefab, root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-354f, 58f));
            ((RectTransform)gadgetButton.transform).sizeDelta = new Vector2(88f, 88f);
            gadgetButton.Preview("돌진", cooldownRatio: 0f, cooldownRemain: 0f);
            // 버튼이 작아서 스킬 버튼과 같은 여백(25)이면 아이콘이 콩알만 해진다. 여백을 줄여 버튼 안을 채운다.
            var icon = (RectTransform)gadgetButton.transform.Find("Icon");
            icon.offsetMin = Vector2.one * GadgetIconInset;
            icon.offsetMax = -Vector2.one * GadgetIconInset;
            icon.GetComponent<Image>().sprite = Art("Icons/Dash");
            SetSkillBorder(gadgetButton, GadgetRim);
            return gadgetButton;
        }

        /// <summary>스킬 버튼의 테두리(Rim) 색.</summary>
        private static void SetSkillBorder(SkillButtonWidget button, Color color)
        {
            button.transform.Find("Rim").GetComponent<Image>().color = color;
        }

        private static GameObject BuildEquipmentSlotWidget()
        {
            var root = WidgetRoot("EquipmentSlotWidget", new Vector2(180f, 136f), new Vector2(0.5f, 0.5f));
            var bg = Img(root, Color.white, _panel);
            bg.raycastTarget = true;

            var slotName = Text(
                Rect(root, "SlotName", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(172f, 22f)),
                "-", 14f, TextGray);

            var iconRT = Rect(root, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            iconRT.anchorMin = new Vector2(0.2f, 0.24f);
            iconRT.anchorMax = new Vector2(0.8f, 0.78f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
            var icon = Img(iconRT, Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;

            var itemName = Text(
                Rect(root, "ItemName", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(172f, 22f)),
                "없음", 15f, TextWhite);

            var widget = root.gameObject.AddComponent<EquipmentSlotWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_slotName").objectReferenceValue = slotName;
            so.FindProperty("_itemName").objectReferenceValue = itemName;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject, EquipmentUIFolder);
        }

        private static GameObject BuildEquipmentCandidateWidget()
        {
            var root = WidgetRoot("EquipmentCandidateWidget", BagCellSize, new Vector2(0.5f, 0.5f));
            var bg = Img(root, Color.white, _panel);
            bg.raycastTarget = true;
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

            var iconRT = Rect(root, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 7f), new Vector2(46f, 46f));
            var icon = Img(iconRT, Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;

            var label = Text(
                Rect(root, "Label", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f),
                    new Vector2(BagCellSize.x - 6f, 16f)),
                "-", 11f, TextWhite);

            // 장착중 표식. 기본은 꺼져 있고 창이 장비 상태를 보고 켠다.
            var equippedMark = Rect(root, "EquippedMark", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(4f, -4f), new Vector2(40f, 20f));
            Img(equippedMark, Gold, _rounded);
            Text(Stretch(equippedMark, "Text", 1f), "장착", 11f, new Color(0.08f, 0.09f, 0.14f));
            equippedMark.gameObject.SetActive(false);

            // 선택 테두리. 가운데를 비운 9슬라이스라 테두리만 그려진다.
            var selectedMark = Stretch(root, "SelectedMark", 0f);
            var selectedFrame = Img(selectedMark, Gold, _rounded);
            selectedFrame.fillCenter = false;
            selectedFrame.pixelsPerUnitMultiplier = 0.5f;
            selectedMark.gameObject.SetActive(false);

            var widget = root.gameObject.AddComponent<EquipmentCandidateWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("_equippedMark").objectReferenceValue = equippedMark.gameObject;
            so.FindProperty("_selectedMark").objectReferenceValue = selectedMark.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject, EquipmentUIFolder);
        }

        /// <summary>장비창 스탯 한 줄. 이름 · 값("지금 > 바뀔 값") · 증감 칩. 칩은 변화가 있을 때만 창이 켠다.</summary>
        private static GameObject BuildEquipmentStatRowWidget()
        {
            var root = WidgetRoot("EquipmentStatRowWidget", new Vector2(282f, 52f));
            Img(root, Color.white, _panel);

            var label = Text(
                Rect(root, "Label", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(84f, 28f)),
                "-", 17f, TextGray, HorizontalAlignmentOptions.Left);

            var value = Text(
                Rect(root, "Value", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(100f, 0f), new Vector2(110f, 28f)),
                "0", 19f, TextWhite, HorizontalAlignmentOptions.Left);
            value.overflowMode = TextOverflowModes.Overflow;

            var chipRT = Rect(root, "DeltaChip", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(62f, 30f));
            var chip = Img(chipRT, Color.white, _rounded);
            var deltaText = Text(Stretch(chipRT, "Text", 1f), "+0", 16f, new Color(0.05f, 0.07f, 0.10f));
            chipRT.gameObject.SetActive(false);

            var widget = root.gameObject.AddComponent<EquipmentStatRowWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_value").objectReferenceValue = value;
            so.FindProperty("_deltaChip").objectReferenceValue = chip;
            so.FindProperty("_deltaText").objectReferenceValue = deltaText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject, EquipmentUIFolder);
        }

        /// <summary>
        /// 장비창. 직사각형을 좌우 반반으로 나눈다.
        /// 좌 = 위쪽에 캐릭터 프리뷰와 그 오른쪽 스탯 줄, 아래쪽에 자리별 장착 슬롯.
        /// 우 = 위쪽에 고른 장비의 이름·설명과 장착 버튼, 아래쪽에 자리 탭과 칸 수가 정해진 가방.
        /// </summary>
        private static GameObject BuildEquipmentWindow(GameObject slotPrefab, GameObject candidatePrefab, GameObject statRowPrefab)
        {
            const float left = 24f;
            const float right = 612f;
            const float halfWidth = 564f;
            const float top = -72f;

            var popup = PopupRoot("EquipmentWindow");
            var modal = Rect(popup, "Modal", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1200f, 720f));
            Img(modal, Color.white, _panel).raycastTarget = true;

            // 비슷한 일을 하는 것끼리 묶음 하나에 모은다. 묶음은 모달을 꽉 덮으므로 안의 자리 계산은 모달 기준 그대로다.
            var header = Stretch(modal, "Header", 0f);
            var statGroup = Stretch(modal, "StatRows", 0f);
            var slotGroup = Stretch(modal, "Slots", 0f);
            var detail = Stretch(modal, "Detail", 0f);
            var tabGroup = Stretch(modal, "Tabs", 0f);

            Text(Rect(header, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -16f), new Vector2(200f, 36f)),
                "장비", 26f, TextWhite, HorizontalAlignmentOptions.Left);
            var closeBtn = MakeButton(header, "CloseBtn", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-12f, -12f), new Vector2(48f, 48f), "X");

            Img(Rect(modal, "Divider", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, top), new Vector2(2f, 622f)),
                new Color(1f, 1f, 1f, 0.12f));

            // ---- 좌상단: 캐릭터 프리뷰. 그림은 런타임에 프리뷰 무대의 텍스처가 꽂힌다 ----
            var previewFrame = Rect(modal, "PreviewFrame", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(left, top), new Vector2(270f, 300f));
            Img(previewFrame, PanelDarker, _rounded);
            var preview = Stretch(previewFrame, "Preview", 6f).gameObject.AddComponent<RawImage>();
            preview.raycastTarget = false;
            preview.enabled = false;

            // ---- 프리뷰 오른쪽: 스탯 한 줄씩. 고른 장비를 끼면 달라지는 값이 초록·빨강으로 붙는다 ----
            const float statX = left + 270f + 12f;
            Text(Rect(statGroup, "StatHeader", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(statX, top), new Vector2(282f, 26f)),
                "능력치", 16f, TextGray, HorizontalAlignmentOptions.Left);

            // 스탯 줄은 StatTypes.All 을 그대로 따른다 — 스탯이 늘면 줄도 따라 늘고, 여기 고칠 것은 없다.
            var statRows = new EquipmentStatRowWidget[StatTypes.All.Length];
            for (var i = 0; i < statRows.Length; i++)
            {
                statRows[i] = Place<EquipmentStatRowWidget>(statRowPrefab, statGroup, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(statX, top - 32f - 60f * i));
                statRows[i].Setup(StatTypes.All[i]);
            }

            // 안내문은 마지막 줄 아래에 붙는다 — 스탯이 늘어 줄이 내려가도 겹치지 않게.
            var statHintY = top - 100f - 60f * (statRows.Length - 1);
            var statHint = Text(
                Rect(statGroup, "StatHint", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(statX, statHintY), new Vector2(282f, 60f)),
                "장비를 고르면 끼었을 때 오르는 값은 초록, 내리는 값은 빨강으로 보인다", 13f, TextGray, HorizontalAlignmentOptions.Left);
            statHint.verticalAlignment = VerticalAlignmentOptions.Top;
            statHint.textWrappingMode = TextWrappingModes.Normal;

            // ---- 좌하단: 장착 슬롯. 몸을 마주 본 배치 — 주장비는 오른팔, 보조장비는 왼팔 자리다 ----
            Text(Rect(slotGroup, "SlotHeader", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, top - 312f), new Vector2(halfWidth, 26f)),
                "장착 슬롯  <size=80%>목록에서 끌어다 놓으면 바로 장착</size>", 16f, TextGray, HorizontalAlignmentOptions.Left);

            var slotLayout = new[]
            {
                EquipmentSlot.MainHand, EquipmentSlot.Helmet, EquipmentSlot.OffHand,
                EquipmentSlot.Greaves, EquipmentSlot.Chest, EquipmentSlot.Boots,
            };

            var slotWidgets = new EquipmentSlotWidget[slotLayout.Length];
            for (var i = 0; i < slotLayout.Length; i++)
            {
                var pos = new Vector2(left + 90f + 192f * (i % 3), top - 342f - 148f * (i / 3));
                slotWidgets[i] = PlaceSlot(slotPrefab, slotGroup, slotLayout[i], pos, new Vector2(180f, 136f));
            }

            // ---- 우상단: 설명 칸 + 장착 버튼. 후보나 슬롯을 고르면 여기가 채워진다 ----
            var description = Rect(detail, "Description", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(right, top), new Vector2(halfWidth, 150f));
            Img(description, Color.white, _panel);

            var descIconFrame = Rect(description, "IconFrame", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -14f), new Vector2(96f, 96f));
            Img(descIconFrame, PanelDarker, _rounded);
            var descIcon = Img(Stretch(descIconFrame, "Icon", 10f), Color.white);
            descIcon.preserveAspect = true;
            descIcon.enabled = false;

            var descName = Text(
                Rect(description, "Name", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(124f, -12f), new Vector2(426f, 30f)),
                "-", 21f, Gold, HorizontalAlignmentOptions.Left);

            var descStats = Text(
                Rect(description, "Stats", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(124f, -44f), new Vector2(426f, 24f)),
                "", 16f, TextWhite, HorizontalAlignmentOptions.Left);

            var descText = Text(
                Rect(description, "Text", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(124f, -72f), new Vector2(426f, 66f)),
                "", 14f, TextGray, HorizontalAlignmentOptions.Left);
            descText.verticalAlignment = VerticalAlignmentOptions.Top;
            descText.textWrappingMode = TextWrappingModes.Normal;

            var actionBtn = MakeButton(detail, "ActionBtn", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(right, top - 158f), new Vector2(halfWidth, 52f), "장착");
            var actionBtnLabel = actionBtn.GetComponentInChildren<TextMeshProUGUI>();

            // ---- 우하단: 자리 탭 한 줄 + 고정 칸 가방 ----
            var tabBtns = new Button[EquipmentSlots.All.Length + 1];
            var tabWidth = (halfWidth - 5f * (tabBtns.Length - 1)) / tabBtns.Length;
            for (var i = 0; i < tabBtns.Length; i++)
            {
                var displayName = i == 0 ? "전체" : EquipmentSlots.DisplayName(EquipmentSlots.All[i - 1]);
                tabBtns[i] = MakeButton(tabGroup, $"Tab_{displayName}", new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(right + (tabWidth + 5f) * i, top - 222f), new Vector2(tabWidth, 40f), displayName);
                tabBtns[i].GetComponentInChildren<TextMeshProUGUI>().fontSize = 14f;
            }

            // 가방은 칸 수가 정해진 고정 격자다. 빈 칸 바탕을 먼저 깔고, 후보는 같은 격자로 그 위에 앞에서부터 채워진다.
            var bag = Rect(modal, "Bag", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(right, top - 270f), new Vector2(halfWidth, 352f));
            Img(bag, PanelDarker, _rounded);

            var emptyCells = Stretch(bag, "EmptyCells", 0f);
            BagGrid(emptyCells);
            for (var i = 0; i < EquipmentWindow.BagCapacity; i++)
            {
                Img(Rect(emptyCells, $"Cell_{i}", Vector2.zero, Vector2.zero, Vector2.zero, BagCellSize),
                    new Color(1f, 1f, 1f, 0.06f), _rounded);
            }

            var candidatesRoot = Stretch(bag, "Candidates", 0f);
            BagGrid(candidatesRoot);

            var window = popup.gameObject.AddComponent<EquipmentWindow>();
            var so = new SerializedObject(window);
            BindPopup(so, popup);
            var slots = so.FindProperty("_slots");
            slots.arraySize = slotWidgets.Length;
            for (var i = 0; i < slotWidgets.Length; i++)
            {
                slots.GetArrayElementAtIndex(i).objectReferenceValue = slotWidgets[i];
            }

            var tabs = so.FindProperty("_tabBtns");
            tabs.arraySize = tabBtns.Length;
            for (var i = 0; i < tabBtns.Length; i++)
            {
                tabs.GetArrayElementAtIndex(i).objectReferenceValue = tabBtns[i];
            }

            so.FindProperty("_candidatesRoot").objectReferenceValue = candidatesRoot;
            so.FindProperty("_candidatePrefab").objectReferenceValue = candidatePrefab.GetComponent<EquipmentCandidateWidget>();
            so.FindProperty("_closeBtn").objectReferenceValue = closeBtn;
            so.FindProperty("_preview").objectReferenceValue = preview;
            var rows = so.FindProperty("_statRows");
            rows.arraySize = statRows.Length;
            for (var i = 0; i < statRows.Length; i++)
            {
                rows.GetArrayElementAtIndex(i).objectReferenceValue = statRows[i];
            }
            so.FindProperty("_descriptionIcon").objectReferenceValue = descIcon;
            so.FindProperty("_descriptionName").objectReferenceValue = descName;
            so.FindProperty("_descriptionStats").objectReferenceValue = descStats;
            so.FindProperty("_descriptionText").objectReferenceValue = descText;
            so.FindProperty("_actionBtn").objectReferenceValue = actionBtn;
            so.FindProperty("_actionBtnLabel").objectReferenceValue = actionBtnLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePopup(popup.gameObject, EquipmentUIFolder);
        }

        /// <summary>성장 항목 한 줄. 이름·레벨, "기본 + 성장" 분해, 값이 붙은 강화 버튼. 값은 창이 구독해서 채운다.</summary>
        private static GameObject BuildGrowthStatWidget()
        {
            var root = WidgetRoot("GrowthStatWidget", GrowthRowSize);
            Img(root, Color.white, _panel);

            var nameText = FitText(Text(
                Rect(root, "NameText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -12f), new Vector2(320f, 32f)),
                "공격력  Lv.1", 22f, TextWhite, HorizontalAlignmentOptions.Left), 15f);

            // 값은 "기본 +성장" 한 줄. 성장 몫은 초록으로 붙는다.
            var valueText = FitText(Text(
                Rect(root, "ValueText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -50f), new Vector2(320f, 32f)),
                "0 +0", 24f, TextWhite, HorizontalAlignmentOptions.Left), 15f);

            var upgradeBtn = MakeButton(root, "UpgradeBtn", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-14f, 0f), new Vector2(170f, 68f), "▲ 강화");
            var upgradeLabel = upgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
            upgradeLabel.fontSize = 20f;
            ((RectTransform)upgradeLabel.transform).anchoredPosition = new Vector2(0f, 12f);

            // 값은 버튼 안 아래쪽에 붙인다 — 무엇을 누르면 얼마가 나가는지 한 덩어리로 읽힌다.
            var costText = FitText(Text(
                Rect((RectTransform)upgradeBtn.transform, "CostText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 8f), new Vector2(156f, 24f)),
                "0 G", 17f, Gold), 12f);

            var widget = root.gameObject.AddComponent<GrowthStatWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_valueText").objectReferenceValue = valueText;
            so.FindProperty("_upgradeBtn").objectReferenceValue = upgradeBtn;
            so.FindProperty("_costText").objectReferenceValue = costText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        /// <summary>성장 모달. 보유 골드와 성장 항목 줄들. 줄 순서는 GrowthPlan.All 과 같아야 한다.</summary>
        /// <remarks>
        /// 장비창과 같은 짜임이다 — 왼쪽에 캐릭터 프리뷰, 오른쪽에 항목 줄. 줄마다 강화 버튼이 하나씩 붙는다.
        /// </remarks>
        private static GameObject BuildGrowthWindow(GameObject statPrefab)
        {
            const float margin = 24f;
            const float top = -72f;
            const float previewSize = 300f;
            const float rowGap = 12f;
            const float columnX = margin + previewSize + margin;

            var stats = GrowthPlan.All;
            var rowsBottom = -top + 44f + (GrowthRowSize.y + rowGap) * stats.Length;
            var modalSize = new Vector2(columnX + GrowthRowSize.x + margin, Mathf.Max(-top + previewSize, rowsBottom) + margin);

            var popup = PopupRoot("GrowthWindow");
            var root = Rect(popup, "Modal", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), modalSize);
            Img(root, Color.white, _panel).raycastTarget = true;

            Text(Rect(root, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -16f), new Vector2(200f, 36f)),
                "성장", 26f, TextWhite, HorizontalAlignmentOptions.Left);
            var closeBtn = MakeButton(root, "CloseBtn", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-12f, -12f), new Vector2(48f, 48f), "X");

            // ---- 왼쪽: 캐릭터 프리뷰. 그림은 런타임에 프리뷰 무대의 텍스처가 꽂힌다 ----
            var previewFrame = Rect(root, "PreviewFrame", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(margin, top), new Vector2(previewSize, previewSize));
            Img(previewFrame, PanelDarker, _rounded);
            var preview = Stretch(previewFrame, "Preview", 6f).gameObject.AddComponent<RawImage>();
            preview.raycastTarget = false;
            preview.enabled = false;

            // ---- 오른쪽: 보유 골드와 항목 줄 ----
            var goldText = Text(
                Rect(root, "GoldText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(columnX, top), new Vector2(GrowthRowSize.x, 32f)),
                "보유 골드  0", 20f, TextWhite, HorizontalAlignmentOptions.Left);

            var statWidgets = new GrowthStatWidget[stats.Length];
            for (var i = 0; i < stats.Length; i++)
            {
                statWidgets[i] = Place<GrowthStatWidget>(statPrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(columnX, top - 44f - (GrowthRowSize.y + rowGap) * i));
            }

            var window = popup.gameObject.AddComponent<GrowthWindow>();
            var so = new SerializedObject(window);
            BindPopup(so, popup);
            so.FindProperty("_closeBtn").objectReferenceValue = closeBtn;
            so.FindProperty("_goldText").objectReferenceValue = goldText;
            so.FindProperty("_preview").objectReferenceValue = preview;

            var widgets = so.FindProperty("_statWidgets");
            widgets.arraySize = statWidgets.Length;
            for (var i = 0; i < statWidgets.Length; i++)
            {
                widgets.GetArrayElementAtIndex(i).objectReferenceValue = statWidgets[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePopup(popup.gameObject);
        }

        /// <summary>
        /// 편성창 영웅 카드. 왼쪽에 고유색 띠와 초상화 메달, 오른쪽에 이름과 "출전 중 / 대기".
        /// 카드 전체가 버튼이다. 출전 중이면 금빛 테두리가 켜진다.
        /// </summary>
        private static GameObject BuildFormationHeroWidget()
        {
            var root = WidgetRoot("FormationHeroWidget", FormationHeroSize);
            var bg = Img(root, Color.white, _panel);
            bg.raycastTarget = true;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            StyleButton(button);

            var themeBar = Img(Rect(root, "ThemeBar", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f),
                new Vector2(8f, FormationHeroSize.y - 28f)), Color.white, _rounded);

            // 초상화는 명패(HeroProfileWidget)와 같은 비율로 원 안에 얼굴이 오게 올려 담는다.
            var medallion = Rect(root, "Portrait", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(118f, 118f));
            Img(medallion, Color.white, _medallion);
            var clip = Rect(medallion, "PortraitMask", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(102f, 102f));
            Img(clip, Color.white, _circle);
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var portrait = Icon(clip, "HeroPortrait", "Portraits/HeroPortrait", Vector2.one * 0.5f, new Vector2(0f, 22f), new Vector2(108f, 108f));

            var nameText = FitText(Text(
                Rect(root, "NameText", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(166f, 18f), new Vector2(176f, 40f)),
                "-", 30f, TextWhite, HorizontalAlignmentOptions.Left), 18f);

            var stateText = Text(
                Rect(root, "StateText", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(166f, -24f), new Vector2(176f, 28f)),
                "대기", 18f, TextGray, HorizontalAlignmentOptions.Left);

            // 출전 중 테두리. 가운데를 비운 9슬라이스라 테두리만 그려진다.
            var selectedMark = Stretch(root, "SelectedMark", 0f);
            var selectedFrame = Img(selectedMark, Gold, _rounded);
            selectedFrame.fillCenter = false;
            selectedFrame.pixelsPerUnitMultiplier = 0.5f;
            selectedMark.gameObject.SetActive(false);

            var widget = root.gameObject.AddComponent<FormationHeroWidget>();
            var so = new SerializedObject(widget);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_portrait").objectReferenceValue = portrait;
            so.FindProperty("_themeBar").objectReferenceValue = themeBar;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_stateText").objectReferenceValue = stateText;
            so.FindProperty("_selectedMark").objectReferenceValue = selectedMark.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveWidget(root.gameObject);
        }

        /// <summary>
        /// 편성 모달. 위 = 파티 칸 넷(첫 칸만 진짜, 나머지 셋은 잠긴 장식), 아래 = 영웅 명단과 오른쪽 아래 적용 버튼.
        /// 첫 칸에는 성장창·장비창과 같은 프리뷰 무대가 꽂혀 지금 영웅이 입은 그대로 선다.
        /// 명단 카드는 HeroID.All 순서로 깔고, 누가 누구인지는 여기서 프로필을 보고 구워 둔다.
        /// </summary>
        private static GameObject BuildFormationWindow(GameObject heroPrefab)
        {
            const float margin = 42f;
            const float slotGap = 20f;
            const float top = -72f;
            const float slotTop = top - 36f;
            var rosterTop = slotTop - PartySlotSize.y - 24f;
            var rosterCardTop = rosterTop - 36f;
            var rosterGap = (PartySlotSize.x * 4f + slotGap * 3f - FormationHeroSize.x * HeroID.All.Length) / (HeroID.All.Length - 1);

            var applyTop = rosterCardTop - FormationHeroSize.y - 16f;
            var applySize = new Vector2(240f, 56f);

            var modalSize = new Vector2(margin * 2f + PartySlotSize.x * 4f + slotGap * 3f, -applyTop + applySize.y + 24f);

            var popup = PopupRoot("FormationWindow");
            var root = Rect(popup, "Modal", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), modalSize);
            Img(root, Color.white, _panel).raycastTarget = true;

            Text(Rect(root, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -16f), new Vector2(200f, 36f)),
                "편성", 26f, TextWhite, HorizontalAlignmentOptions.Left);
            var closeBtn = MakeButton(root, "CloseBtn", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-12f, -12f), new Vector2(48f, 48f), "X");

            // ---- 위: 파티 칸 넷 ----
            Text(Rect(root, "PartyHeader", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(margin, top), new Vector2(800f, 26f)),
                "파티  <size=80%>첫 칸의 영웅이 지금 싸운다</size>", 16f, TextGray, HorizontalAlignmentOptions.Left);

            var leaderSlot = Rect(root, "PartySlot_1", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(margin, slotTop), PartySlotSize);
            Img(leaderSlot, Color.white, _panel);

            // 프리뷰 칸은 아래 이름판 자리를 비우고 칸을 채운다. 그림은 런타임에 프리뷰 무대의 텍스처가 꽂힌다.
            var previewFrame = Stretch(leaderSlot, "PreviewFrame", 10f);
            previewFrame.offsetMin = new Vector2(10f, 58f);
            Img(previewFrame, PanelDarker, _rounded);
            var preview = Stretch(previewFrame, "Preview", 6f).gameObject.AddComponent<RawImage>();
            preview.raycastTarget = false;
            preview.enabled = false;

            var leaderTag = Rect(leaderSlot, "LeaderTag", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(64f, 26f));
            Img(leaderTag, Gold, _gauge).type = Image.Type.Sliced;
            Text(Stretch(leaderTag, "Text", 2f), "리더", 15f, PanelDarker);

            var leaderName = FitText(Text(
                Rect(leaderSlot, "LeaderName", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(PartySlotSize.x - 24f, 40f)),
                "-", 26f, Gold), 16f);

            // 나머지 셋은 잠긴 칸이다. 누를 것도 바뀔 것도 없는 장식이라 컴포넌트를 달지 않는다.
            for (var i = 1; i < 4; i++)
            {
                var slot = Rect(root, $"PartySlot_{i + 1}", new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(margin + (PartySlotSize.x + slotGap) * i, slotTop), PartySlotSize);
                Img(slot, new Color(0.62f, 0.64f, 0.72f), _panel);
                Img(Stretch(slot, "Inner", 10f), PanelDarker, _rounded);

                Text(Rect(slot, "Number", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -16f), new Vector2(40f, 30f)),
                    $"{i + 1}", 22f, new Color(1f, 1f, 1f, 0.25f), HorizontalAlignmentOptions.Left);
                var lockIcon = Icon(slot, "Lock", "Icons/Lock", Vector2.one * 0.5f, new Vector2(0f, 28f), new Vector2(72f, 72f));
                lockIcon.color = new Color(0.64f, 0.68f, 0.80f, 0.8f);
                Text(Rect(slot, "LockedText", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -36f), new Vector2(220f, 32f)),
                    "잠김", 22f, TextGray);
                Text(Rect(slot, "LockedHint", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -68f), new Vector2(220f, 26f)),
                    "파티 사냥 준비 중", 15f, new Color(0.65f, 0.65f, 0.7f, 0.7f));
            }

            // ---- 아래: 영웅 명단 ----
            Text(Rect(root, "RosterHeader", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(margin, rosterTop), new Vector2(800f, 26f)),
                "영웅  <size=80%>고르고 적용을 누르면 첫 칸의 영웅이 바뀐다. 웨이브는 그대로 이어진다</size>", 16f, TextGray, HorizontalAlignmentOptions.Left);

            // 적용 버튼은 명단 아래 오른쪽. 쓰러진 동안은 막히고, 그때만 켜지는 안내가 버튼 왼쪽에 붙는다.
            var applyBtn = MakeButton(root, "ApplyBtn", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-margin, applyTop), applySize, "적용");

            var downHint = Text(
                Rect(root, "DownHint", new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                    new Vector2(-margin - applySize.x - 20f, applyTop - applySize.y * 0.5f), new Vector2(360f, 26f)),
                "쓰러진 동안은 바꿀 수 없다", 16f, new Color(0.9f, 0.45f, 0.45f), HorizontalAlignmentOptions.Right);
            downHint.gameObject.SetActive(false);

            var heroWidgets = new FormationHeroWidget[HeroID.All.Length];
            for (var i = 0; i < heroWidgets.Length; i++)
            {
                var heroID = HeroID.All[i];
                heroWidgets[i] = Place<FormationHeroWidget>(heroPrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(margin + (FormationHeroSize.x + rosterGap) * i, rosterCardTop));
                heroWidgets[i].Setup(heroID, HeroProfile(heroID));
                heroWidgets[i].SetSelected(i == 0);
                heroWidgets[i].SetDeployed(i == 0);
            }

            leaderName.text = HeroProfile(HeroID.All[0]).DisplayName;

            var window = popup.gameObject.AddComponent<FormationWindow>();
            var so = new SerializedObject(window);
            BindPopup(so, popup);
            so.FindProperty("_closeBtn").objectReferenceValue = closeBtn;
            so.FindProperty("_applyBtn").objectReferenceValue = applyBtn;
            so.FindProperty("_leaderPreview").objectReferenceValue = preview;
            so.FindProperty("_leaderName").objectReferenceValue = leaderName;
            so.FindProperty("_downHint").objectReferenceValue = downHint.gameObject;

            var widgets = so.FindProperty("_heroWidgets");
            widgets.arraySize = heroWidgets.Length;
            for (var i = 0; i < heroWidgets.Length; i++)
            {
                widgets.GetArrayElementAtIndex(i).objectReferenceValue = heroWidgets[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (var widget in heroWidgets)
            {
                foreach (var component in widget.GetComponentsInChildren<Component>(true))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component.gameObject);
                }
            }

            return SavePopup(popup.gameObject);
        }

        private static CharacterProfile HeroProfile(string heroID)
        {
            var profile = AssetDatabase.LoadAssetAtPath<CharacterProfile>($"{HeroesFolder}/{heroID}/{heroID}.asset");
            if (profile == null) throw new System.InvalidOperationException($"Hero profile missing: {heroID}");
            return profile;
        }

        private static EquipmentSlotWidget PlaceSlot(GameObject slotPrefab, RectTransform parent, EquipmentSlot slot,
            Vector2 pos, Vector2 size)
        {
            var widget = Place<EquipmentSlotWidget>(slotPrefab, parent, new Vector2(0f, 1f), new Vector2(0.5f, 1f), pos);
            ((RectTransform)widget.transform).sizeDelta = size;
            widget.Setup(slot, SlotLabel(slot));
            widget.SetItem("없음", null);
            return widget;
        }

        /// <summary>가방 격자. 빈 칸 바탕과 후보가 같은 설정을 써야 칸이 정확히 겹친다.</summary>
        private static void BagGrid(RectTransform rt)
        {
            var grid = rt.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = BagCellSize;
            grid.spacing = new Vector2(6f, 6f);
            grid.padding = new RectOffset(9, 9, 7, 7);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = BagColumns;
        }

        /// <summary>손 자리는 어느 팔인지까지 적는다 — 주장비는 오른팔, 보조장비는 왼팔에 든다.</summary>
        private static string SlotLabel(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.MainHand => "주장비 (오른팔)",
                EquipmentSlot.OffHand => "보조장비 (왼팔)",
                _ => EquipmentSlots.DisplayName(slot)
            };
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

        /// <summary>
        /// 웨이브 시작 화면. 화면 전체를 덮는다. ScreenPerformanceManager 가 웨이브마다 만들어 띄우고 치운다.
        /// 어두운 바탕 위에 판 하나, 그 위에 웨이브 이름과 "전투 개시" 만 있다.
        /// </summary>
        private static void ComposeWaveStartPanel()
        {
            var panelRoot = new GameObject("WaveStartPanel", typeof(RectTransform), typeof(WaveStartPanel));
            var panelRect = (RectTransform)panelRoot.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var dim = Stretch(panelRect, "Dim", 0f);
            // 알림이 떠 있는 동안에도 게임은 돈다. 바탕이 입력을 가로채면 그 사이 조작이 먹통이 된다.
            Img(dim, new Color(0f, 0f, 0.05f, 0.45f)).raycastTarget = false;
            var dimGroup = Group(dim);

            var board = Rect(dim, "Board", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 0f), new Vector2(880f, 260f));
            Img(board, PanelDark, _panel);
            var boardGroup = Group(board);

            var waveText = Text(
                Rect(board, "WaveText", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -62f), new Vector2(640f, 36f)),
                "STAGE 1 - 1", 26f, TextGray);

            var titleText = Text(
                Rect(board, "TitleText", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -132f), new Vector2(680f, 72f)),
                "전투 개시", 54f, Gold);

            var divider = Rect(board, "Divider", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(760f, 3f));
            Img(divider, new Color(1f, 1f, 1f, 0.14f));

            var panel = panelRoot.GetComponent<WaveStartPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_dimGroup").objectReferenceValue = dimGroup;
            so.FindProperty("_board").objectReferenceValue = board;
            so.FindProperty("_boardGroup").objectReferenceValue = boardGroup;
            so.FindProperty("_waveText").objectReferenceValue = waveText;
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_divider").objectReferenceValue = divider;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(panelRoot, WaveStartPanelPath);
            Object.DestroyImmediate(panelRoot);
        }

        /// <summary>구역 머리말 한 줄. 왼쪽 정렬 소제목이다.</summary>
        /// <summary>
        /// 저체력 경고 막. 화면 전체를 덮되 가운데는 비어 있고 가장자리만 붉다.
        /// 켜고 끄는 건 ScreenPerformanceManager 가, 울렁이는 건 패널 자신이 한다.
        /// </summary>
        private static void ComposeLowHealthPanel()
        {
            var panelRoot = new GameObject("LowHealthPanel", typeof(RectTransform), typeof(CanvasGroup),
                typeof(LowHealthPanel));
            var panelRect = (RectTransform)panelRoot.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // 막이 안팎으로 숨을 쉬므로 화면보다 조금 크게 깔아 둔다 — 줄어들 때 가장자리가 비지 않게.
            var edge = Stretch(panelRect, "Edge", -40f);
            Img(edge, new Color(0.78f, 0.05f, 0.08f), Art("Frames/Vignette")).raycastTarget = false;

            var group = panelRoot.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var panel = panelRoot.GetComponent<LowHealthPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_group").objectReferenceValue = group;
            so.FindProperty("_edge").objectReferenceValue = edge;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(panelRoot, LowHealthPanelPath);
            Object.DestroyImmediate(panelRoot);
        }

        private static RectTransform SectionHeader(RectTransform board, string name, float y, string label)
        {
            var rt = Rect(board, name, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(64f, y), new Vector2(320f, 32f));
            Text(rt, label, 22f, Gold, HorizontalAlignmentOptions.Left);
            return rt;
        }

        private static CanvasGroup Group(RectTransform rt)
        {
            return rt.gameObject.AddComponent<CanvasGroup>();
        }

        private static void ComposePanel(GameObject heroProfilePrefab, GameObject currencyPrefab, GameObject questPrefab,
            GameObject stagePrefab, GameObject iconMenuPrefab, GameObject circleButtonPrefab, GameObject skillButtonPrefab)
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
            var heroProfile = Place<HeroProfileWidget>(heroProfilePrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f));
            heroProfile.Preview("이름없음", level: 0, hp: 5000, maxHP: 5000, expRatio: 0.35f);

            var gold = Place<CurrencyWidget>(currencyPrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -158f));
            gold.Preview(1004);
            gold.SetColor(Gold);

            var gem = Place<CurrencyWidget>(currencyPrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(240f, -158f));
            gem.Preview(500);
            gem.SetColor(GemBlue);
            gem.transform.Find("Icon").GetComponent<Image>().sprite = Art("Icons/Gem");

            var quest = Place<QuestWidget>(questPrefab, root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -218f));
            quest.Preview(QuestPlan.EnemyKill("고블린 사냥꾼", "고블린", count: 10, goldReward: 300, EnemyID.Goblin),
                index: 2, progress: 4, isComplete: false);

            // 중앙 상단
            var stage = Place<StageWidget>(stagePrefab, root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f));
            stage.Preview("STAGE 1-1", kills: 8, goal: 25);

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
            Button growthMenuButton = null;
            Button formationMenuButton = null;
            foreach (var (label, iconName, offsetX) in bottomMenus)
            {
                var menu = Place<IconMenuWidget>(iconMenuPrefab, bottomBar, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(offsetX, 0f));
                ((RectTransform)menu.transform).sizeDelta = new Vector2(100f, 104f);
                menu.SetLabel(label);
                SetMenuIcon(menu, iconName, label != "장비" && label != "성장" && label != "편성");
                if (label == "장비")
                {
                    equipMenuButton = MenuButton(menu);
                }
                if (label == "성장")
                {
                    growthMenuButton = MenuButton(menu);
                }
                if (label == "편성")
                {
                    formationMenuButton = MenuButton(menu);
                }
            }
            var centerSlot = Place<CircleButtonWidget>(circleButtonPrefab, bottomBar, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 14f));
            ((RectTransform)centerSlot.transform).sizeDelta = new Vector2(104f, 104f);
            centerSlot.SetText(string.Empty);
            Icon((RectTransform)centerSlot.transform, "Crest", "Icons/Summon", Vector2.one * 0.5f, new Vector2(0f, 8f), new Vector2(62f, 62f));
            Text(Rect((RectTransform)centerSlot.transform, "Caption", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(88f, 24f)), "전투", 18f, Gold);
            // 궁극기가 모서리의 큰 버튼, 스킬은 그 왼쪽의 작은 버튼. 크기와 테두리 색 둘 다로 갈라 헷갈리지 않게 한다.
            var ultimateButton = Place<SkillButtonWidget>(skillButtonPrefab, root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 28f));
            ((RectTransform)ultimateButton.transform).sizeDelta = new Vector2(172f, 172f);
            ultimateButton.Preview("궁극기", cooldownRatio: 0f, cooldownRemain: 0f);
            ultimateButton.transform.Find("Icon").GetComponent<Image>().sprite = Art("Icons/Ultimate");
            SetSkillBorder(ultimateButton, UltimateRim);
            var skillButton = Place<SkillButtonWidget>(skillButtonPrefab, root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-218f, 44f));
            ((RectTransform)skillButton.transform).sizeDelta = new Vector2(116f, 116f);
            skillButton.Preview("스킬", cooldownRatio: 0f, cooldownRemain: 0f);
            SetSkillBorder(skillButton, SkillRim);
            var gadgetButton = PlaceGadgetButton(skillButtonPrefab, root);
            BuildBottomLeftDecorations(root);

            // 부활 텍스트
            var reviveText = Text(
                Rect(root, "ReviveText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(500f, 50f)),
                string.Empty, 36f, TextWhite);

            var panel = panelRoot.GetComponent<BattlePanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_reviveText").objectReferenceValue = reviveText;
            so.FindProperty("_joystick").objectReferenceValue = joystick;
            so.FindProperty("_heroProfile").objectReferenceValue = heroProfile;
            so.FindProperty("_goldWidget").objectReferenceValue = gold;
            so.FindProperty("_gemWidget").objectReferenceValue = gem;
            so.FindProperty("_stageWidget").objectReferenceValue = stage;
            so.FindProperty("_questWidget").objectReferenceValue = quest;
            so.FindProperty("_gadgetButton").objectReferenceValue = gadgetButton;
            so.FindProperty("_skillButton").objectReferenceValue = skillButton;
            so.FindProperty("_ultimateButton").objectReferenceValue = ultimateButton;
            so.FindProperty("_equipMenuButton").objectReferenceValue = equipMenuButton;
            so.FindProperty("_growthMenuButton").objectReferenceValue = growthMenuButton;
            so.FindProperty("_formationMenuButton").objectReferenceValue = formationMenuButton;
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

        /// <summary>아이콘 메뉴를 실제 눌리는 버튼으로 승격한다.</summary>
        private static Button MenuButton(IconMenuWidget menu)
        {
            var icon = menu.GetComponent<Image>();
            icon.raycastTarget = true;
            var button = menu.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            StyleButton(button);
            return button;
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
            Img(baseRT, new Color(1f, 1f, 1f, 0.35f), JoystickSprite("JoystickBase"));
            var knobRT = Rect(baseRT, "JoystickKnob", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
            Img(knobRT, new Color(1f, 1f, 1f, 0.8f), JoystickSprite("JoystickKnob"));
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

        /// <summary>조이스틱은 자기 폴더의 그림을 쓴다 — SayneAssets 만 들고 가도 그대로 선다.</summary>
        private static Sprite JoystickSprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/SayneAssets/UI/Joystick/Sprites/{name}.png");
            if (sprite == null) throw new System.InvalidOperationException($"Joystick sprite missing: {name}");
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

        /// <summary>어느 기능에도 매이지 않은 위젯만 공용 위젯 폴더로 간다. 기능 전용 위젯은 그 기능 폴더를 받아 간다.</summary>
        private static GameObject SaveWidget(GameObject temp, string folder = WidgetFolder)
        {
            var path = $"{folder}/{temp.name}.prefab";
            foreach (var component in temp.GetComponentsInChildren<Component>(true))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        /// <summary>
        /// 팝업의 뿌리. 화면을 꽉 채우는 입력 막이다 — 뒤쪽 UI 는 눌리지 않는다.
        /// 그 아래로 흐린 화면(Blur) → 옅은 암막(Dim) 을 깔고, 창 모양은 호출한 쪽이 이 뒤에 "Modal" 로 단다.
        /// 뒤판이 모달의 부모라 언제나 모달 뒤에 그려진다.
        /// </summary>
        private static RectTransform PopupRoot(string name)
        {
            var root = WidgetRoot(name, Vector2.zero, new Vector2(0.5f, 0.5f));
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.gameObject.AddComponent<CanvasGroup>();
            Img(root, Color.clear).raycastTarget = true;

            // 흐린 화면은 열 때 PopupManager 가 찍어 넣는다. 그 전(프리팹·미리보기)엔 꺼져 있고 암막만 보인다.
            var blur = Stretch(root, "Blur", 0f).gameObject.AddComponent<RawImage>();
            blur.material = PopupBlurMaterial();
            blur.color = PopupBlurTint;
            blur.raycastTarget = false;
            blur.enabled = false;

            Img(Stretch(root, "Dim", 0f), PopupDim);
            return root;
        }

        /// <summary>PopupWindow 공통 바인딩. 창 컴포넌트를 단 뒤에 부른다.</summary>
        private static void BindPopup(SerializedObject so, RectTransform popup)
        {
            so.FindProperty("_group").objectReferenceValue = popup.GetComponent<CanvasGroup>();
            so.FindProperty("_blur").objectReferenceValue = popup.Find("Blur").GetComponent<RawImage>();
        }

        /// <summary>흐린 화면을 그리는 머티리얼. 프리팹이 들고 있어야 빌드에 셰이더가 따라간다.</summary>
        private static Material PopupBlurMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(PopupBlurMaterialPath);

            if (material == null)
            {
                material = new Material(Shader.Find("Sayne/UI/PopupBlur"));
                AssetDatabase.CreateAsset(material, PopupBlurMaterialPath);
            }

            return material;
        }

        private static GameObject SavePopup(GameObject temp, string folder = PopupFolder)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, $"{folder}/{temp.name}.prefab");
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

        /// <summary>글이 칸보다 길면 넘치지 않고 minSize 까지 줄어든다. 그래도 넘치면 말줄임.</summary>
        private static TextMeshProUGUI FitText(TextMeshProUGUI tmp, float minSize)
        {
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = minSize;
            tmp.fontSizeMax = tmp.fontSize;
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
