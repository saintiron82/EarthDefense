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

        public float FireRate => fireRate;
        public float ProjectileSpeed => projectileSpeed;
        public float SpreadAngle => spreadAngle;
        public float ProjectileLifetime => projectileLifetime;
        public float ProjectileScale => projectileScale;
        public Color ProjectileColor => projectileColor;

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
                projectileColor = new[] { projectileColor.r, projectileColor.g, projectileColor.b, projectileColor.a }
            };
            
            return JsonUtility.ToJson(data, prettyPrint);
        }

        public override void FromJson(string json)
        {
            var data = JsonUtility.FromJson<MachinegunWeaponDataJson>(json);
            
            // Base
            base.FromJson(data.baseData);
            
            // Machinegun Specific
            this.fireRate = data.fireRate;
            this.projectileSpeed = data.projectileSpeed;
            this.spreadAngle = data.spreadAngle;
            this.projectileLifetime = data.projectileLifetime;
            this.projectileScale = data.projectileScale;
            if (data.projectileColor != null && data.projectileColor.Length == 4)
            {
                this.projectileColor = new Color(data.projectileColor[0], data.projectileColor[1], data.projectileColor[2], data.projectileColor[3]);
            }
        }

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
        }
    }
}
