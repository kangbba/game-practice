using System.Threading;
using Cysharp.Threading.Tasks;

namespace Sayne
{
    public class WeaponAssetManager : AssetManagerBase<WeaponDefinition>
    {
        protected override async UniTask OnLoadAsync(CancellationToken token)
        {
            foreach (var definition in await LoadAssetsByLabelAsync<WeaponDefinition>(AssetAddresses.WeaponsLabel, token))
            {
                Register(definition.name, definition);
            }
        }
    }
}
