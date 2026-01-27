using UnityEngine;
using Script.SystemCore.Pool;

namespace Polar.Weapons
{
    /// <summary>
    /// 일반 투사체 무기 (미사일, 머신건, 중력 등)
    /// - WeaponData.ProjectileBundleId로 투사체 타입 결정
    /// - 각도/반경 기반 발사
    /// </summary>
    public class PolarProjectileWeapon : PolarWeaponBase
    {
        public override void Fire(float angleDeg, float startRadius)
        {
            if (_field == null || weaponData == null) return;
            
            SpawnProjectile(angleDeg, startRadius);
        }

        private void SpawnProjectile(float angleDeg, float startRadius)
        {
            if (PoolService.Instance == null || string.IsNullOrEmpty(weaponData.ProjectileBundleId))
            {
                Debug.LogError($"[PolarProjectileWeapon] PoolService or ProjectileBundleId not available!");
                return;
            }

            Vector3 origin = Muzzle.position;
            
            var projectile = PoolService.Instance.Get<PolarProjectileBase>(
                weaponData.ProjectileBundleId,
                origin,
                Quaternion.identity
            );

            if (projectile != null)
            {
                // Launch(field, data, angle, radius) 오버로드 호출
                var launchMethod = projectile.GetType().GetMethod("Launch",
                    new[] { typeof(IPolarField), typeof(PolarWeaponData), typeof(float), typeof(float) });

                if (launchMethod != null)
                {
                    launchMethod.Invoke(projectile, new object[] { _field, weaponData, angleDeg, startRadius });
                }
                else
                {
                    // 기본 Launch만 있는 경우
                    projectile.Launch(_field, weaponData);
                }
            }
        }
    }
}

