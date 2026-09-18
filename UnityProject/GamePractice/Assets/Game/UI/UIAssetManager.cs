using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class UIAssetManager : AssetManagerBase<GameObject>
    {
        public OverlayHPBar OverlayHPBarPrefab => Get(AssetAddresses.OverlayHPBar).GetComponent<OverlayHPBar>();
        public WorldSpeechBubble WorldSpeechBubblePrefab => Get(AssetAddresses.WorldSpeechBubble).GetComponent<WorldSpeechBubble>();
        public WorldHPBar WorldHPBarPrefab => Get(AssetAddresses.WorldHPBar).GetComponent<WorldHPBar>();
        public DamageText DamageTextPrefab => Get(AssetAddresses.DamageText).GetComponent<DamageText>();
        public BattlePanel BattlePanelPrefab => Get(AssetAddresses.BattlePanel).GetComponent<BattlePanel>();
        public WaveStartPanel WaveStartPanelPrefab => Get(AssetAddresses.UIPrefab_WaveStart).GetComponent<WaveStartPanel>();
        public LowHealthPanel LowHealthPanelPrefab => Get(AssetAddresses.UIPrefab_LowHealth).GetComponent<LowHealthPanel>();
        public DropItem DropItemPrefab => Get(AssetAddresses.DropItem).GetComponent<DropItem>();
        public UltimateCutscenePanel UltimateCutscenePanelPrefab => Get(AssetAddresses.UltimateCutscenePanel).GetComponent<UltimateCutscenePanel>();

        public TutorialWidget TutorialWidgetPrefab => Get(AssetAddresses.TutorialWidget).GetComponent<TutorialWidget>();
        public OverlaySpeechBubble OverlaySpeechBubblePrefab => Get(AssetAddresses.OverlaySpeechBubble).GetComponent<OverlaySpeechBubble>();

        /// <summary>그 종류에 짝지은 팝업 프리팹.</summary>
        public PopupWindow GetPopupPrefab(PopupType type)
        {
            return Get(PopupTypes.GetAddress(type)).GetComponent<PopupWindow>();
        }

        protected override async UniTask OnLoadAsync()
        {
            var (hpBar, damageText, battlePanel, waveStartPanel, lowHealthPanel, dropItem, cutscenePanel, tutorialWidget, overlayBubble) = await UniTask.WhenAll(
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.OverlayHPBar),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.DamageText),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.BattlePanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.UIPrefab_WaveStart),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.UIPrefab_LowHealth),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.DropItem),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.UltimateCutscenePanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.TutorialWidget),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.OverlaySpeechBubble));

            Register(AssetAddresses.OverlayHPBar, hpBar);
            Register(AssetAddresses.DamageText, damageText);
            Register(AssetAddresses.BattlePanel, battlePanel);
            Register(AssetAddresses.UIPrefab_WaveStart, waveStartPanel);
            Register(AssetAddresses.UIPrefab_LowHealth, lowHealthPanel);
            Register(AssetAddresses.DropItem, dropItem);
            Register(AssetAddresses.UltimateCutscenePanel, cutscenePanel);
            Register(AssetAddresses.TutorialWidget, tutorialWidget);
            Register(AssetAddresses.OverlaySpeechBubble, overlayBubble);
            Register(AssetAddresses.WorldHPBar, await LoadAssetByAddressAsync<GameObject>(AssetAddresses.WorldHPBar));
            Register(AssetAddresses.WorldSpeechBubble, await LoadAssetByAddressAsync<GameObject>(AssetAddresses.WorldSpeechBubble));

            foreach (var type in PopupTypes.All)
            {
                var address = PopupTypes.GetAddress(type);
                Register(address, await LoadAssetByAddressAsync<GameObject>(address));
            }
        }
    }
}
