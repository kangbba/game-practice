using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class UIAssetManager : AssetManagerBase<GameObject>
    {
        public OverlayHPBar OverlayHPBarPrefab => Get(AssetAddresses.OverlayHPBar).GetComponent<OverlayHPBar>();
        public DamageText DamageTextPrefab => Get(AssetAddresses.DamageText).GetComponent<DamageText>();
        public BattlePanel BattlePanelPrefab => Get(AssetAddresses.BattlePanel).GetComponent<BattlePanel>();
        public WaveStartPanel WaveStartPanelPrefab => Get(AssetAddresses.UIPrefab_WaveStart).GetComponent<WaveStartPanel>();
        public LowHealthPanel LowHealthPanelPrefab => Get(AssetAddresses.UIPrefab_LowHealth).GetComponent<LowHealthPanel>();
        public DropItem DropItemPrefab => Get(AssetAddresses.DropItem).GetComponent<DropItem>();
        public UltimateCutscenePanel UltimateCutscenePanelPrefab => Get(AssetAddresses.UltimateCutscenePanel).GetComponent<UltimateCutscenePanel>();

        public TutorialWidget TutorialWidgetPrefab => Get(AssetAddresses.TutorialWidget).GetComponent<TutorialWidget>();
        public OverlaySpeechBubble OverlaySpeechBubblePrefab => Get(AssetAddresses.OverlaySpeechBubble).GetComponent<OverlaySpeechBubble>();

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
        }
    }
}
