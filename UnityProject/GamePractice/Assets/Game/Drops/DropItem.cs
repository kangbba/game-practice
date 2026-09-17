using DG.Tweening;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 월드에 떨어진 전리품. 투명한 구슬 안에 그림 하나가 떠 있다.
    /// 이 그림이 무엇인지는 알지 않는다 — 주입받은 스프라이트를 띄우고, 튀고, 빨려 들어가는 연출만 한다.
    /// </summary>
    public class DropItem : MonoBehaviour
    {
        private const float PopHeight = 1.2f;
        private const float PopSeconds = 0.5f;
        private const float BobHeight = 0.12f;
        private const float BobSeconds = 1.2f;

        /// <summary>구슬 안쪽에 그림이 들어갈 크기(월드 단위). 어떤 초상화든 이 안에 맞춰 줄인다.</summary>
        private const float PortraitSize = 0.55f;

        /// <summary>빨려가는 속도. 가까울수록 빨라져 착 달라붙는 느낌이 난다.</summary>
        private const float AbsorbSpeed = 9f;
        private const float AbsorbAcceleration = 24f;
        private const float AbsorbReachDistance = 0.35f;

        [SerializeField] private SpriteRenderer _portrait;

        private readonly Subject<DropItem> _absorbed = new Subject<DropItem>();

        private Transform _magnet;
        private float _currentAbsorbSpeed;

        /// <summary>대상에게 닿았다. 무엇을 준다는 판단은 이걸 받는 쪽이 한다.</summary>
        public Observable<DropItem> Absorbed => _absorbed;

        /// <summary>이미 누군가에게 빨려가는 중인가. 흡수는 한 번뿐이다.</summary>
        public bool IsAbsorbing => _magnet != null;

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
                .AppendCallback(() => Bob())
                .SetLink(gameObject);
        }

        /// <summary>이 대상에게 빨려간다. 닿으면 Absorbed 를 흘리고 사라진다.</summary>
        public void Absorb(Transform target)
        {
            if (IsAbsorbing)
            {
                return;
            }

            _magnet = target;
            _currentAbsorbSpeed = AbsorbSpeed;

            // 둥실거림이 살아 있으면 흡입 경로와 싸운다.
            transform.DOKill();
        }

        private void Bob()
        {
            transform.DOMoveY(transform.position.y + BobHeight, BobSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        private void Update()
        {
            if (!IsAbsorbing)
            {
                return;
            }

            // 대상이 먼저 사라지면(죽거나 디스폰) 흡입을 멈추고 그 자리에 남는다.
            if (_magnet == null)
            {
                _magnet = null;
                Bob();
                return;
            }

            _currentAbsorbSpeed += AbsorbAcceleration * Time.deltaTime;

            var target = _magnet.position;
            transform.position = Vector3.MoveTowards(transform.position, target, _currentAbsorbSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) <= AbsorbReachDistance)
            {
                _absorbed.OnNext(this);
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            _absorbed.Dispose();
        }
    }
}
