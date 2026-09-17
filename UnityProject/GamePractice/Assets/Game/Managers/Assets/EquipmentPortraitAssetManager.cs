using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>EquipmentPortraits 라벨의 장비 초상화를 로드한다. 에셋 이름 = 장비 ID.</summary>
    public class EquipmentPortraitAssetManager : AssetManagerBase<Sprite>
    {
        protected override async UniTask OnLoadAsync()
        {
            foreach (var sprite in await LoadAssetsByLabelAsync<Sprite>(AssetAddresses.EquipmentPortraitsLabel))
            {
                Register(sprite.name, sprite);
            }
        }
    }
}
