using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class MapAssetManager : AssetManagerBase<GameObject>
    {
        protected override async UniTask OnLoadAsync(CancellationToken token)
        {
            foreach (var go in await LoadAssetsByLabelAsync<GameObject>(AssetAddresses.MapsLabel, token))
            {
                Register(go.name, go);
            }
        }
    }
}
