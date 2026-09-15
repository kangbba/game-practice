using UnityEngine;

namespace Sayne
{
    public abstract class PhaseUIBase : MonoBehaviour
    {
        public abstract string PhaseKey { get; }

        public virtual void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }
    }
}
