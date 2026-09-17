using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class ParticleAssetManager : AssetManagerBase<GameObject>
    {
        protected override async UniTask OnLoadAsync()
        {
            foreach (var go in await LoadAssetsByLabelAsync<GameObject>(AssetAddresses.ParticlesLabel))
            {
                Register(go.name, go);
            }
        }
    }
}
