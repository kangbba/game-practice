using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 카메라가 따라가는 대상을 어디서 어떤 각도로 보나. 대상에서 떨어진 거리, 내려다보는 각(x 회전), 시야각(투시)이다.
    /// 보는 방식은 여기 적힌 것뿐이고, 상황에 맞춰 GameCamera.SetView 로 갈아 끼운다.
    /// </summary>
    public readonly struct CameraView
    {
        /// <summary>평소 전투. 높이 떠서 비스듬히 내려다본다.</summary>
        public static readonly CameraView Battle = new CameraView(new Vector3(0f, 24.43f, -9.8f), 65.638f, 60f);

        /// <summary>궁극기. 평소보다 낮고 가까이서, 덜 내려다본다.</summary>
        public static readonly CameraView Ultimate = new CameraView(new Vector3(0.2f, 12.38f, -16.34f), 34.899f, 34.8f);

        public Vector3 Offset { get; }
        public float Pitch { get; }

        /// <summary>세로 시야각(도). 작을수록 망원처럼 좁고 납작하게 보인다.</summary>
        public float FieldOfView { get; }

        public Quaternion Rotation => Quaternion.Euler(Pitch, 0f, 0f);

        private CameraView(Vector3 offset, float pitch, float fieldOfView)
        {
            Offset = offset;
            Pitch = pitch;
            FieldOfView = fieldOfView;
        }
    }
}
