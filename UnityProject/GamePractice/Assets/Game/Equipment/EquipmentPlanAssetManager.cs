using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>EquipmentPlans 라벨의 장비 설계값을 로드한다. 열쇠는 파일명이 아니라 설계값이 스스로 밝히는 장비 ID 다.</summary>
    public class EquipmentPlanAssetManager : AssetManagerBase<EquipmentPlan>
    {
        protected override async UniTask OnLoadAsync()
        {
            foreach (var plan in await LoadAssetsByLabelAsync<EquipmentPlan>(AssetAddresses.EquipmentPlansLabel))
            {
                if (string.IsNullOrEmpty(plan.EquipmentID))
                {
                    Debug.LogError($"EquipmentPlanAssetManager: {plan.name} 에 주인 장비 ID 가 비어 있다");
                    continue;
                }

                Register(plan.EquipmentID, plan);
            }
        }
    }
}
