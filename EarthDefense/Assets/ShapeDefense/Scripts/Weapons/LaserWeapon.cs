using System;
using Script.SystemCore.Pool;
using ShapeDefense.Scripts.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace ShapeDefense.Scripts.Weapons
{
    /// <summary>
    /// 레이저 무기 - 연속 발사 레이저 무기
    /// </summary>
    public sealed class LaserWeapon : BaseWeapon
    {
 
        [Header("Hit Effects")]
        [SerializeField] private HitEffect hitEffect;

        private LaserWeaponData _laserWeaponData;
        private BeamProjectile _activeBeam;
 
        protected IObjectPool<BeamProjectile> _projectilePool;

        private LaserWeaponData LaserWeaponData => _laserWeaponData ?? throw new InvalidOperationException("LaserWeapon not initialized with LaserWeaponData");

        public override void Initialize(WeaponData weaponData, GameObject source, int sourceTeamKey)
        {
            if (weaponData is not LaserWeaponData laserData)
                throw new ArgumentException("LaserWeapon requires LaserWeaponData", nameof(weaponData));

            _laserWeaponData = laserData;
            base.Initialize(weaponData, source, sourceTeamKey);
        }
        
        
        protected override void LoadProjectilePool()
        {   
            if( !string.IsNullOrWhiteSpace(ProjectileBundleId) && PoolService.Instance != null)
            {
                _projectilePool = PoolService.Instance.GetPool<BeamProjectile>(ProjectileBundleId);
            }
        }

        protected override void FireInternal(Vector2 direction)
        {
            if (!CanFire) return;

            // 기존 빔은 소멸 시퀀스로 전환하고 새 빔 생성
            if (_activeBeam != null)
            {
                StopBeam();
            }

            // 자동 모드: 발사 중에는 쿨다운을 멈춰서 빔 유지, 중지 시점에만 쿨다운 적용
            if (CurrentFireMode == FireMode.Automatic)
            {
                _nextFireTime = float.MaxValue;
            }
            else
            {
                _nextFireTime = Time.time + (1f / FireRate);
            }

            SpawnBeam();
        }
 
        private void SpawnBeam()
        {
            var bundleId = _laserWeaponData.ProjectileBundleId;
            var beam = PoolService.Instance?.Get<BeamProjectile>(bundleId);
            if (beam == null)
            {
                Debug.LogWarning($"[LaserWeapon] BeamProjectile not found in pool: {bundleId}");
                return;
            }

            _activeBeam = beam;

            UpdateBeamTransform();

            // Beam 자체가 방향/틱을 관리하므로 speed=0, lifetime은 입력 홀드와 동기화(무한 지속)
            const float infiniteLifetime = float.MaxValue;
            beam.Fire(Vector2.zero, ProjectileDamage, 0f, infiniteLifetime, int.MaxValue, _source, _sourceTeamKey);
            beam.Configure(
                LaserWeaponData.LaserWidth,
                LaserWeaponData.HitRadius,
                LaserWeaponData.SweepSteps,
                LaserWeaponData.SweepEpsilon,
                LaserWeaponData.RehitCooldown,
                LaserWeaponData.DamageTickRate,
                LaserWeaponData.LaserExtendSpeed,
                LaserWeaponData.LaserRetractSpeed,
                LaserWeaponData.BeamMaxLength,
                LaserWeaponData.LaserColor,
                hitEffect);

            var maxHits = LaserWeaponData.BeamMaxHits <= 0 ? int.MaxValue : LaserWeaponData.BeamMaxHits;
            beam.OverrideMaxHits(maxHits);
        }

        private void UpdateBeamTransform()
        {
            if (_activeBeam == null) return;
            _activeBeam.transform.SetParent(Muzzle, worldPositionStays: true);
            _activeBeam.transform.position = Muzzle.position;
            _activeBeam.transform.rotation = Muzzle.rotation;
        }

        private void StopBeam()
        {
            if (_activeBeam != null)
            {
                _activeBeam.BeginRetract();
                _activeBeam = null;
            }
        }

        private void OnDisable()
        {
            StopBeam();
        }

        public override void StopFire()
        {
            base.StopFire();

            if (_activeBeam != null && _activeBeam.IsActive)
            {
                // 현재 방향으로 분리 후 리트랙트 이동
                var dir = (Vector2)_activeBeam.transform.right;
                var travelSpeed = LaserWeaponData.LaserExtendSpeed;
                var remainLifetime = LaserWeaponData.LaserDuration > 0f ? LaserWeaponData.LaserDuration : 1f;
                _activeBeam.DetachAndExpire(dir, travelSpeed, remainLifetime);
                _activeBeam = null;
            }

            // 자동 모드에서 끊을 때만 쿨다운 재적용
            if (CurrentFireMode == FireMode.Automatic)
            {
                _nextFireTime = Time.time + (1f / FireRate);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (_activeBeam != null)
            {
                if (!_activeBeam.IsActive)
                {
                    _activeBeam = null;
                }

                if (_isFiring)
                {
                    UpdateBeamTransform();
                }
            }
        }
    }
}
