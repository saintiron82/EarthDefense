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
            
            // 발사 각도: 오브젝트 중심 → 머즐 방향 (실시간 추적)
            _nextFireTime = Time.time + (1f / FireRate);
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

            beam.transform.SetParent(Muzzle, worldPositionStays: true);
            beam.transform.position = Muzzle.position;
            beam.transform.rotation = Muzzle.rotation;

            // Beam 자체가 방향/틱을 관리하므로 speed=0, lifetime은 넉넉히(리트랙트 호출로 종료)
            beam.Fire(Vector2.zero, ProjectileDamage, 0f, _laserWeaponData.LaserDuration * 2f, int.MaxValue, _source, _sourceTeamKey);
            beam.Configure(
                _laserWeaponData.LaserWidth,
                _laserWeaponData.HitRadius,
                _laserWeaponData.SweepSteps,
                _laserWeaponData.SweepEpsilon,
                _laserWeaponData.RehitCooldown,
                _laserWeaponData.DamageTickRate,
                _laserWeaponData.LaserExtendSpeed,
                _laserWeaponData.LaserRetractSpeed,
                _laserWeaponData.BeamMaxLength,
                _laserWeaponData.LaserColor,
                hitEffect);

            // 빔이 동일 대상 여러 번 때릴 수 있는 총량을 데이터에서 설정
            beam.OverrideMaxHits(_laserWeaponData.BeamMaxHits);

            _activeBeam = beam;

            // 지속시간 후 리트랙트
            Invoke(nameof(StopBeam), _laserWeaponData.LaserDuration);
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
            CancelInvoke(nameof(StopBeam));
            StopBeam();
        }
    }
}
