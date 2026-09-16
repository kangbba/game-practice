using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>히어로는 프리팹 + CharacterDefinition 두 테이블을 가진다. Get = 프리팹, GetDefinition = 정의.</summary>
    public interface IHeroAssets : IAssets<Hero>
    {
        CharacterDefinition GetDefinition(string heroID);
    }

    /// <summary>Heroes 라벨의 에셋(프리팹 + CharacterDefinition)을 로드한다. 이름 = 히어로 ID.</summary>
    public class HeroAssetManager : AssetManagerBase<Hero>, IHeroAssets
    {
        private readonly Dictionary<string, CharacterDefinition> _definitions = new Dictionary<string, CharacterDefinition>();

        protected override async UniTask OnLoadAsync(CancellationToken token)
        {
            foreach (var asset in await LoadAssetsByLabelAsync<Object>(AssetAddresses.HeroesLabel, token))
            {
                switch (asset)
                {
                    case GameObject go when go.TryGetComponent<Hero>(out var hero):
                        Register(go.name, hero);
                        break;
                    case CharacterDefinition definition:
                        _definitions[definition.name] = definition;
                        break;
                }
            }

            ValidatePairs();
        }

        public CharacterDefinition GetDefinition(string heroID)
        {
            if (!IsLoaded)
            {
                Debug.LogError($"HeroAssetManager: LoadAsync 가 끝나기 전에 GetDefinition({heroID}) 을 불렀다");
                return null;
            }

            if (!_definitions.TryGetValue(heroID, out var definition))
            {
                Debug.LogError($"HeroAssetManager: {heroID} 정의가 등록되지 않았다");
                return null;
            }

            return definition;
        }

        protected override void OnAssetRelease()
        {
            base.OnAssetRelease();
            _definitions.Clear();
        }

        private void ValidatePairs()
        {
            foreach (var id in IDs)
            {
                if (!_definitions.ContainsKey(id))
                {
                    Debug.LogError($"HeroAssetManager: {id} 프리팹만 있고 CharacterDefinition 이 없다");
                }
            }

            foreach (var id in _definitions.Keys)
            {
                var known = false;
                foreach (var prefabID in IDs)
                {
                    if (prefabID == id)
                    {
                        known = true;
                        break;
                    }
                }

                if (!known)
                {
                    Debug.LogWarning($"HeroAssetManager: {id} CharacterDefinition 만 있고 프리팹이 없다");
                }
            }
        }
    }
}
