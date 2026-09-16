using R3;

namespace Sayne
{
    /// <summary>타격하는 쪽. 어떤 타가 나갈지는 캐릭터가 사이클로 정한다.</summary>
    public interface IAttacker
    {
        CharacterCombat Combat { get; }
    }
}
