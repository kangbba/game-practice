using System;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 날아가는 타격 한 발. 쏜 자리에서 목표까지 날아가고, 닿으면 도착을 알린 뒤 사라진다.
    /// 피해는 여기서 안 굴린다 — 도착했다고 알릴 뿐이고, 누구를 얼마나 때리는지는 쏜 쪽이 정한다.
    /// 그래서 원거리 무기는 "때리는 순간"이 뒤로 밀릴 뿐, 판정 규칙은 근접과 똑같이 굴러간다.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        /// <summary>초당 날아가는 거리. 사거리 5짜리 지팡이면 대략 0.4초 만에 닿는다.</summary>
        [SerializeField] private float _speed = 14f;

        /// <summary>날아간 자리에 남는 궤적. 투사체는 알갱이가 작아서 이게 없으면 눈으로 못 쫓는다.</summary>
        [SerializeField] private TrailRenderer _trail;

        /// <summary>목표의 발밑이 아니라 가슴께로 날아간다. 타격 이펙트가 터지는 높이와 같다.</summary>
        private const float ChestHeight = 0.9f;

        /// <summary>목표를 잃고도 영영 날아다니지 않게 두는 수명(초).</summary>
        private const float MaxLifetime = 3f;

        private Transform _target;
        private Vector3 _destination;
        private Action _arrived;

        /// <summary>어느 정렬 레이어에 그릴지. 궁극기 무대에서 쏜 건 배경 위(UltimateEffect)에 그려야 보인다.</summary>
        public void SetSortingLayer(string layer)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.sortingLayerName = layer;
            }
        }

        /// <summary>쏜다. 도착하면 arrived 가 불린다 — 날아가는 동안 목표가 죽었어도 도착은 한다.</summary>
        public void Launch(Vector3 from, Transform target, Action arrived)
        {
            transform.position = from;

            // 만들어진 자리(원점)에서 쏘는 자리까지 선이 그어지지 않게 첫 프레임에 한 번 지운다.
            _trail.Clear();

            _target = target;
            _destination = ChestPointOf(target);
            _arrived = arrived;

            Destroy(gameObject, MaxLifetime);
        }

        private void Update()
        {
            // 목표가 죽어 사라졌으면 마지막으로 본 자리까지 마저 날아간다. 허공에서 멈추는 것보다 자연스럽다.
            if (_target != null)
            {
                _destination = ChestPointOf(_target);
            }

            var offset = _destination - transform.position;
            var step = _speed * Time.deltaTime;

            if (offset.magnitude <= step)
            {
                Arrive();
                return;
            }

            transform.position += offset.normalized * step;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
        }

        /// <summary>도착 통보는 한 번뿐이다 — 바로 지워지니 다음 Update 가 오지 않는다.</summary>
        private void Arrive()
        {
            Destroy(gameObject);

            _arrived();
        }

        private static Vector3 ChestPointOf(Transform target)
        {
            return target.position + new Vector3(0f, ChestHeight, 0f);
        }
    }
}
