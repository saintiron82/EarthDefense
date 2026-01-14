using UnityEngine;
using Script.SystemCore.Pool;
using Polar.Weapons.Data;
using Polar.Weapons.Projectiles;

namespace Polar.Weapons
{
    /// <summary>
    /// 레이저 무기 (PolarLaserWeaponData + PolarLaserProjectile 매칭)
    /// - 독립 로직: 빔 발사, 홀드 관리
    /// - SGSystem: PoolService 전용
    /// - Arm: Muzzle.position 사용
    /// </summary>
    public class PolarLaserWeapon : PolarWeaponBase
    {
        private PolarLaserWeaponData LaserData => weaponData as PolarLaserWeaponData;
        private PolarLaserProjectile _activeBeam;

        private bool _isFiring;

        public override void Fire()
        {
            if (_field == null || LaserData == null) return;

            Vector2 origin = Muzzle.position;
            Vector2 direction = Muzzle.right;

            // 활성 빔이 있으면 위치/방향만 갱신
            if (_activeBeam != null && _activeBeam.IsActive)
            {
                _activeBeam.UpdateOriginDirection(origin, direction);
                if (_isFiring)
                {
                    Debug.Log($"[PolarLaserWeapon] Fire (Update) - Origin: {origin}, Direction: {direction}, Frame: {Time.frameCount}");
                }
                return;
            }

            // 입력 핸들러가 hold-to-fire 모드일 경우 매 프레임 Fire()가 호출되므로,
            // 여기서 신규 스폰을 막고 StartFire()에서만 신규 빔을 생성한다.
        }

        /// <summary>
        /// 발사 시작(클릭/눌림 이벤트)
        /// - 이미 빔이 있으면 즉시 새 빔을 만들기 위해 기존 빔은 FlyAway로 전환
        /// </summary>
        public void StartFire()
        {
            if (_field == null || LaserData == null) return;

            _isFiring = true;

            Vector2 origin = Muzzle.position;
            Vector2 direction = Muzzle.right;

            if (_activeBeam != null && _activeBeam.IsActive)
            {
                _activeBeam.BeginFlyAway();
                _activeBeam = null;
            }

            Debug.Log($"[PolarLaserWeapon] StartFire (NEW BEAM) - Origin: {origin}, Direction: {direction}, Frame: {Time.frameCount}, Time: {Time.time:F4}");
            SpawnBeam(origin, direction);
        }

        private void SpawnBeam(Vector2 origin, Vector2 direction)
        {
            // ✅ ProjectileBundleId 사용
            if (PoolService.Instance == null || string.IsNullOrEmpty(LaserData.ProjectileBundleId))
            {
                Debug.LogError("[PolarLaserWeapon] PoolService not available!");
                return;
            }

            PolarLaserProjectile beam = PoolService.Instance.Get<PolarLaserProjectile>(
                LaserData.ProjectileBundleId,
                origin,
                Quaternion.identity
            );

            if (beam != null)
            {
                beam.Launch(_field, LaserData, origin, direction);
                _activeBeam = beam;
            }
        }

        /// <summary>
        /// 발사 중지 (빔이 날아가며 소멸)
        /// </summary>
        public void StopFire()
        {
            _isFiring = false;

            if (_activeBeam != null)
            {
                Debug.Log($"[PolarLaserWeapon] StopFire - Frame: {Time.frameCount}, Time: {Time.time:F4}");
                _activeBeam.BeginFlyAway();
                _activeBeam = null;
            }
        }

        private void OnDisable()
        {
            StopFire();
        }
    }
}
