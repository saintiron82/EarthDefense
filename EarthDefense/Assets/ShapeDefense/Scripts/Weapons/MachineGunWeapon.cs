using Script.SystemCore.Pool;
using UnityEngine;
using UnityEngine.Pool;

namespace ShapeDefense.Scripts.Weapons
{
    /// <summary>
    /// 머신건 무기 - 빠른 연사 발사체 무기
    /// WeaponData로부터 발사체 스펙을 주입받아 사용
    /// </summary>
    public class MachineGunWeapon : BaseWeapon
    {
        [Header("Projectile Prefab")]
        [Tooltip("사용할 발사체 프리팹 (타입/비주얼/효과만 정의됨)")]
        [SerializeField] private BaseProjectile projectilePrefab;

        protected IObjectPool<Bullet> _projectilePool;
        protected override void LoadProjectilePool()
        {   
            if( !string.IsNullOrWhiteSpace(ProjectileBundleId) && PoolService.Instance != null)
            {
                _projectilePool = PoolService.Instance.GetPool<Bullet>(ProjectileBundleId);
            }
        }

        protected override void FireInternal(Vector2 direction)
        {
            if (!CanFire) return;

            // 발사 쿨다운 설정 (WeaponData의 FireRate 사용)
            _nextFireTime = Time.time + (1f / FireRate);

            // 암의 머즐 위치에서 발사
            Vector2 spawnPos = Muzzle.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            // 발사체 생성
            if( _projectilePool != null )
            {
                var projGO = _projectilePool.Get();
                projGO.transform.position = spawnPos;
                projGO.transform.rotation = rotation;
                
                projGO.Fire(
                    direction,
                    ProjectileDamage,    // WeaponData에서 가져온 데미지
                    ProjectileSpeed,     // WeaponData에서 가져온 속도
                    ProjectileLifetime,  // WeaponData에서 가져온 수명
                    ProjectileMaxHits,   // WeaponData에서 가져온 관통
                    _source,
                    _sourceTeamKey
                );
            }
            else
            {
                return;
            }
            // WeaponData의 스펙을 발사체에 주입 ⭐
        }
    }
}
