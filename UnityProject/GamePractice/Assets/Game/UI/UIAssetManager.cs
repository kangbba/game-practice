using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// UI 프리팹 창고. 로드 때 컴포넌트까지 뽑아 두고, 쓰는 쪽에는 필요한 것만 인터페이스로 내준다.
    /// 팝업만 테이블에 남는다 — 무엇을 쓸지 런타임에 PopupType 으로 고르기 때문이다.
    /// </summary>
    public class UIAssetManager : AssetManagerBase<GameObject>, IScreenUIAssets, IPopupAssets
    {
        public OverlayHPBar OverlayHPBarPrefab { get; private set; }
        public WorldSpeechBubble WorldSpeechBubblePrefab { get; private set; }
        public WorldHPBar WorldHPBarPrefab { get; private set; }
        public DamageText DamageTextPrefab { get; private set; }
        public BattlePanel BattlePanelPrefab { get; private set; }
        public WaveStartPanel WaveStartPanelPrefab { get; private set; }
        public LowHealthPanel LowHealthPanelPrefab { get; private set; }
        public UltimateCutscenePanel UltimateCutscenePanelPrefab { get; private set; }
        public SpeechBubbleWidget SpeechBubbleWidgetPrefab { get; private set; }
        public OverlaySpeechBubble OverlaySpeechBubblePrefab { get; private set; }

        /// <summary>그 종류에 짝지은 팝업 프리팹.</summary>
        public PopupWindow GetPopupPrefab(PopupType type)
        {
            return Get(PopupTypes.GetAddress(type)).GetComponent<PopupWindow>();
        }

        protected override async UniTask OnLoadAsync()
        {
            var (hpBar, damageText, battlePanel, waveStartPanel, lowHealthPanel, cutscenePanel, speechBubbleWidget, overlayBubble) = await UniTask.WhenAll(
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.OverlayHPBar),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.DamageText),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.BattlePanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.WaveStartPanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.LowHealthPanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.UltimateCutscenePanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.SpeechBubbleWidget),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.OverlaySpeechBubble));

            OverlayHPBarPrefab = hpBar.GetComponent<OverlayHPBar>();
            DamageTextPrefab = damageText.GetComponent<DamageText>();
            BattlePanelPrefab = battlePanel.GetComponent<BattlePanel>();
            WaveStartPanelPrefab = waveStartPanel.GetComponent<WaveStartPanel>();
            LowHealthPanelPrefab = lowHealthPanel.GetComponent<LowHealthPanel>();
            UltimateCutscenePanelPrefab = cutscenePanel.GetComponent<UltimateCutscenePanel>();
            SpeechBubbleWidgetPrefab = speechBubbleWidget.GetComponent<SpeechBubbleWidget>();
            OverlaySpeechBubblePrefab = overlayBubble.GetComponent<OverlaySpeechBubble>();

            WorldHPBarPrefab = (await LoadAssetByAddressAsync<GameObject>(AssetAddresses.WorldHPBar))
                .GetComponent<WorldHPBar>();
            WorldSpeechBubblePrefab = (await LoadAssetByAddressAsync<GameObject>(AssetAddresses.WorldSpeechBubble))
                .GetComponent<WorldSpeechBubble>();

            foreach (var type in PopupTypes.All)
            {
                var address = PopupTypes.GetAddress(type);
                Register(address, await LoadAssetByAddressAsync<GameObject>(address));
            }
        }

        protected override void OnAssetRelease()
        {
            OverlayHPBarPrefab = null;
            WorldSpeechBubblePrefab = null;
            WorldHPBarPrefab = null;
            DamageTextPrefab = null;
            BattlePanelPrefab = null;
            WaveStartPanelPrefab = null;
            LowHealthPanelPrefab = null;
            UltimateCutscenePanelPrefab = null;
            SpeechBubbleWidgetPrefab = null;
            OverlaySpeechBubblePrefab = null;
        }
    }
}
