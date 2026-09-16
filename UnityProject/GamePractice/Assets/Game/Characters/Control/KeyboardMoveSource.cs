using UnityEngine;
using UnityEngine.InputSystem;

namespace Sayne
{
    public class KeyboardMoveSource : IMoveInputSource
    {
        public bool IsActive => Direction != Vector3.zero;

        public Vector3 Direction
        {
            get
            {
                var keyboard = Keyboard.current;
                if (keyboard == null)
                {
                    return Vector3.zero;
                }

                var direction = Vector3.zero;

                if (keyboard.upArrowKey.isPressed) direction.z += 1f;
                if (keyboard.downArrowKey.isPressed) direction.z -= 1f;
                if (keyboard.rightArrowKey.isPressed) direction.x += 1f;
                if (keyboard.leftArrowKey.isPressed) direction.x -= 1f;

                return direction;
            }
        }
    }
}
