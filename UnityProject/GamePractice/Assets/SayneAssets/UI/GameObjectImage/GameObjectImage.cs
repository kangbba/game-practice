using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 게임오브젝트를 UI 위에 비추는 RawImage. 보여줄 프리팹만 주면 무대·카메라·렌더텍스처는 이 안에서 산다.
    /// 3D 를 2D 한 장처럼 다루게 해주는 부품이라, 받는 쪽은 그냥 그림 한 장으로 여기면 된다.
    ///
    /// 붙이기만 해도 선다 — 카메라는 기본값으로 이미 잡혀 있고, 손볼 일이 있을 때만 SetView·SetOrthographic·
    /// SetPerspective 로 따로 정한다.
    ///
    /// 무대는 전장에서 멀리 떨어진 곳에 선다. 여러 개가 동시에 떠 있어도 서로의 카메라에 옆 무대가 찍히지 않게
    /// 자리를 하나씩 잡고, 사라질 때 그 자리를 반납한다 — 열고 닫기를 반복해도 무대가 점점 멀어지지 않는다.
    ///
    /// 렌더텍스처와 무대에 세운 사본은 전부 이 컴포넌트의 수명에 묶인다. 밖에서 반납을 잊어 새는 일이 없다.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class GameObjectImage : MonoBehaviour
    {
        private const int TextureSize = 512;

        /// <summary>전장의 카메라에 안 걸리는 먼 곳.</summary>
        private static readonly Vector3 StageOrigin = new Vector3(0f, -500f, 0f);

        /// <summary>무대끼리의 간격. 옆 무대의 몸이 이 카메라에 들어오지 않을 만큼 띄운다.</summary>
        private const float StageSpacing = 50f;

        /// <summary>지금 쓰이고 있는 무대 자리. 반납된 자리는 다음 무대가 다시 쓴다.</summary>
        private static readonly HashSet<int> UsedSlots = new HashSet<int>();

        /// <summary>발끝이 원점인 몸을 화면 가운데에 담는 자리. 사람 크기를 기준으로 잡은 값이다.</summary>
        private static readonly Vector3 DefaultViewOffset = new Vector3(0f, 1.1f, -10f);
        private const float DefaultOrthographicSize = 1.6f;

        private RawImage _image;
        private GameObject _stage;
        private Camera _camera;
        private RenderTexture _texture;
        private GameObject _shown;
        private int _slot;

        private bool _isLive = true;

        /// <summary>
        /// 살아 움직이는가. 참이면 담긴 것이 움직이는 대로 계속 비친다 — 애니메이션이든 회전이든 그대로 보인다.
        /// 거짓이면 끄는 순간의 한 장이 그대로 남는다. 몸은 무대에 그대로 서 있고 그림만 멈춘 것이라,
        /// 다시 켜면 이어서 움직인다.
        /// </summary>
        public bool IsLive
        {
            get => _isLive;
            set
            {
                _isLive = value;
                _camera.enabled = value;

                // 멈추는 순간을 담아야 한다. 이 한 번이 없으면 직전 프레임이 아니라 훨씬 예전 그림이 남는다.
                if (!value)
                {
                    Capture();
                }
            }
        }

        /// <summary>지금 무대에 서 있는 사본. 아무것도 안 세웠으면 null.</summary>
        public GameObject Shown => _shown;

        private void Awake()
        {
            _image = GetComponent<RawImage>();
            _slot = TakeSlot();

            _stage = new GameObject($"{name} Stage");
            _stage.transform.position = StageOrigin + Vector3.right * StageSpacing * _slot;

            _texture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32);

            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(_stage.transform, false);

            _camera = cameraObject.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.targetTexture = _texture;
            _camera.enabled = _isLive;

            SetView(DefaultViewOffset, Vector3.zero);
            SetOrthographic(DefaultOrthographicSize);

            _image.texture = _texture;
        }

        private void OnDestroy()
        {
            Destroy(_stage);

            // 그림을 먼저 떼고 반납한다 — 반납한 텍스처를 가리키고 있는 RawImage 는 분홍색으로 뜬다.
            _image.texture = null;
            _texture.Release();
            Destroy(_texture);

            UsedSlots.Remove(_slot);
        }

        /// <summary>
        /// 무대에 세울 것을 바꾼다. 프리팹을 주면 사본을 만들어 세운다 — 원본은 건드리지 않는다.
        /// 앞서 세운 것은 이쪽이 치우고, 이 컴포넌트가 사라질 때도 같이 치운다. 밖에서 부수지 않는다.
        /// 돌려받은 사본에 옷을 입히거나 모션을 트는 건 부르는 쪽 몫이다.
        /// </summary>
        public GameObject Show(GameObject prefab)
        {
            Clear();

            _shown = Instantiate(prefab, _stage.transform);
            _shown.transform.localPosition = Vector3.zero;

            // 멈춘 그림으로 쓰는 중이라면 세운 김에 한 장 담아 둔다.
            if (!_isLive)
            {
                Capture();
            }

            return _shown;
        }

        /// <summary>무대를 비운다. 담겨 있던 그림은 지워지지 않는다 — 멈춰 있었다면 마지막 장이 그대로 남는다.</summary>
        public void Clear()
        {
            if (_shown == null)
            {
                return;
            }

            Destroy(_shown);
            _shown = null;
        }

        /// <summary>
        /// 지금 무대를 한 장 담는다. 멈춘 그림으로 쓸 때, 세운 것을 손본 뒤 다시 부르면 그 모습으로 갱신된다.
        /// 살아 움직이는 중이라면 어차피 매 프레임 담기므로 부를 일이 없다.
        /// </summary>
        public void Capture()
        {
            _camera.Render();
        }

        /// <summary>카메라가 어디서 어느 쪽을 보는가. 담는 것마다 키와 보고 싶은 각도가 달라 밖에서 정한다.</summary>
        public void SetView(Vector3 offset, Vector3 eulerAngles)
        {
            _camera.transform.localPosition = offset;
            _camera.transform.localEulerAngles = eulerAngles;
        }

        /// <summary>원근 없이 담는다. 초상처럼 형태만 보여줄 때 — 기본값이다.</summary>
        public void SetOrthographic(float size)
        {
            _camera.orthographic = true;
            _camera.orthographicSize = size;
        }

        /// <summary>원근을 살려 담는다. 물건을 돌려 보여줄 때처럼 입체가 느껴져야 할 때.</summary>
        public void SetPerspective(float fieldOfView)
        {
            _camera.orthographic = false;
            _camera.fieldOfView = fieldOfView;
        }

        /// <summary>제일 작은 빈 자리를 집는다. 반납된 자리가 있으면 그걸 다시 쓴다.</summary>
        private static int TakeSlot()
        {
            var slot = 0;

            while (!UsedSlots.Add(slot))
            {
                slot++;
            }

            return slot;
        }
    }
}
