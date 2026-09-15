using UnityEngine;
namespace DarkFantasy2D
{
    public sealed class EffectLifetime : MonoBehaviour
    {
        [Min(.1f)] public float seconds = 2.8f;
        void Start() { Destroy(gameObject, seconds); }
    }
}
