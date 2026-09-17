using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sayne
{
    /// <summary>
    /// UI 에 캐릭터를 비추는 작은 무대. 전장에서 멀리 떨어진 곳에 인형(히어로 프리팹 사본)을 세우고,
    /// 전용 카메라로 찍어 텍스처로 내놓는다. 인형은 Init 을 안 거친 빈 몸이라 싸우지도 움직이지도 않고,
    /// 무엇을 걸칠지는 바깥에서 Wear·TakeOff 로 알려준다. 프리팹이 오른쪽을 보고 있으므로 인형도 오른쪽을 본다.
    /// </summary>
    public class CharacterPreviewStage : IDisposable
    {
        private const int TextureSize = 512;

        /// <summary>전장의 카메라에 안 걸리는 먼 곳.</summary>
        private static readonly Vector3 StagePosition = new Vector3(0f, -500f, 0f);

        /// <summary>발끝이 원점인 몸을 화면 가운데로 올리는 카메라 자리.</summary>
        private static readonly Vector3 CameraOffset = new Vector3(0f, 1.1f, -10f);

        private const float CameraSize = 1.6f;

        private readonly GameObject _root;
        private readonly Camera _camera;
        private readonly RenderTexture _texture;

        private Hero _doll;
        private CharacterSkin _dollSkin;

        public Texture Texture => _texture;

        public CharacterPreviewStage()
        {
            _root = new GameObject("CharacterPreviewStage");
            _root.transform.position = StagePosition;

            _texture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32);

            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(_root.transform, false);
            cameraObject.transform.localPosition = CameraOffset;

            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = CameraSize;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.targetTexture = _texture;
            _camera.enabled = false;
        }

        /// <summary>창이 열려 있는 동안만 찍는다.</summary>
        public void SetActive(bool isActive)
        {
            _camera.enabled = isActive;
        }

        /// <summary>무대에 세울 몸을 바꾼다. 장비는 벗은 채로 서므로 이어서 Wear 로 입혀야 한다.</summary>
        public void SetDoll(Hero prefab)
        {
            if (_doll != null)
            {
                Object.Destroy(_doll.gameObject);
            }

            var doll = Object.Instantiate(prefab, _root.transform);
            _doll = doll;
            doll.name = "Doll";
            doll.transform.localPosition = Vector3.zero;

            // 전장 카메라를 따라 눕는 빌보드는 무대에선 필요 없다 — 무대 카메라는 정면에서 본다.
            var billboard = doll.GetComponentInChildren<Billboard>();
            billboard.enabled = false;
            billboard.transform.localRotation = Quaternion.identity;

            // 창이 열린 동안 게임은 멈춰 있다. 인형은 그 동안에도 숨을 쉰다.
            doll.GetComponentInChildren<Animator>().updateMode = AnimatorUpdateMode.UnscaledTime;

            _dollSkin = doll.GetComponentInChildren<CharacterSkin>();
        }

        /// <summary>그 자리에 이 그림을 걸친다. 그림이 없는 장비(맨손 등)는 벗은 것과 같다.</summary>
        public void Wear(EquipmentSlot slot, GameObject visual)
        {
            if (visual == null)
            {
                _dollSkin.TakeOff(slot);
                return;
            }

            _dollSkin.Wear(slot, visual);
        }

        public void Dispose()
        {
            Object.Destroy(_root);
            _texture.Release();
            Object.Destroy(_texture);
        }
    }
}
