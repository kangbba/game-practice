using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 플로팅 조이스틱의 화면 방향(XY)을 월드 이동 방향(XZ)으로 바꾼다.
    /// 조이스틱은 방향만 정한다 — 기울인 정도는 속도에 반영하지 않고 항상 길이 1로 넘긴다.
    /// 이동 속도는 캐릭터 스탯(MoveSpeed)이 정한다.
    /// </summary>
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
                return new Vector3(direction.x, 0f, direction.y).normalized;
            }
        }
    }
}
