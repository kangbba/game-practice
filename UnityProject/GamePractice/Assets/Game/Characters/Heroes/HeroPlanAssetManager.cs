using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>HeroPlans 라벨의 히어로 설계값을 로드한다. 열쇠는 파일명이 아니라 설계값이 스스로 밝히는 히어로 ID 다.</summary>
    public class HeroPlanAssetManager : AssetManagerBase<HeroPlan>
    {
        protected override async UniTask OnLoadAsync()
        {
            foreach (var plan in await LoadAssetsByLabelAsync<HeroPlan>(AssetAddresses.HeroPlansLabel))
            {
                if (string.IsNullOrEmpty(plan.HeroID))
                {
                    Debug.LogError($"HeroPlanAssetManager: {plan.name} 에 주인 히어로 ID 가 비어 있다");
                    continue;
                }

                Register(plan.HeroID, plan);
            }
        }
    }
}
