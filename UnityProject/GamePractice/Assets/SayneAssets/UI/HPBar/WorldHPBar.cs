using R3;
using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 월드에 떠서 몸의 발밑을 따라다니는 HP 바. 월드 캔버스 하나에 모여 있고, 늘 카메라를 정면으로 본다.
    /// 월드에 있으니 멀면 작아지고 몸과 함께 화면에서 움직인다 — 스크린에 붙는 OverlayHPBar 와 다른 점이다.
    /// 무엇을 따라다닐지만 받고 그것만 한다 — 캐릭터도 죽음도 모르고, 치우는 건 만든 쪽 일이다.
    /// </summary>
    public sealed class WorldHPBar : MonoBehaviour
    {
        [SerializeField] private SlicedFillBar _frontFill;
        [SerializeField] private SlicedFillBar _backFill;

        /// <summary>"현재/최대" 절대수치 라벨. 안 꽂으면 바만 그린다.</summary>
        [SerializeField] private TMP_Text _label;

        private HPBarCore _core;

        private Transform _camera;
        private Transform _target;

        /// <summary>몸의 발에서 바까지의 월드 벡터.</summary>
        private Vector3 _worldOffset;

        private HPBarCore Core => _core ??= new HPBarCore(_frontFill, _backFill, _label, HPBarMotion.Default);

        /// <param name="scale">캔버스 1 단위(프리팹 픽셀)를 월드 몇 단위로 볼지. 바의 실제 크기가 이걸로 정해진다.</param>
        public void Attach(Camera camera, Transform target, Vector3 worldOffset, float scale,
            ReadOnlyReactiveProperty<float> currentHP, ReadOnlyReactiveProperty<float> maxHP)
        {
            _camera = camera.transform;
            _target = target;
            _worldOffset = worldOffset;
            transform.localScale = Vector3.one * scale;

            // 첫 프레임부터 제자리에 서 있어야 한다. 루프를 기다리면 한 프레임 동안 원점에 찍힌다.
            Place();

            Core.Bind(currentHP, maxHP);

            // 카메라와 같은 단계(PostLateUpdate)에서, 카메라보다 늦게 구독해 그 뒤에 돈다 —
            // 이 바는 카메라 회전을 그대로 베끼기 때문에 순서가 뒤바뀌면 지난 프레임 각도로 선다.
            Observable.EveryUpdate(UnityFrameProvider.PostLateUpdate)
                .Subscribe(this, (_, self) => self.Follow())
                .AddTo(this);
        }

        /// <summary>카메라가 자리를 잡은 뒤, 한 프레임분 몸을 따라간다.</summary>
        private void Follow()
        {
            Core.Advance(Time.deltaTime);
            Place();
        }

        private void Place()
        {
            transform.SetPositionAndRotation(_target.position + _worldOffset, _camera.rotation);
        }

        private void OnDestroy()
        {
            Core.Dispose();
        }
    }
}
