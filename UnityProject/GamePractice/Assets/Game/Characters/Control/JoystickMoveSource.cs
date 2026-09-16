using UnityEngine;

namespace Sayne
{
    /// <summary>플로팅 조이스틱의 화면 방향(XY)을 월드 이동 방향(XZ)으로 바꾼다.</summary>
    public class JoystickMoveSource : IMoveInputSource
    {
        private readonly FloatingJoystick _joystick;

        public JoystickMoveSource(FloatingJoystick joystick)
        {
            _joystick = joystick;
        }

        public bool IsActive => _joystick != null && _joystick.IsActive;

        public Vector3 Direction
        {
            get
            {
                if (_joystick == null)
                {
                    return Vector3.zero;
                }

                var direction = _joystick.Direction;
                return new Vector3(direction.x, 0f, direction.y);
            }
        }
    }
}
