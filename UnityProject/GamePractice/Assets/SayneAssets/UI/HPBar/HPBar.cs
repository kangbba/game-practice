using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    public class HPBar : MonoBehaviour
    {
        [SerializeField] private Image _frontFill;
        [SerializeField] private Image _backFill;

        private HPBarCore _core;

        protected HPBarCore Core => _core ??= new HPBarCore(_frontFill, _backFill, HPBarStyle.Default);

        public float Value
        {
            get => Core.Ratio;
            set => Core.SetRatio(value);
        }

        public void Bind(ReadOnlyReactiveProperty<float> currentHP, float maxHP)
        {
            Core.Bind(currentHP, maxHP);
        }

        public void SetStyle(HPBarStyle style)
        {
            Core.SetStyle(style);
        }

        protected virtual void LateUpdate()
        {
            Core.Tick(Time.deltaTime);
        }

        protected virtual void OnDestroy()
        {
            Core.Dispose();
        }
    }
}
