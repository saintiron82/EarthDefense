using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 레이저 무기 전용 데이터 (Drill 타입)
    /// - 느낌: 좁은 곳을 꾸준히 지지는 용접기
    /// - 전술: 약점 집중 타격, 장시간 유지
    /// </summary>
    [CreateAssetMenu(fileName = "PolarLaserWeaponData", menuName = "EarthDefense/Polar/Weapon Data/Laser", order = 10)]
    public class PolarLaserWeaponData : PolarWeaponData
    {
        [Header("Laser Specific")]
        [Tooltip("빔 확장 속도")]
        [SerializeField, Min(1f)] private float extendSpeed = 50f;

        [Tooltip("빔 수축 속도")]
        [SerializeField, Min(1f)] private float retractSpeed = 70f;

        [Tooltip("빔 최대 길이")]
        [SerializeField, Min(1f)] private float maxLength = 50f;

        [Tooltip("빔 두께")]
        [SerializeField, Min(0.01f)] private float beamWidth = 0.1f;

        [Tooltip("빔 색상")]
        [SerializeField] private Color beamColor = Color.cyan;

        [Tooltip("지속 시간 (0 = 무한)")]
        [SerializeField, Min(0f)] private float duration = 2f;

        public float ExtendSpeed => extendSpeed;
        public float RetractSpeed => retractSpeed;
        public float MaxLength => maxLength;
        public float BeamWidth => beamWidth;
        public Color BeamColor => beamColor;
        public float Duration => duration;
    }
}
