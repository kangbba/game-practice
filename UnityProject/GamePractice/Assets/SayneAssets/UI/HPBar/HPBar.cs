using R3;
using TMPro;
using UnityEngine;

namespace Sayne
{
    public class HPBar : MonoBehaviour
    {
        [SerializeField] private SlicedFillBar _frontFill;
        [SerializeField] private SlicedFillBar _backFill;

        /// <summary>"현재/최대" 절대수치 라벨. 안 꽂으면 바만 그린다.</summary>
        [SerializeField] private TMP_Text _label;

        private HPBarCore _core;

        protected HPBarCore Core => _core ??= new HPBarCore(_frontFill, _backFill, _label, HPBarStyle.Default);

        public float Value
        {
            get => Core.Ratio;
            set => Core.SetRatio(value);
        }

        public void Bind(ReadOnlyReactiveProperty<float> currentHP, ReadOnlyReactiveProperty<float> maxHP)
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
