using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 미사일 무기 전용 데이터 (Hammer 타입)
    /// - 느낌: 쾅! 하고 넓은 범위를 한 번에 밀어내는 묵직함
    /// - 전술: 대규모 후퇴, 긴급 공간 확보
    /// </summary>
    [CreateAssetMenu(fileName = "PolarMissileWeaponData", menuName = "EarthDefense/Polar/Weapon Data/Missile", order = 12)]
    public class PolarMissileWeaponData : PolarWeaponData
    {
        [Header("Missile Specific")]
        [Tooltip("발사 속도 (발/초)")]
        [SerializeField, Min(0.1f)] private float fireRate = 0.5f;

        [Tooltip("미사일 속도")]
        [SerializeField, Min(1f)] private float missileSpeed = 12f;

        [Tooltip("미사일 수명 (초)")]
        [SerializeField, Min(0.1f)] private float missileLifetime = 5f;

        [Tooltip("폭발 반경 (섹터 개수)")]
        [SerializeField, Range(5, 30)] private int explosionRadius = 10;

        [Tooltip("폭발 시각 효과 프리팹")]
        [SerializeField] private GameObject explosionVFXPrefab;

        [Tooltip("미사일 크기")]
        [SerializeField, Min(0.1f)] private float missileScale = 0.5f;

        [Tooltip("미사일 색상")]
        [SerializeField] private Color missileColor = Color.red;

        public float FireRate => fireRate;
        public float MissileSpeed => missileSpeed;
        public float MissileLifetime => missileLifetime;
        public int ExplosionRadius => explosionRadius;
        public GameObject ExplosionVFXPrefab => explosionVFXPrefab;
        public float MissileScale => missileScale;
        public Color MissileColor => missileColor;
    }
}
