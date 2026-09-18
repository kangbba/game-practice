using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 게임오브젝트를 UI 위에 비추는 RawImage. 보여줄 프리팹만 주면 무대·카메라·렌더텍스처는 이 안에서 산다.
    /// 3D 를 2D 한 장처럼 다루게 해주는 부품이라, 받는 쪽은 그냥 그림 한 장으로 여기면 된다.
    ///
    /// 무대는 <b>보여줄 것이 생길 때 서고 치울 때 접힌다</b> — 유니티 수명(Awake·OnEnable)에 매달지 않는다.
    /// 그래서 꺼져 있는 창을 미리 채워 넣어도, 창을 열지 않고 지워도 아무 일이 없다. 어떻게 볼지(SetView·
    /// SetOrthographic·SetPerspective)는 언제 정해 주든 값으로 기억해 뒀다가 무대가 설 때 카메라에 옮긴다 —
    /// 무대가 아직 없다는 건 "안 보여줄 뿐"이지 잘못된 상태가 아니다.
    ///
    /// 무대는 전장에서 멀리 떨어진 곳에 선다. 여러 개가 동시에 떠 있어도 서로의 카메라에 옆 무대가 찍히지 않게
    /// 자리를 하나씩 잡고, 접을 때 그 자리를 반납한다 — 열고 닫기를 반복해도 무대가 점점 멀어지지 않는다.
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
        private const float DefaultFieldOfView = 60f;

        private GameObject _stage;
        private Camera _camera;
        private RenderTexture _texture;
        private GameObject _shown;

        /// <summary>
        /// 이 부품이 쓰는 무대 자리. 한 번 집으면 부품이 사라질 때까지 들고 있는다 —
        /// 무대를 접을 때 반납하면, 같은 프레임에 다시 세울 때 아직 안 지워진 옛 무대와 같은 자리를 집는다.
        /// </summary>
        private int _slot = -1;

        // 어떻게 볼지는 값으로만 들고 있다가 무대가 설 때 카메라에 옮긴다.
        private Vector3 _viewOffset = DefaultViewOffset;
        private Vector3 _viewAngles;
        private bool _isOrthographic = true;
        private float _orthographicSize = DefaultOrthographicSize;
        private float _fieldOfView = DefaultFieldOfView;

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

                if (_camera == null)
                {
                    return;
                }

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

        private void OnDestroy()
        {
            Clear();

            if (_slot >= 0)
            {
                UsedSlots.Remove(_slot);
            }
        }

        /// <summary>
        /// 무대에 세울 것을 바꾼다. 프리팹을 주면 사본을 만들어 세운다 — 원본은 건드리지 않는다.
        /// 무대가 아직 없으면 여기서 세운다. 앞서 세운 것은 이쪽이 치우고, 이 컴포넌트가 사라질 때도 같이 치운다.
        /// 돌려받은 사본에 옷을 입히거나 모션을 트는 건 부르는 쪽 몫이다.
        /// </summary>
        public GameObject Show(GameObject prefab)
        {
            // 인형만 갈아 세운다 — 무대까지 접었다 세우면 렌더텍스처를 부질없이 새로 만든다.
            if (_shown != null)
            {
                Destroy(_shown);
                _shown = null;
            }

            Raise();

            _shown = Instantiate(prefab, _stage.transform);
            _shown.transform.localPosition = Vector3.zero;

            // 멈춘 그림으로 쓰는 중이라면 세운 김에 한 장 담아 둔다.
            if (!_isLive)
            {
                Capture();
            }

            return _shown;
        }

        /// <summary>
        /// 아무것도 안 보여주는 상태로 돌아간다. 세운 사본도, 무대와 카메라와 렌더텍스처도 함께 치운다 —
        /// 안 쓰는 동안 카메라가 남아 있을 이유가 없다. 다시 Show 하면 무대는 그때 새로 선다.
        /// </summary>
        public void Clear()
        {
            if (_shown != null)
            {
                Destroy(_shown);
                _shown = null;
            }

            if (_stage == null)
            {
                return;
            }

            Destroy(_stage);
            _stage = null;
            _camera = null;

            // 그림을 먼저 떼고 반납한다 — 반납한 텍스처를 가리키고 있는 RawImage 는 분홍색으로 뜬다.
            // 오브젝트째 지워지는 길이면 RawImage 가 먼저 사라졌을 수 있다.
            var image = GetComponent<RawImage>();
            if (image != null)
            {
                image.texture = null;
            }

            _texture.Release();
            Destroy(_texture);
            _texture = null;
        }

        /// <summary>
        /// 지금 무대를 한 장 담는다. 멈춘 그림으로 쓸 때, 세운 것을 손본 뒤 다시 부르면 그 모습으로 갱신된다.
        /// 살아 움직이는 중이라면 어차피 매 프레임 담기므로 부를 일이 없다. 무대가 없으면 담을 것도 없다.
        /// </summary>
        public void Capture()
        {
            if (_camera == null)
            {
                return;
            }

            _camera.Render();
        }

        /// <summary>카메라가 어디서 어느 쪽을 보는가. 담는 것마다 키와 보고 싶은 각도가 달라 밖에서 정한다.</summary>
        public void SetView(Vector3 offset, Vector3 eulerAngles)
        {
            _viewOffset = offset;
            _viewAngles = eulerAngles;
            ApplyView();
        }

        /// <summary>원근 없이 담는다. 초상처럼 형태만 보여줄 때 — 기본값이다.</summary>
        public void SetOrthographic(float size)
        {
            _isOrthographic = true;
            _orthographicSize = size;
            ApplyView();
        }

        /// <summary>원근을 살려 담는다. 물건을 돌려 보여줄 때처럼 입체가 느껴져야 할 때.</summary>
        public void SetPerspective(float fieldOfView)
        {
            _isOrthographic = false;
            _fieldOfView = fieldOfView;
            ApplyView();
        }

        /// <summary>무대를 세운다. 이미 서 있으면 그대로 쓴다.</summary>
        private void Raise()
        {
            if (_stage != null)
            {
                return;
            }

            if (_slot < 0)
            {
                _slot = TakeSlot();
            }

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

            ApplyView();

            GetComponent<RawImage>().texture = _texture;
        }

        /// <summary>정해 둔 시점을 카메라에 옮긴다. 무대가 아직 없으면 설 때 옮겨진다.</summary>
        private void ApplyView()
        {
            if (_camera == null)
            {
                return;
            }

            _camera.transform.localPosition = _viewOffset;
            _camera.transform.localEulerAngles = _viewAngles;

            if (_isOrthographic)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = _orthographicSize;
            }
            else
            {
                _camera.orthographic = false;
                _camera.fieldOfView = _fieldOfView;
            }
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
