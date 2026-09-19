using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 게임 내내 쓰는 UI 프리팹 창고 — 팝업 창과 튜토리얼 말풍선(화면 아래 위젯·머리 위 말풍선). 전투에서만 쓰는 HUD·HP 바 등은 BattleUIAssetManager 의 것이다.
    /// 팝업만 테이블에 남는다 — 무엇을 쓸지 런타임에 PopupType 으로 고르기 때문이다.
    /// </summary>
    public class UIAssetManager : AssetManagerBase<GameObject>, IPopupAssets, ITutorialAssets
    {
        public SpeechBubbleWidget SpeechBubbleWidgetPrefab { get; private set; }
        public OverlaySpeechBubble OverlaySpeechBubblePrefab { get; private set; }

        /// <summary>그 종류에 짝지은 팝업 프리팹.</summary>
        public PopupWindow GetPopupPrefab(PopupType type)
        {
            return Get(PopupTypes.GetAddress(type)).GetComponent<PopupWindow>();
        }

        protected override async UniTask OnLoadAsync()
        {
            var (speechBubbleWidget, overlayBubble) = await UniTask.WhenAll(
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.SpeechBubbleWidget),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.OverlaySpeechBubble));

            SpeechBubbleWidgetPrefab = speechBubbleWidget.GetComponent<SpeechBubbleWidget>();
            OverlaySpeechBubblePrefab = overlayBubble.GetComponent<OverlaySpeechBubble>();

            foreach (var type in PopupTypes.All)
            {
                var address = PopupTypes.GetAddress(type);
                Register(address, await LoadAssetByAddressAsync<GameObject>(address));
            }
        }

        protected override void OnAssetRelease()
        {
            SpeechBubbleWidgetPrefab = null;
            OverlaySpeechBubblePrefab = null;
        }
    }
}
