using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class MapAssetManager : AssetManagerBase<GameObject>
    {
        /// <summary>본편 맵. 맵이 늘면 이름을 가진 속성을 하나씩 더한다 — 문자열 ID 로 찾지 않는다.</summary>
        public GameObject MainMap => Get(AssetAddresses.MainMap);

        protected override async UniTask OnLoadAsync()
        {
            Register(AssetAddresses.MainMap, await LoadAssetByAddressAsync<GameObject>(AssetAddresses.MainMap));
        }
    }
}
