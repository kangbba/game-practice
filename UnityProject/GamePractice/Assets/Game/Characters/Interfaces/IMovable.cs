using UnityEngine;

namespace Sayne
{
    public interface IMovable
    {
        void Move(Vector3 direction);
        void StopMove();
    }
}
