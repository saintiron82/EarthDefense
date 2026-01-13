using UnityEngine;
using Polar.Weapons;

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

        [Header("Projectile Option Profile (optional)")]
        [SerializeField] private PolarProjectileOptionProfile missileOptions;

        public float FireRate => fireRate;
        public float MissileSpeed => missileOptions != null ? missileOptions.Speed : missileSpeed;
        public float MissileLifetime => missileOptions != null ? missileOptions.Lifetime : missileLifetime;
        
        public int CoreRadius => coreRadius;
        public int EffectiveRadius => effectiveRadius;
        public int MaxRadius => maxRadius;
        public float CoreMultiplier => coreMultiplier;
        public float EffectiveMinMultiplier => effectiveMinMultiplier;
        public float MaxMinMultiplier => maxMinMultiplier;
        public ExplosionFalloffType FalloffType => falloffType;
        
        public GameObject ExplosionVFXPrefab => explosionVFXPrefab;
        public float MissileScale => missileOptions != null ? missileOptions.Scale : missileScale;
        public Color MissileColor => missileOptions != null ? missileOptions.Color : missileColor;
        public PolarProjectileOptionProfile MissileOptions => missileOptions;

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
                missileColor = new[] { this.missileColor.r, this.missileColor.g, this.missileColor.b, this.missileColor.a },
                missileOptionProfileId = this.missileOptions != null ? this.missileOptions.Id : null
            };

            return JsonUtility.ToJson(data, prettyPrint);
        }

        public override void FromJson(string json)
        {
            var data = JsonUtility.FromJson<MissileWeaponDataJson>(json);

            // Base - baseData가 있으면 중첩 구조, 없으면 평면 구조
            if (!string.IsNullOrEmpty(data.baseData))
            {
                base.FromJson(data.baseData);
            }
            else
            {
                base.FromJson(json);
            }

            // Missile Specific (값이 0보다 크면 적용)
            if (data.fireRate > 0) this.fireRate = data.fireRate;
            if (data.missileSpeed > 0) this.missileSpeed = data.missileSpeed;
            if (data.missileLifetime > 0) this.missileLifetime = data.missileLifetime;
            if (data.coreRadius > 0) this.coreRadius = data.coreRadius;
            if (data.effectiveRadius > 0) this.effectiveRadius = data.effectiveRadius;
            if (data.maxRadius > 0) this.maxRadius = data.maxRadius;
            if (data.coreMultiplier > 0) this.coreMultiplier = data.coreMultiplier;
            if (data.effectiveMinMultiplier > 0) this.effectiveMinMultiplier = data.effectiveMinMultiplier;
            if (data.maxMinMultiplier > 0) this.maxMinMultiplier = data.maxMinMultiplier;
            if (!string.IsNullOrEmpty(data.falloffType))
            {
                this.falloffType = System.Enum.TryParse<ExplosionFalloffType>(data.falloffType, out var parsed) ? parsed : ExplosionFalloffType.Smooth;
            }
            if (data.missileScale > 0) this.missileScale = data.missileScale;
            if (data.missileColor != null && data.missileColor.Length == 4)
            {
                this.missileColor = new Color(data.missileColor[0], data.missileColor[1], data.missileColor[2], data.missileColor[3]);
            }

#if UNITY_EDITOR
            // missileOptionProfileId가 있으면 프로필 참조 연결 시도
            if (!string.IsNullOrEmpty(data.missileOptionProfileId))
            {
                var profile = FindProjectileOptionProfileById(data.missileOptionProfileId);
                if (profile != null)
                {
                    this.missileOptions = profile;
                }
                else
                {
                    Debug.LogWarning($"[PolarMissileWeaponData] Missile option profile not found: {data.missileOptionProfileId}");
                }
            }
#endif
        }

#if UNITY_EDITOR
        private PolarProjectileOptionProfile FindProjectileOptionProfileById(string profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return null;

            var guids = UnityEditor.AssetDatabase.FindAssets("t:PolarProjectileOptionProfile");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<PolarProjectileOptionProfile>(path);
                if (profile != null && profile.Id == profileId)
                {
                    return profile;
                }
            }
            return null;
        }
#endif

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
            public string missileOptionProfileId;
        }
    }
}
