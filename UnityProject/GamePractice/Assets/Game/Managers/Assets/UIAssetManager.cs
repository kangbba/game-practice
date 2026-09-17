using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class UIAssetManager : AssetManagerBase<GameObject>
    {
        public WorldHPBar WorldHPBarPrefab => Get(AssetAddresses.WorldHPBar).GetComponent<WorldHPBar>();
        public DamageText DamageTextPrefab => Get(AssetAddresses.DamageText).GetComponent<DamageText>();
        public BattlePhaseUIPanel BattlePanelPrefab => Get(AssetAddresses.BattlePhaseUIPanel).GetComponent<BattlePhaseUIPanel>();
        public ResultPhaseUIPanel ResultPanelPrefab => Get(AssetAddresses.ResultPhaseUIPanel).GetComponent<ResultPhaseUIPanel>();
        public DropItem DropItemPrefab => Get(AssetAddresses.DropItem).GetComponent<DropItem>();

        protected override async UniTask OnLoadAsync()
        {
            var (hpBar, damageText, battlePanel, resultPanel, dropItem) = await UniTask.WhenAll(
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.WorldHPBar),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.DamageText),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.BattlePhaseUIPanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.ResultPhaseUIPanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.DropItem));

            Register(AssetAddresses.WorldHPBar, hpBar);
            Register(AssetAddresses.DamageText, damageText);
            Register(AssetAddresses.BattlePhaseUIPanel, battlePanel);
            Register(AssetAddresses.ResultPhaseUIPanel, resultPanel);
            Register(AssetAddresses.DropItem, dropItem);
        }
    }
}
