using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class MapAssetManager : AssetManagerBase<GameObject>
    {
        protected override async UniTask OnLoadAsync()
        {
            foreach (var go in await LoadAssetsByLabelAsync<GameObject>(AssetAddresses.MapsLabel))
            {
                Register(go.name, go);
            }
        }
    }
}
