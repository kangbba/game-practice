using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 카메라 자식으로 붙어 화면을 꽉 채우는 배경 카드. 카메라를 그대로 따라다니니 무한히 먼 하늘처럼 보인다.
    /// 로컬 회전 0 이면 카메라 기울기를 물려받아 화면과 평행해지므로, 그라디언트가 화면 기준 세로로 선다.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SkyCard : MonoBehaviour
    {
        /// <summary>카메라 앞 거리. 월드의 어떤 것보다 멀면 된다.</summary>
        [SerializeField] private float _distance = 60f;

        /// <summary>화면 밖으로 조금 넘치게 두는 여유. 해상도 비율이 바뀌어도 가장자리가 안 보인다.</summary>
        [SerializeField] private float _margin = 1.08f;

        private void LateUpdate()
        {
            Fit();
        }

        private void Fit()
        {
            var camera = transform.parent.GetComponent<Camera>();
            var size = GetComponent<SpriteRenderer>().sprite.bounds.size;

            transform.localPosition = new Vector3(0f, 0f, _distance);
            transform.localRotation = Quaternion.identity;

            var height = 2f * _distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * _margin;
            var width = height * camera.aspect;
            transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
        }
    }
}
