namespace Sayne
{
    /// <summary>
    /// 게임 내내 사는 매니저와 에셋을 한데 묶은 것. GameManager 가 다 세운 뒤 한 번 만들어 전투 쪽에 넘긴다.
    ///
    /// 절충이다. 받는 쪽은 UI(HUD·창)와 연출 대본(튜토리얼 디렉터), 그리고 조립처(BattleScope)뿐이다 —
    /// 이들은 여러 시스템을 두루 보여 주고, 개발 중에 보는 것이 자주 늘며, 아무도 이들에게 기대지 않아서 숨은 의존이 싸다.
    /// 게임 규칙을 가진 매니저는 이걸 받지 않고 필요한 것만 하나씩 받는다 — 누가 누구를 아는지가 생성자에 드러나야
    /// 역방향 의존(오래 사는 쪽이 전투를 붙드는 것)을 잡을 수 있다.
    /// </summary>
    public sealed class GameContext
    {
        public GameAssets Assets { get; }

        public EquipmentManager EquipmentManager { get; }
        public PopupManager PopupManager { get; }
        public TutorialManager TutorialManager { get; }

        public CurrencyManager CurrencyManager { get; }
        public GrowthManager GrowthManager { get; }
        public PartyManager PartyManager { get; }
        public RecordManager RecordManager { get; }
        public QuestManager QuestManager { get; }

        public GameContext(GameAssets assets, EquipmentManager equipmentManager, PopupManager popupManager,
            TutorialManager tutorialManager, CurrencyManager currencyManager, GrowthManager growthManager,
            PartyManager partyManager, RecordManager recordManager, QuestManager questManager)
        {
            Assets = assets;
            EquipmentManager = equipmentManager;
            PopupManager = popupManager;
            TutorialManager = tutorialManager;
            CurrencyManager = currencyManager;
            GrowthManager = growthManager;
            PartyManager = partyManager;
            RecordManager = recordManager;
            QuestManager = questManager;
        }
    }
}
