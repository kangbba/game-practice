using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 전투에서만 쓰는 UI 프리팹 창고 — 전투 HUD, 캐릭터별 HP 바·데미지 숫자, 화면 연출 패널.
    /// 전투 에셋(BattleAssets)과 함께 로드되고 전투가 끝나면 같이 놓인다. 로드 때 컴포넌트까지 뽑아 둔다.
    /// </summary>
    public class BattleUIAssetManager : AssetManagerBase<GameObject>, ICharacterUIAssets, IScreenPerformanceAssets
    {
        public BattlePanel BattlePanelPrefab { get; private set; }
        public OverlayHPBar HeroOverlayHPBarPrefab { get; private set; }
        public OverlayHPBar BossOverlayHPBarPrefab { get; private set; }
        public WorldHPBar EnemyWorldHPBarPrefab { get; private set; }
        public DamageText DamageTextPrefab { get; private set; }
        public WaveStartPanel WaveStartPanelPrefab { get; private set; }
        public LowHealthPanel LowHealthPanelPrefab { get; private set; }
        public UltimateCutscenePanel UltimateCutscenePanelPrefab { get; private set; }

        protected override async UniTask OnLoadAsync()
        {
            var (battlePanel, damageText, waveStartPanel, lowHealthPanel, cutscenePanel) = await UniTask.WhenAll(
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.BattlePanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.DamageText),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.WaveStartPanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.LowHealthPanel),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.UltimateCutscenePanel));

            BattlePanelPrefab = battlePanel.GetComponent<BattlePanel>();
            DamageTextPrefab = damageText.GetComponent<DamageText>();
            WaveStartPanelPrefab = waveStartPanel.GetComponent<WaveStartPanel>();
            LowHealthPanelPrefab = lowHealthPanel.GetComponent<LowHealthPanel>();
            UltimateCutscenePanelPrefab = cutscenePanel.GetComponent<UltimateCutscenePanel>();

            var (heroHPBar, bossHPBar, enemyHPBar) = await UniTask.WhenAll(
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.HeroOverlayHPBar),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.BossOverlayHPBar),
                LoadAssetByAddressAsync<GameObject>(AssetAddresses.EnemyWorldHPBar));

            HeroOverlayHPBarPrefab = heroHPBar.GetComponent<OverlayHPBar>();
            BossOverlayHPBarPrefab = bossHPBar.GetComponent<OverlayHPBar>();
            EnemyWorldHPBarPrefab = enemyHPBar.GetComponent<WorldHPBar>();
        }

        protected override void OnAssetRelease()
        {
            BattlePanelPrefab = null;
            HeroOverlayHPBarPrefab = null;
            BossOverlayHPBarPrefab = null;
            EnemyWorldHPBarPrefab = null;
            DamageTextPrefab = null;
            WaveStartPanelPrefab = null;
            LowHealthPanelPrefab = null;
            UltimateCutscenePanelPrefab = null;
        }
    }
}
