using R3;
using UnityEngine;

namespace Sayne
{
    public interface IMovable
    {
        ReadOnlyReactiveProperty<float> CurrentSpeed { get; }

        void Move(Vector3 direction);
        void StopMove();
    }
}
