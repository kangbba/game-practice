using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>HeroData 라벨의 히어로 설계값을 로드한다. 열쇠는 파일명이 아니라 설계값이 스스로 밝히는 히어로 ID 다.</summary>
    public class HeroDataAssetManager : AssetManagerBase<HeroData>
    {
        protected override async UniTask OnLoadAsync()
        {
            foreach (var data in await LoadAssetsByLabelAsync<HeroData>(AssetAddresses.HeroDataLabel))
            {
                if (string.IsNullOrEmpty(data.HeroID))
                {
                    Debug.LogError($"HeroDataAssetManager: {data.name} 에 주인 히어로 ID 가 비어 있다");
                    continue;
                }

                Register(data.HeroID, data);
            }
        }
    }
}
