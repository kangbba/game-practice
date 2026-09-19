using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    public class Billboard : MonoBehaviour
    {
        private static readonly List<Billboard> _actives = new List<Billboard>();
        private static readonly Subject<Billboard> _registered = new Subject<Billboard>();

        public static IReadOnlyList<Billboard> Actives => _actives;
        public static Observable<Billboard> Registered => _registered;

        /// <summary>플레이를 시작할 때마다 목록을 비운다. 도메인 리로드를 끈 채 다시 플레이해도 앞 판의 빌보드가 남지 않는다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _actives.Clear();
        }

        private void OnEnable()
        {
            _actives.Add(this);
            _registered.OnNext(this);
        }

        private void OnDisable()
        {
            _actives.Remove(this);
        }

        public void Apply(Quaternion rotation)
        {
            transform.rotation = rotation;
        }
    }
}
