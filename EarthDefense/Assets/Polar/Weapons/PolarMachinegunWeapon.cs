﻿using Polar.Weapons.Projectiles;
 using UnityEngine;
using Script.SystemCore.Pool;

namespace Polar.Weapons
{
    /// <summary>
    /// 머신건 무기 (PolarMachinegunWeaponData + PolarMachinegunProjectile 매칭)
    /// - 독립 로직: 연사 발사, 산포도 적용
    /// - SGSystem: PoolService 전용
    /// - Arm: Muzzle.position 사용
    /// </summary>
    public class PolarMachinegunWeapon : PolarWeaponBase
    {
        private PolarMachinegunWeaponData MachinegunData => weaponData as PolarMachinegunWeaponData;

        protected override void OnInitialized()
        {
            // 초기화 로직 (필요시 추가)
        }

        public override void Fire()
        {
            if (!CanFire || _field == null || MachinegunData == null) return;

            SpawnProjectile();
            SetCooldown(1f / MachinegunData.FireRate);
        }

        /// <summary>
        /// 특정 각도로 발사
        /// </summary>
        public void Fire(float angleDeg)
        {
            if (!CanFire || _field == null || MachinegunData == null) return;

            SpawnProjectile(angleDeg);
            SetCooldown(1f / MachinegunData.FireRate);
        }

        private void SpawnProjectile(float? targetAngle = null)
        {
            // ✅ Muzzle.position 사용 (Arm 통합)
            Vector3 origin = Muzzle.position;

            // 각도 계산 (산포도 적용)
            float angle;
            if (targetAngle.HasValue)
            {
                angle = targetAngle.Value;
            }
            else
            {
                // Muzzle 방향 기준 ✅
                Vector2 muzzleDir = Muzzle.right;
                angle = Mathf.Atan2(muzzleDir.y, muzzleDir.x) * Mathf.Rad2Deg;
            }
            
            float spread = Random.Range(-MachinegunData.SpreadAngle, MachinegunData.SpreadAngle);
            angle += spread;

            // PoolService 전용
            if (PoolService.Instance == null || string.IsNullOrEmpty(MachinegunData.ProjectileBundleId))
            {
                Debug.LogError($"[PolarMachinegunWeapon] -[{MachinegunData.ProjectileBundleId}] PoolService not available!");
                return;
            }

            var projectile = PoolService.Instance.Get<PolarMachinegunProjectile>(
                MachinegunData.ProjectileBundleId,
                origin,
                Quaternion.identity
            );

            if (projectile != null)
            {
                float startRadius = Vector2.Distance(origin, GetFieldCenter());
                projectile.Launch(_field, MachinegunData, angle, startRadius);
            }
        }
    }
}
