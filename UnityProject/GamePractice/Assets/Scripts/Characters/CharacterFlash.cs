using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>피격 순간 스프라이트를 하얗게 번쩍인다.</summary>
    public class CharacterFlash : MonoBehaviour
    {
        private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

        private const float FlashDuration = 0.12f;

        private SpriteRenderer[] _renderers;
        private MaterialPropertyBlock _block;
        private float _remain;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _block = new MaterialPropertyBlock();

            GetComponentInParent<Character>().Damaged
                .Subscribe(this, (_, self) => self._remain = FlashDuration)
                .AddTo(this);
        }

        private void LateUpdate()
        {
            if (_remain <= 0f)
            {
                return;
            }

            _remain -= Time.deltaTime;
            Apply(Mathf.Clamp01(_remain / FlashDuration));
        }

        private void Apply(float amount)
        {
            foreach (var renderer in _renderers)
            {
                renderer.GetPropertyBlock(_block);
                _block.SetFloat(FlashAmount, amount);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
