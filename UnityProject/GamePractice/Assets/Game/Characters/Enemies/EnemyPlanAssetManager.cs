using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>EnemyPlans 라벨의 적 설계값을 로드한다. 열쇠는 파일명이 아니라 설계값이 스스로 밝히는 적 ID 다.</summary>
    public class EnemyPlanAssetManager : AssetManagerBase<EnemyPlan>
    {
        protected override async UniTask OnLoadAsync()
        {
            foreach (var plan in await LoadAssetsByLabelAsync<EnemyPlan>(AssetAddresses.EnemyPlansLabel))
            {
                if (string.IsNullOrEmpty(plan.EnemyID))
                {
                    Debug.LogError($"EnemyPlanAssetManager: {plan.name} 에 주인 적 ID 가 비어 있다");
                    continue;
                }

                Register(plan.EnemyID, plan);
            }
        }
    }
}
