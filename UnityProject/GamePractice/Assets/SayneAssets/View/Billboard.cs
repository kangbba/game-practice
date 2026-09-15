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
