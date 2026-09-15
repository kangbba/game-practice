using UnityEngine;
using UnityEngine.EventSystems;
namespace DarkFantasy2D
{
    [DefaultExecutionOrder(-100)]
    public sealed class ShowcaseInputSetup : MonoBehaviour
    {
        void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            var legacy = GetComponent<StandaloneInputModule>();
            if (legacy) { legacy.enabled = false; Destroy(legacy); }
            var module = GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (!module) module = gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            if (!GetComponent<StandaloneInputModule>()) gameObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
