using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class UIAssetManager : AssetManagerBase<GameObject>
    {
        public OverlayHPBar OverlayHPBarPrefab => Get(AssetAddresses.OverlayHPBar).GetComponent<OverlayHPBar>();
        public DamageText DamageTextPrefab => Get(AssetAddresses.DamageText).GetComponent<DamageText>();
        public BattlePhaseUIPanel BattlePanelPrefab => Get(AssetAddresses.BattlePhaseUIPanel).GetComponent<BattlePhaseUIPanel>();
        public DropItem DropItemPrefab => Get(AssetAddresses.DropItem).GetComponent<DropItem>();
        public UltimateCutscenePanel UltimateCutscenePanelPrefab => Get(AssetAddresses.UltimateCutscenePanel).GetComponent<UltimateCutscenePanel>();

        public TutorialWidget TutorialWidgetPrefab => Get(AssetAddresses.TutorialWidget).GetComponent<TutorialWidget>();
        public OverlaySpeechBubble OverlaySpeechBubblePrefab => Get(AssetAddresses.OverlaySpeechBubble).GetComponent<OverlaySpeechBubble>();

        protected override async UniTask OnLoadAsync()
        {
            var (hpBar, damageText, battlePanel, dropItem, cutscenePanel, tutorialWidget, overlayBubble) = await UniTask.WhenAll(
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.OverlayHPBar),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.DamageText),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.BattlePhaseUIPanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.DropItem),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.UltimateCutscenePanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.TutorialWidget),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.OverlaySpeechBubble));

            Register(AssetAddresses.OverlayHPBar, hpBar);
            Register(AssetAddresses.DamageText, damageText);
            Register(AssetAddresses.BattlePhaseUIPanel, battlePanel);
            Register(AssetAddresses.DropItem, dropItem);
            Register(AssetAddresses.UltimateCutscenePanel, cutscenePanel);
            Register(AssetAddresses.TutorialWidget, tutorialWidget);
            Register(AssetAddresses.OverlaySpeechBubble, overlayBubble);
        }
    }
}
