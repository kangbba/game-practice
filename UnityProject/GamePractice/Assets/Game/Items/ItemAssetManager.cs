using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 아이템 자산. 지금은 월드에 뿌려지는 구슬 프리팹과 코인 그림 둘뿐이다.
    /// 장비 아이콘은 여기 없다 — 그건 장비 자기 것이라 EquipmentManager 가 안다.
    /// </summary>
    public class ItemAssetManager : AssetManagerBase<Object>
    {
        /// <summary>월드에 떨어지는 구슬. 무엇이 들었든 껍데기는 이 하나다.</summary>
        public DropItem DropOrbPrefab => ((GameObject)Get(AssetAddresses.DropOrb)).GetComponent<DropItem>();

        /// <summary>골드 구슬에 들어가는 그림.</summary>
        public Sprite CoinIcon => (Sprite)Get(AssetAddresses.CoinIcon);

        /// <summary>회복 구슬에 들어가는 그림.</summary>
        public Sprite HealIcon => (Sprite)Get(AssetAddresses.HealIcon);

        protected override async UniTask OnLoadAsync()
        {
            var (orb, coin, heal) = await UniTask.WhenAll(
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.DropOrb),
                LoadAssetByAddressAsync<Sprite>(AssetAddresses.CoinIcon),
                LoadAssetByAddressAsync<Sprite>(AssetAddresses.HealIcon));

            Register(AssetAddresses.DropOrb, orb);
            Register(AssetAddresses.CoinIcon, coin);
            Register(AssetAddresses.HealIcon, heal);
        }
    }
}
