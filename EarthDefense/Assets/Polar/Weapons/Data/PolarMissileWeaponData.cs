using UnityEngine;

namespace Polar.Weapons.Data
{
    /// <summary>
    /// 미사일 무기 전용 데이터 (Hammer 타입)
    /// - 느낌: 쾅! 하고 넓은 범위를 한 번에 밀어내는 묵직함
    /// - 전술: 대규모 후퇴, 긴급 공간 확보
    /// - 3단계 폭발: 폭심 (Core) → 유효 범위 (Effective) → 외곽 (Outer)
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

        [Header("Explosion Mechanics")]
        [Tooltip("폭심 반경 (섹터 수) - 이 범위는 풀 데미지")]
        [SerializeField, Range(0, 5)] private int coreRadius = 1;
        
        [Tooltip("유효 피해 반경 (섹터 수) - 이 범위까지는 의미있는 데미지")]
        [SerializeField, Range(3, 10)] private int effectiveRadius = 5;
        
        [Tooltip("최대 영향 반경 (섹터 수) - 이 범위까지 감쇠 적용")]
        [SerializeField, Range(5, 15)] private int maxRadius = 8;
        
        [Tooltip("폭심 데미지 배율 (1.0 = 기본 데미지)")]
        [SerializeField, Range(0.8f, 1.5f)] private float coreMultiplier = 1.0f;
        
        [Tooltip("유효 범위 최소 데미지 배율")]
        [SerializeField, Range(0.5f, 1.0f)] private float effectiveMinMultiplier = 0.8f;
        
        [Tooltip("최대 범위 최소 데미지 배율")]
        [SerializeField, Range(0.0f, 0.5f)] private float maxMinMultiplier = 0.1f;
        
        [Tooltip("감쇠 곡선 타입")]
        [SerializeField] private ExplosionFalloffType falloffType = ExplosionFalloffType.Smooth;

        [Header("Visual")]
        [Tooltip("폭발 시각 효과 프리팹")]
        [SerializeField] private GameObject explosionVFXPrefab;

        [Tooltip("미사일 크기")]
        [SerializeField, Min(0.1f)] private float missileScale = 0.5f;

        [Tooltip("미사일 색상")]
        [SerializeField] private Color missileColor = Color.red;

        public float FireRate => fireRate;
        public float MissileSpeed => missileSpeed;
        public float MissileLifetime => missileLifetime;
        
        public int CoreRadius => coreRadius;
        public int EffectiveRadius => effectiveRadius;
        public int MaxRadius => maxRadius;
        public float CoreMultiplier => coreMultiplier;
        public float EffectiveMinMultiplier => effectiveMinMultiplier;
        public float MaxMinMultiplier => maxMinMultiplier;
        public ExplosionFalloffType FalloffType => falloffType;
        
        public GameObject ExplosionVFXPrefab => explosionVFXPrefab;
        public float MissileScale => missileScale;
        public Color MissileColor => missileColor;

        public override string ToJson(bool prettyPrint = true)
        {
            var data = new MissileWeaponDataJson
            {
                // Base
                baseData = base.ToJson(false),
                // Missile Specific
                fireRate = this.fireRate,
                missileSpeed = this.missileSpeed,
                missileLifetime = this.missileLifetime,
                coreRadius = this.coreRadius,
                effectiveRadius = this.effectiveRadius,
                maxRadius = this.maxRadius,
                coreMultiplier = this.coreMultiplier,
                effectiveMinMultiplier = this.effectiveMinMultiplier,
                maxMinMultiplier = this.maxMinMultiplier,
                falloffType = this.falloffType.ToString(),
                missileScale = this.missileScale,
                missileColor = new[] { missileColor.r, missileColor.g, missileColor.b, missileColor.a }
            };
            
            return JsonUtility.ToJson(data, prettyPrint);
        }

        public override void FromJson(string json)
        {
            var data = JsonUtility.FromJson<MissileWeaponDataJson>(json);
            
            // Base
            base.FromJson(data.baseData);
            
            // Missile Specific
            this.fireRate = data.fireRate;
            this.missileSpeed = data.missileSpeed;
            this.missileLifetime = data.missileLifetime;
            this.coreRadius = data.coreRadius;
            this.effectiveRadius = data.effectiveRadius;
            this.maxRadius = data.maxRadius;
            this.coreMultiplier = data.coreMultiplier;
            this.effectiveMinMultiplier = data.effectiveMinMultiplier;
            this.maxMinMultiplier = data.maxMinMultiplier;
            this.falloffType = System.Enum.TryParse<ExplosionFalloffType>(data.falloffType, out var parsed) ? parsed : ExplosionFalloffType.Smooth;
            this.missileScale = data.missileScale;
            if (data.missileColor != null && data.missileColor.Length == 4)
            {
                this.missileColor = new Color(data.missileColor[0], data.missileColor[1], data.missileColor[2], data.missileColor[3]);
            }
        }

        [System.Serializable]
        private class MissileWeaponDataJson
        {
            public string baseData;
            public float fireRate;
            public float missileSpeed;
            public float missileLifetime;
            public int coreRadius;
            public int effectiveRadius;
            public int maxRadius;
            public float coreMultiplier;
            public float effectiveMinMultiplier;
            public float maxMinMultiplier;
            public string falloffType;
            public float missileScale;
            public float[] missileColor;
        }
    }
}
