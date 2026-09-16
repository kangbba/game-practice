using UnityEngine;

namespace Sayne
{
    /// <summary>타겟으로 지정될 수 있음. 타겟 탐색이 이 인터페이스만 보고 동작한다.</summary>
    public interface ITargetable
    {
        Transform Transform { get; }
        Team Team { get; }
        bool IsTargetable { get; }
    }

    public enum Team
    {
        Ally,
        Enemy
    }
}
