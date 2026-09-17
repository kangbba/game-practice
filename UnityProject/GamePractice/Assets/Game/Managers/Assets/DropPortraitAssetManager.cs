using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>DropPortraits 라벨의 드랍 그림을 로드한다. 장비가 아닌 것(골드 같은)의 구슬 속 그림이 여기 있다.</summary>
    public class DropPortraitAssetManager : AssetManagerBase<Sprite>
    {
        protected override async UniTask OnLoadAsync()
        {
            foreach (var sprite in await LoadAssetsByLabelAsync<Sprite>(AssetAddresses.DropPortraitsLabel))
            {
                Register(sprite.name, sprite);
            }
        }
    }
}
