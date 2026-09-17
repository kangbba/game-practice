using UnityEngine;

namespace Sayne
{
    /// <summary>수동 이동 입력원. IsActive 가 false 면 컨트롤러가 자동 행동으로 전환한다.</summary>
    public interface IMoveInputSource
    {
        bool IsActive { get; }
        Vector3 Direction { get; }
    }
}
