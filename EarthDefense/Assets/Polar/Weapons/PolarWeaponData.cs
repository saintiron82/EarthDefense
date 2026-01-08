using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// Polar 전용 타격 범위 타입
    /// </summary>
    public enum PolarAreaType 
    { 
        Fixed,      // 고정 섹터 (1칸, 레이저)
        Gaussian,   // 가우시안 분포 (머신건)
        Explosion   // 물리적 반경 (미사일)
    }

    /// <summary>
    /// Polar 전용 무기 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "PolarWeaponData", menuName = "EarthDefense/Polar/WeaponData", order = 1)]
    public class PolarWeaponData : ScriptableObject
    {
        [Header("ID & UI")]
        [SerializeField] private string id;
        [SerializeField] private string weaponName;
        [SerializeField] private Sprite icon;

        [Header("Bundles")]
        [Tooltip("무기 프리팹 (선택 사항)")]
        [SerializeField] private string weaponBundleId;
        
        [Tooltip("투사체 프리팹 (레이저 빔, 머신건 탄환, 미사일 등)")]
        [SerializeField] private string projectileBundleId;

        [Header("Combat")]
        [SerializeField] private float damage = 5f;
        [SerializeField] private float knockbackPower = 0.2f;
        [SerializeField] private PolarAreaType areaType = PolarAreaType.Fixed;
        [SerializeField, Min(0)] private int damageRadius = 0;
        [SerializeField] private bool useGaussianFalloff = true;
        [SerializeField, Range(0f, 1f)] private float woundIntensity = 0.2f;

        [Header("Beam (optional)")]
        [SerializeField, Min(0.1f)] private float tickRate = 10f;

        public string Id => id;
        public string WeaponName => weaponName;
        public Sprite Icon => icon;
        public string WeaponBundleId => weaponBundleId;
        public string ProjectileBundleId => projectileBundleId;
        public float Damage => damage;
        public float KnockbackPower => knockbackPower;
        public PolarAreaType AreaType => areaType;
        public int DamageRadius => Mathf.Max(0, damageRadius);
        public bool UseGaussianFalloff => useGaussianFalloff;
        public float WoundIntensity => woundIntensity;
        public float TickRate => tickRate;
    }

    /// <summary>
    /// Polar 전용 전투 속성 구조체 (투사체 전달용)
    /// </summary>
    public struct PolarCombatProperties
    {
        public float Damage;
        public float KnockbackPower;
        public PolarAreaType AreaType;
        public bool UseGaussianFalloff;
        public int DamageRadius;
        public float WoundIntensity;

        public PolarCombatProperties(float damage, float knockbackPower, PolarAreaType areaType,
                                      bool useGaussianFalloff, int damageRadius, float woundIntensity)
        {
            Damage = damage;
            KnockbackPower = knockbackPower;
            AreaType = areaType;
            UseGaussianFalloff = useGaussianFalloff;
            DamageRadius = damageRadius;
            WoundIntensity = woundIntensity;
        }

        /// <summary>
        /// PolarWeaponData로부터 생성
        /// </summary>
        public static PolarCombatProperties FromWeaponData(PolarWeaponData weaponData)
        {
            if (weaponData == null)
            {
                return CreateDefault();
            }

            return new PolarCombatProperties(
                weaponData.Damage,
                weaponData.KnockbackPower,
                weaponData.AreaType,
                weaponData.UseGaussianFalloff,
                weaponData.DamageRadius,
                weaponData.WoundIntensity
            );
        }

        /// <summary>
        /// 기본값 생성
        /// </summary>
        public static PolarCombatProperties CreateDefault()
        {
            return new PolarCombatProperties(
                damage: 10f,
                knockbackPower: 1f,
                areaType: PolarAreaType.Fixed,
                useGaussianFalloff: false,
                damageRadius: 1,
                woundIntensity: 0.5f
            );
        }
    }
}
