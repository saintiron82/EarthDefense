﻿using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 머신건 무기 전용 데이터 (Ripper 타입)
    /// - 느낌: 표면을 긁어내고 상처를 벌려놓는 느낌
    /// - 전술: 약점 확장, 영역 무력화
    /// </summary>
    [CreateAssetMenu(fileName = "PolarMachinegunWeaponData", menuName = "EarthDefense/Polar/Weapon Data/Machinegun", order = 11)]
    public class PolarMachinegunWeaponData : PolarWeaponData
    {
        [Header("Machinegun Specific")]
        [Tooltip("연사 속도 (발/초)")]
        [SerializeField, Min(0.1f)] private float fireRate = 10f;

        [Tooltip("투사체 속도")]
        [SerializeField, Min(1f)] private float projectileSpeed = 15f;

        [Tooltip("산포도 (각도)")]
        [SerializeField, Range(0f, 10f)] private float spreadAngle = 2f;

        [Tooltip("투사체 수명 (초)")]
        [SerializeField, Min(0.1f)] private float projectileLifetime = 3f;

        [Tooltip("투사체 크기")]
        [SerializeField, Min(0.1f)] private float projectileScale = 0.3f;

        [Tooltip("투사체 색상")]
        [SerializeField] private Color projectileColor = Color.yellow;

        [Header("Projectile Option Profile (optional)")]
        [SerializeField] private PolarProjectileOptionProfile projectileOptions;

        public float FireRate => fireRate;
        public float ProjectileSpeed => projectileOptions != null ? projectileOptions.Speed : projectileSpeed;
        public float SpreadAngle => spreadAngle;
        public float ProjectileLifetime => projectileOptions != null ? projectileOptions.Lifetime : projectileLifetime;
        public float ProjectileScale => projectileOptions != null ? projectileOptions.Scale : projectileScale;
        public Color ProjectileColor => projectileOptions != null ? projectileOptions.Color : projectileColor;
        public PolarProjectileOptionProfile ProjectileOptions => projectileOptions;

        public override string ToJson(bool prettyPrint = true)
        {
            var data = new MachinegunWeaponDataJson
            {
                // Base
                baseData = base.ToJson(false),
                // Machinegun Specific
                fireRate = this.fireRate,
                projectileSpeed = this.projectileSpeed,
                spreadAngle = this.spreadAngle,
                projectileLifetime = this.projectileLifetime,
                projectileScale = this.projectileScale,
                projectileColor = new[] { this.projectileColor.r, this.projectileColor.g, this.projectileColor.b, this.projectileColor.a },
                projectileOptionProfileId = this.projectileOptions != null ? this.projectileOptions.Id : null
            };

            return JsonUtility.ToJson(data, prettyPrint);
        }

        public override void FromJson(string json)
        {
            var data = JsonUtility.FromJson<MachinegunWeaponDataJson>(json);

            // Base - baseData가 있으면 중첩 구조, 없으면 평면 구조
            if (!string.IsNullOrEmpty(data.baseData))
            {
                base.FromJson(data.baseData);
            }
            else
            {
                base.FromJson(json);
            }

            // Machinegun Specific (값이 0보다 크면 적용)
            if (data.fireRate > 0) this.fireRate = data.fireRate;
            if (data.projectileSpeed > 0) this.projectileSpeed = data.projectileSpeed;
            if (data.spreadAngle > 0) this.spreadAngle = data.spreadAngle;
            if (data.projectileLifetime > 0) this.projectileLifetime = data.projectileLifetime;
            if (data.projectileScale > 0) this.projectileScale = data.projectileScale;
            if (data.projectileColor != null && data.projectileColor.Length == 4)
            {
                this.projectileColor = new Color(data.projectileColor[0], data.projectileColor[1], data.projectileColor[2], data.projectileColor[3]);
            }

#if UNITY_EDITOR
            // projectileOptionProfileId가 있으면 프로필 참조 연결 시도
            if (!string.IsNullOrEmpty(data.projectileOptionProfileId))
            {
                var profile = FindProjectileOptionProfileById(data.projectileOptionProfileId);
                if (profile != null)
                {
                    this.projectileOptions = profile;
                }
                else
                {
                    Debug.LogWarning($"[PolarMachinegunWeaponData] Projectile option profile not found: {data.projectileOptionProfileId}");
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
        private class MachinegunWeaponDataJson
        {
            public string baseData;
            public float fireRate;
            public float projectileSpeed;
            public float spreadAngle;
            public float projectileLifetime;
            public float projectileScale;
            public float[] projectileColor;
            public string projectileOptionProfileId;
        }
    }
}
