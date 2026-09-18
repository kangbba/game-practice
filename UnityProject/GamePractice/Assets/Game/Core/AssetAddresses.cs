namespace Sayne
{
    /// <summary>Addressables 라벨·주소 선언 테이블. 에디터 셋업(Game > Setup Addressables)과 짝을 이룬다.</summary>
    public static class AssetAddresses
    {
        public const string HeroesLabel = "Heroes";
        public const string EnemiesLabel = "Enemies";
        public const string ParticlesLabel = "Particles";
        public const string MapsLabel = "Maps";
        public const string MainMap = "WorldMap";
        public const string EquipmentLabel = "Equipment";
        public const string ProfilesLabel = "Profiles";
        public const string EnemyPlansLabel = "EnemyPlans";
        public const string HeroPlansLabel = "HeroPlans";
        public const string EquipmentPlansLabel = "EquipmentPlans";

        public const string OverlayHPBar = "OverlayHPBar";
        public const string WorldHPBar = "WorldHPBar";
        public const string UltimateCutscenePanel = "UltimateCutscenePanel";
        public const string DamageText = "DamageText";
        public const string BattlePanel = "BattlePanel";
        public const string WaveStartPanel = "WaveStartPanel";
        public const string LowHealthPanel = "LowHealthPanel";
        /// <summary>월드에 떨어지는 구슬 프리팹.</summary>
        public const string DropOrb = "DropItem";
        public const string SpeechBubbleWidget = "SpeechBubbleWidget";
        public const string OverlaySpeechBubble = "OverlaySpeechBubble";
        public const string WorldSpeechBubble = "WorldSpeechBubble";

        // 팝업. 종류와의 짝은 PopupTypes.GetAddress 가 든다.
        public const string GrowthWindow = "GrowthWindow";
        public const string EquipmentWindow = "EquipmentWindow";

        /// <summary>골드 드랍 구슬에 들어가는 동전 그림.</summary>
        /// <summary>코인 그림. 에셋 파일명이 Gold 라 주소도 그대로다.</summary>
        public const string CoinIcon = "Gold";
    }
}
