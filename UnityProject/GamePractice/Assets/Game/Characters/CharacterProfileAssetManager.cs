using Cysharp.Threading.Tasks;

namespace Sayne
{
    /// <summary>Profiles 라벨의 캐릭터 프로필을 로드한다. 에셋 이름 = 캐릭터 ID.</summary>
    public class CharacterProfileAssetManager : AssetManagerBase<CharacterProfile>
    {
        protected override async UniTask OnLoadAsync()
        {
            foreach (var profile in await LoadAssetsByLabelAsync<CharacterProfile>(AssetAddresses.ProfilesLabel))
            {
                Register(profile.name, profile);
            }
        }
    }
}
