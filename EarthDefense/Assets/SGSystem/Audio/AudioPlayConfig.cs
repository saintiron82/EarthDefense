using UnityEngine;

namespace SG.Audio
{
    /// <summary>
    /// 오디오 재생 설정
    /// </summary>
    public class AudioPlayConfig
    {
        /// <summary>볼륨 (0~1, 채널 볼륨에 곱해짐)</summary>
        public float Volume = 1f;

        /// <summary>피치 (0.5~3, 기본 1)</summary>
        public float Pitch = 1f;

        /// <summary>루프 재생 여부</summary>
        public bool Loop = false;

        /// <summary>페이드 인 시간(초, 0이면 즉시)</summary>
        public float FadeInDuration = 0f;

        /// <summary>지연 시간(초, 0이면 즉시)</summary>
        public float Delay = 0f;

        /// <summary>3D 사운드 여부</summary>
        public bool Is3D = false;

        /// <summary>3D 사운드 위치 (Is3D=true일 때만 사용)</summary>
        public Vector3 Position = Vector3.zero;

        /// <summary>3D 사운드 최소 거리</summary>
        public float MinDistance = 1f;

        /// <summary>3D 사운드 최대 거리</summary>
        public float MaxDistance = 500f;

        /// <summary>공간 블렌드 (0=2D, 1=3D)</summary>
        public float SpatialBlend = 0f;

        /// <summary>우선순위 (0=최고, 256=최저)</summary>
        public int Priority = 128;

        /// <summary>기본 설정 (2D, 원샷)</summary>
        public static AudioPlayConfig Default => new AudioPlayConfig();

        /// <summary>루프 설정 (2D, 루프)</summary>
        public static AudioPlayConfig Loop2D => new AudioPlayConfig { Loop = true };

        /// <summary>3D 원샷 설정</summary>
        public static AudioPlayConfig OneShot3D(Vector3 position)
        {
            return new AudioPlayConfig
            {
                Is3D = true,
                Position = position,
                SpatialBlend = 1f,
                MinDistance = 1f,
                MaxDistance = 50f
            };
        }

        /// <summary>3D 루프 설정 (환경음)</summary>
        public static AudioPlayConfig AmbientLoop3D(Vector3 position)
        {
            return new AudioPlayConfig
            {
                Loop = true,
                Is3D = true,
                Position = position,
                SpatialBlend = 1f,
                MinDistance = 10f,
                MaxDistance = 100f,
                Priority = 200
            };
        }
    }
}
