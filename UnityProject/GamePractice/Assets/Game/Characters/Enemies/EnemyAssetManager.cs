using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    public class EnemyAssetManager : AssetManagerBase<Enemy>
    {
        protected override async UniTask OnLoadAsync()
        {
            foreach (var go in await LoadAssetsByLabelAsync<GameObject>(AssetAddresses.EnemiesLabel))
            {
                if (!go.TryGetComponent<Enemy>(out var enemy))
                {
                    Debug.LogError($"EnemyAssetManager: {go.name} 에 Enemy 컴포넌트가 없다");
                    continue;
                }

                Register(go.name, enemy);
            }
        }
    }
}
