using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sayne
{
    /// <summary>
    /// 장비 아이콘을 찍는 작은 무대. 아이콘을 따로 그려 두지 않는다 — 캐릭터가 실제로 걸치는 장비 프리팹을
    /// 전장에서 멀리 떨어진 곳에 세우고, 전용 카메라로 같은 방식으로 찍어 낸다. 그래서 어떤 장비든 아이콘이 실물과 같고,
    /// 크기·여백·각도가 전부 한 규칙으로 맞는다. 장비 그림은 안 변하므로 한 번 찍은 것은 찍어 달라는 쪽이 들고 있으면 된다.
    /// </summary>
    public class EquipmentIconStage : IDisposable
    {
        private const int TextureSize = 256;

        /// <summary>전장의 카메라에도, 캐릭터 프리뷰 무대에도 안 걸리는 먼 곳.</summary>
        private static readonly Vector3 StagePosition = new Vector3(0f, -600f, 0f);

        /// <summary>장비가 칸을 꽉 채우지 않게 두는 여백. 1 = 여백 없음.</summary>
        private const float Padding = 1.12f;

        /// <summary>이보다 길쭉한 장비(칼·지팡이·활)는 비스듬히 눕혀 찍는다. 세워 찍으면 네모 칸에서 실처럼 가늘어진다.</summary>
        private const float SlantAspect = 2f;

        private const float SlantAngle = -45f;

        private readonly GameObject _root;
        private readonly Camera _camera;
        private readonly RenderTexture _target;

        public EquipmentIconStage()
        {
            _root = new GameObject("EquipmentIconStage");
            _root.transform.position = StagePosition;

            _target = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32);

            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(_root.transform, false);

            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.targetTexture = _target;

            // 찍어 달라고 할 때만 한 장씩 찍는다.
            _camera.enabled = false;
        }

        /// <summary>장비 프리팹을 세워 한 장 찍는다. 돌려주는 스프라이트와 그 텍스처는 받은 쪽이 치운다.</summary>
        public Sprite Shoot(GameObject visual)
        {
            var model = Object.Instantiate(visual, _root.transform);
            model.transform.localPosition = Vector3.zero;

            // 휘두를 때만 나오는 궤적은 장비의 생김새가 아니다.
            foreach (var trail in model.GetComponentsInChildren<WeaponTrail>(true))
            {
                trail.gameObject.SetActive(false);
            }

            // 신발처럼 자리마다 다른 그림을 자식으로 나눠 든 장비는 첫 그림 하나로 대표한다 — 전부 켜면 한자리에 겹친다.
            if (model.GetComponent<SpriteRenderer>() == null)
            {
                for (var i = 1; i < model.transform.childCount; i++)
                {
                    model.transform.GetChild(i).gameObject.SetActive(false);
                }
            }

            var bounds = BoundsOf(model);

            if (bounds.size.y > bounds.size.x * SlantAspect)
            {
                model.transform.RotateAround(bounds.center, Vector3.forward, SlantAngle);
                bounds = BoundsOf(model);
            }

            _camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - 10f);
            _camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * Padding;
            _camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = _target;
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, TextureSize, TextureSize), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;

            // 치우는 건 프레임 끝이라, 그 사이 다음 장비를 찍어도 같이 안 찍히게 먼저 꺼 둔다.
            model.SetActive(false);
            Kill(model);

            return Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), Vector2.one * .5f);
        }

        public void Dispose()
        {
            // 게임이 끝나 씬이 내려갈 때는 유니티가 먼저 무대를 치운다. 그때는 이미 없는 것을 또 치우지 않는다.
            if (_camera != null)
            {
                _camera.targetTexture = null;
            }

            Kill(_root);

            _target.Release();
            Kill(_target);
        }

        private static Bounds BoundsOf(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<SpriteRenderer>();
            var bounds = renderers[0].bounds;

            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        /// <summary>에디터 미리보기도 같은 무대로 찍는다. 플레이 중이 아니면 Destroy 를 못 쓴다.</summary>
        private static void Kill(Object target)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
