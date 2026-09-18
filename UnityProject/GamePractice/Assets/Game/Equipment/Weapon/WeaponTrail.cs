using UnityEngine;

namespace Sayne
{
    /// <summary>무기 프리팹에 붙는 스윙 궤적. 휘두르는 동안만 그려서 모션과 어긋나지 않는다.</summary>
    public class WeaponTrail : MonoBehaviour
    {
        [SerializeField] private TrailRenderer _trail;

        private float _stopTime;

        /// <summary>지금부터 이 시간 동안 궤적을 그린다. 이전 획의 잔상은 지우고 시작한다. 꺼져 있으면 아무 일도 없다.</summary>
        public void Play(float seconds)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            _trail.Clear();
            _trail.emitting = true;
            _stopTime = Time.time + seconds;
        }

        private void Awake()
        {
            _trail.emitting = false;
        }

        private void Update()
        {
            if (_trail.emitting && Time.time >= _stopTime)
            {
                _trail.emitting = false;
            }
        }
    }
}
