using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>Equipment 라벨의 비주얼 프리팹을 로드한다. 이름 = 장비 ID. 수치는 EquipmentPlans 가 들고 있다.</summary>
    public class EquipmentAssetManager : AssetManagerBase<GameObject>
    {
        protected override async UniTask OnLoadAsync()
        {
            foreach (var prefab in await LoadAssetsByLabelAsync<GameObject>(AssetAddresses.EquipmentLabel))
            {
                Register(prefab.name, prefab);
            }
        }
    }
}
