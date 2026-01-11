using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// Polar 전용 타격 범위 타입
    /// </summary>
    public enum PolarAreaType 
    { 
        Fixed,      // 고정 섹터 (1칸, 레이저 - 균일 데미지)
        Linear,     // 선형 감쇠 (불릿 - 거리 비례 감소)
        Gaussian,   // 가우시안 분포 (머신건 - 부드러운 곡선)
        Explosion   // 물리적 반경 (미사일 - 폭발 감쇠)
    }

    /// <summary>
    /// 폭발 감쇠 곡선 타입
    /// </summary>
    public enum ExplosionFalloffType
    {
        Linear,      // 선형 감쇠 (균일한 감소)
        Smooth,      // 부드러운 곡선 (SmoothStep)
        Exponential  // 지수 감쇠 (급격한 감소)
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
        [Tooltip("초당 총 데미지 (DPS). TickRate로 나눠서 틱당 데미지 계산")]
        [SerializeField] private float damage = 5f;
        [SerializeField] private float knockbackPower = 0.2f;
        [SerializeField] private PolarAreaType areaType = PolarAreaType.Fixed;
        [SerializeField, Min(0)] private int damageRadius = 0;
        [SerializeField] private bool useGaussianFalloff = true;
        [SerializeField, Range(0f, 1f)] private float woundIntensity = 0.2f;

        [Header("Beam (optional)")]
        [Tooltip("초당 타격 횟수 (예: 10 = 0.1초마다 1회)")]
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

        /// <summary>
        /// JSON으로 내보내기 (직렬화)
        /// </summary>
        public virtual string ToJson(bool prettyPrint = true)
        {
            var data = new WeaponDataJson
            {
                id = this.id,
                weaponName = this.weaponName,
                weaponBundleId = this.weaponBundleId,
                projectileBundleId = this.projectileBundleId,
                damage = this.damage,
                knockbackPower = this.knockbackPower,
                areaType = this.areaType.ToString(),
                damageRadius = this.damageRadius,
                useGaussianFalloff = this.useGaussianFalloff,
                woundIntensity = this.woundIntensity,
                tickRate = this.tickRate
            };
            
            return JsonUtility.ToJson(data, prettyPrint);
        }

        /// <summary>
        /// JSON에서 데이터 가져오기 (역직렬화)
        /// </summary>
        public virtual void FromJson(string json)
        {
            var data = JsonUtility.FromJson<WeaponDataJson>(json);
            
            this.id = data.id;
            this.weaponName = data.weaponName;
            this.weaponBundleId = data.weaponBundleId;
            this.projectileBundleId = data.projectileBundleId;
            this.damage = data.damage;
            this.knockbackPower = data.knockbackPower;
            this.areaType = System.Enum.TryParse<PolarAreaType>(data.areaType, out var parsed) ? parsed : PolarAreaType.Fixed;
            this.damageRadius = data.damageRadius;
            this.useGaussianFalloff = data.useGaussianFalloff;
            this.woundIntensity = data.woundIntensity;
            this.tickRate = data.tickRate;
        }

        /// <summary>
        /// JSON 직렬화용 데이터 클래스
        /// </summary>
        [System.Serializable]
        protected class WeaponDataJson
        {
            public string id;
            public string weaponName;
            public string weaponBundleId;
            public string projectileBundleId;
            public float damage;
            public float knockbackPower;
            public string areaType;
            public int damageRadius;
            public bool useGaussianFalloff;
            public float woundIntensity;
            public float tickRate;
        }
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
