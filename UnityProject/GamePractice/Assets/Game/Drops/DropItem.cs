using DG.Tweening;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 월드에 떨어진 전리품. 투명한 구슬 안에 그림 하나가 떠 있다.
    /// 이 그림이 무엇인지도, 누가 자기를 끌어가는지도 모른다 — 떨어져 튀고, 제자리에서 둥실거리기만 한다.
    /// 끌어당기는 것도 치우는 것도 만든 쪽(DropManager)의 일이다.
    /// </summary>
    public class DropItem : MonoBehaviour
    {
        private const float PopHeight = 1.2f;
        private const float PopSeconds = 0.5f;
        private const float BobHeight = 0.12f;
        private const float BobSeconds = 1.2f;

        /// <summary>
        /// 착지하고 이만큼 눈에 보인 뒤에야 주울 수 있다. 근접 영웅은 흡입 반경 안에서 적을 잡으므로,
        /// 이게 없으면 구슬이 튀어 오르기도 전에 빨려 들어가 떨어진 걸 볼 수가 없다.
        /// </summary>
        private const float SettleSeconds = 0.4f;

        /// <summary>구슬 안쪽에 그림이 들어갈 크기(월드 단위). 어떤 그림이든 이 안에 맞춰 줄인다.</summary>
        private const float PortraitSize = 0.55f;

        [SerializeField] private SpriteRenderer _portrait;

        /// <summary>떨어져 잠깐 눈에 보인 뒤인가. 그 전에는 주워지지 않는다.</summary>
        public bool IsSettled { get; private set; }

        /// <summary>구슬에 담을 그림. 원본 크기가 제각각이라 구슬 안에 맞게 줄여 넣는다.</summary>
        public void SetPortrait(Sprite portrait)
        {
            _portrait.sprite = portrait;
            _portrait.enabled = portrait != null;

            if (portrait == null)
            {
                return;
            }

            var size = portrait.bounds.size;
            var longest = Mathf.Max(size.x, size.y);
            var scale = longest > Mathf.Epsilon ? PortraitSize / longest : 1f;
            _portrait.transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>떨어진 자리에서 한 번 튀어 착지하고, 그 뒤로는 제자리에서 둥실거린다.</summary>
        public void Drop(Vector3 landing)
        {
            DOTween.Sequence()
                .Append(transform.DOJump(landing, PopHeight, 1, PopSeconds).SetEase(Ease.OutQuad))
                .AppendCallback(() => StartBobbing())
                .AppendInterval(SettleSeconds)
                .AppendCallback(() => IsSettled = true)
                .SetLink(gameObject);
        }

        /// <summary>제자리에서 둥실거린다. 아무도 끌어가지 않는 동안의 모습이다.</summary>
        public void StartBobbing()
        {
            transform.DOMoveY(transform.position.y + BobHeight, BobSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        /// <summary>둥실거림을 멈춘다. 끌려가는 동안 이게 살아 있으면 끌리는 경로와 싸운다.</summary>
        public void StopBobbing()
        {
            transform.DOKill();
        }
    }
}
