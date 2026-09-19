using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 머리 위 UI 가 붙는 자리. 머리 위 HP 바와 말풍선이 같은 자리를 쓴다 — 말풍선이 뜨는 동안은 HP 바가 비켜 준다.
    /// 키에 카메라 위쪽 방향을 곱한 월드 지점을 화면으로 옮기고, 거기서 픽셀만큼 더 띄운다. 키는 캐릭터마다 다르다.
    /// </summary>
    public static class HeadAnchor
    {
        /// <summary>머리 꼭대기에서 붙는 지점까지의 월드 간격.</summary>
        public const float WorldGap = 0.25f;

        /// <summary>그 지점에서 화면상으로 더 띄우는 값. 기준 해상도 단위라 거리와 상관없이 간격이 같다.</summary>
        public static readonly Vector2 ScreenOffset = new Vector2(0f, 22f);

        /// <summary>몸의 발에서 붙는 지점까지의 월드 오프셋.</summary>
        public static Vector3 WorldOffset(Camera camera, float height)
        {
            return camera.transform.up * (height + WorldGap);
        }
    }
}
