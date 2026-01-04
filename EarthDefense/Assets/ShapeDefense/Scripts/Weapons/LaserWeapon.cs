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
        private float _beamEndTime;
        
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
            _nextFireTime = Time.time + (1f / FireRate);

            // 재발사 입력 시 기존 빔 연장만
            if (_activeBeam != null && _activeBeam.IsActive)
            {
                UpdateBeamTransform();
                _beamEndTime = Time.time + LaserWeaponData.LaserDuration;
                return;
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
            _beamEndTime = Time.time + LaserWeaponData.LaserDuration;

            UpdateBeamTransform();

            // Beam 자체가 방향/틱을 관리하므로 speed=0, lifetime은 넉넉히(리트랙트 호출로 종료)
            beam.Fire(Vector2.zero, ProjectileDamage, 0f, LaserWeaponData.LaserDuration * 2f, int.MaxValue, _source, _sourceTeamKey);
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

            beam.OverrideMaxHits(LaserWeaponData.BeamMaxHits);
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
            StopBeam();
        }

        protected override void Update()
        {
            base.Update();

            if (_activeBeam != null)
            {
                if (!_activeBeam.IsActive)
                {
                    _activeBeam = null;
                    return;
                }

                UpdateBeamTransform();

                if (!_isFiring || (LaserWeaponData.LaserDuration > 0f && Time.time >= _beamEndTime))
                {
                    StopBeam();
                }
            }
        }
    }
}
