using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class UIAssetManager : AssetManagerBase<GameObject>
    {
        public WorldHPBar WorldHPBarPrefab => Get(AssetAddresses.WorldHPBar).GetComponent<WorldHPBar>();
        public BattlePhaseUIPanel BattlePanelPrefab => Get(AssetAddresses.BattlePhaseUIPanel).GetComponent<BattlePhaseUIPanel>();
        public ResultPhaseUIPanel ResultPanelPrefab => Get(AssetAddresses.ResultPhaseUIPanel).GetComponent<ResultPhaseUIPanel>();

        protected override async UniTask OnLoadAsync()
        {
            var (hpBar, battlePanel, resultPanel) = await UniTask.WhenAll(
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.WorldHPBar),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.BattlePhaseUIPanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.ResultPhaseUIPanel));

            Register(AssetAddresses.WorldHPBar, hpBar);
            Register(AssetAddresses.BattlePhaseUIPanel, battlePanel);
            Register(AssetAddresses.ResultPhaseUIPanel, resultPanel);
        }
    }
}
