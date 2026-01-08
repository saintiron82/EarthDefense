﻿using UnityEngine;
using Script.SystemCore.Pool;
using Script.SystemCore.Resource;

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

        protected override void OnInitialized()
        {
            LoadBeamPrefab();
        }

        private void LoadBeamPrefab()
        {
            // ✅ ProjectileBundleId 사용 (beamBundleId 제거)
            if (LaserData == null || string.IsNullOrEmpty(LaserData.ProjectileBundleId)) return;
        }

        public override void Fire()
        {
            if (_field == null || LaserData == null) return;

            Vector2 origin = Muzzle.position;
            Vector2 direction = Muzzle.right;

            // 이미 활성 빔이 있으면 위치/방향만 갱신
            if (_activeBeam != null && _activeBeam.IsActive)
            {
                _activeBeam.UpdateOriginDirection(origin, direction);
                return;
            }

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
        /// 발사 중지 (빔 리트랙트)
        /// </summary>
        public void StopFire()
        {
            if (_activeBeam != null)
            {
                _activeBeam.BeginRetract();
                _activeBeam = null;
            }
        }

        private void OnDisable()
        {
            StopFire();
        }
    }
}
