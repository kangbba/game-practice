using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>Heroes 라벨의 프리팹을 로드한다. 이름 = 히어로 ID. 기본 설계값은 HeroManager 가 코드로 들고 있다.</summary>
    public class HeroAssetManager : AssetManagerBase<Hero>
    {
        protected override async UniTask OnLoadAsync()
        {
            foreach (var go in await LoadAssetsByLabelAsync<GameObject>(AssetAddresses.HeroesLabel))
            {
                if (!go.TryGetComponent<Hero>(out var hero))
                {
                    Debug.LogError($"HeroAssetManager: {go.name} 에 Hero 컴포넌트가 없다");
                    continue;
                }

                Register(go.name, hero);
            }
        }
    }
}
