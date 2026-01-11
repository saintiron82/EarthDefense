using UnityEngine;
using Script.SystemCore.Pool;
using Script.SystemCore.Resource;
using Polar.Weapons.Data;
using Polar.Weapons.Projectiles;

namespace Polar.Weapons
{
    /// <summary>
    /// 미사일 무기 (PolarMissileWeaponData + PolarMissileProjectile 매칭)
    /// - 독립 로직: 느린 발사, 폭발 효과
    /// - SGSystem: PoolService 전용
    /// - Arm: Muzzle.position 사용
    /// </summary>
    public class PolarMissileWeapon : PolarWeaponBase
    {
        private PolarMissileWeaponData MissileData => weaponData as PolarMissileWeaponData;

        protected override void OnInitialized()
        {
            // 초기화 로직 (필요시 추가)
        }

        public override void Fire()
        {
            if (!CanFire || _field == null || MissileData == null) return;

            SpawnMissile();
            SetCooldown(1f / MissileData.FireRate);
        }

        /// <summary>
        /// 특정 각도로 발사
        /// </summary>
        public void Fire(float angleDeg)
        {
            if (!CanFire || _field == null || MissileData == null) return;

            SpawnMissile(angleDeg);
            SetCooldown(1f / MissileData.FireRate);
        }

        private void SpawnMissile(float? targetAngle = null)
        {
            // ✅ Muzzle.position 사용 (Arm 통합)
            Vector3 origin = Muzzle.position;

            // 각도 계산
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

            // PoolService 전용
            if (PoolService.Instance == null || string.IsNullOrEmpty(MissileData.ProjectileBundleId))
            {
                Debug.LogError("[PolarMissileWeapon] PoolService not available!");
                return;
            }

            var missile = PoolService.Instance.Get<PolarMissileProjectile>(
                MissileData.ProjectileBundleId,
                origin,
                Quaternion.identity
            );

            if (missile != null)
            {
                float startRadius = Vector2.Distance(origin, GetFieldCenter());
                missile.Launch(_field, MissileData, angle, startRadius);
            }
        }
    }
}
