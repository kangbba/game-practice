using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>UI 프리팹 검색 기능만 노출하는 인터페이스.</summary>
    public interface IUIAssets
    {
        WorldHPBar WorldHPBarPrefab { get; }
        BattlePhaseUIPanel BattlePanelPrefab { get; }
    }

    public class UIAssetManager : AssetManagerBase<GameObject>, IUIAssets
    {
        public WorldHPBar WorldHPBarPrefab => Get(AssetAddresses.WorldHPBar)?.GetComponent<WorldHPBar>();
        public BattlePhaseUIPanel BattlePanelPrefab => Get(AssetAddresses.BattlePhaseUIPanel)?.GetComponent<BattlePhaseUIPanel>();

        protected override async UniTask OnLoadAsync(CancellationToken token)
        {
            var (hpBar, battlePanel) = await UniTask.WhenAll(
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.WorldHPBar, token),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.BattlePhaseUIPanel, token));

            Register(AssetAddresses.WorldHPBar, hpBar);
            Register(AssetAddresses.BattlePhaseUIPanel, battlePanel);
        }
    }
}
