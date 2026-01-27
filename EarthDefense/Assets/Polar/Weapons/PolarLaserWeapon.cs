using UnityEngine;
using Script.SystemCore.Pool;
using Polar.Weapons.Projectiles;
using System.Collections.Generic;

namespace Polar.Weapons
{
    /// <summary>
    /// 레이저 전용 무기 (연속 빔, 반사 등 특수 기능)
    /// </summary>
    public class PolarLaserWeapon : PolarWeaponBase
    {
        [Header("Debug")]
        [SerializeField] private bool logReflect;

        private PolarLaserWeaponData LaserData => weaponData as PolarLaserWeaponData;
        private PolarLaserProjectile _activeBeam;
        private readonly List<PolarLaserProjectile> _activeChildBeams = new List<PolarLaserProjectile>();

        public override void Fire(float angleDeg, float startRadius)
        {
            // 레이저는 각도/반경 사용 안함 - Muzzle 방향 사용
            Fire();
        }

        public void Fire()
        {
            if (_field == null || LaserData == null) return;

            Vector2 origin = Muzzle.position;
            Vector2 direction = Muzzle.right;

            if (_activeBeam != null && _activeBeam.IsActive)
            {
                _activeBeam.UpdateOriginDirection(origin, direction);
            }
        }

        /// <summary>
        /// 발사 시작 (기존 빔이 있으면 FlyAway 후 새 빔 생성)
        /// </summary>
        public void StartFire()
        {
            if (_field == null || LaserData == null) return;

            Vector2 origin = Muzzle.position;
            Vector2 direction = Muzzle.right;

            StopFireInternal();

            SpawnBeam(origin, direction, LaserData.ReflectCount);
        }

        private void SpawnBeam(Vector2 origin, Vector2 direction, int remainingReflects)
        {
            int currentDepth = LaserData.ReflectCount - remainingReflects;
            
            // 자식 빔은 ChildBeamProjectileBundleId 사용 (없으면 메인 번들 재사용)
            string bundleId = currentDepth > 0 && !string.IsNullOrEmpty(LaserData.ChildBeamProjectileBundleId)
                ? LaserData.ChildBeamProjectileBundleId
                : LaserData.ProjectileBundleId;

            if (PoolService.Instance == null || string.IsNullOrEmpty(bundleId))
            {
                Debug.LogError("[PolarLaserWeapon] PoolService not available!");
                return;
            }

            PolarLaserProjectile beam = PoolService.Instance.Get<PolarLaserProjectile>(
                bundleId,
                origin,
                Quaternion.identity
            );

            if (beam != null)
            {
                // 빔 설정
                if (currentDepth == 0)
                {
                    ConfigureMainBeam(beam);
                }
                else
                {
                    ConfigureChildBeam(beam);
                }
                
                // 반사 콜백 설정 (모든 빔)
                beam.OnDamageTick = (hitPoint, dir) => OnBeamDamageTick(hitPoint, dir, remainingReflects);
                
                // Launch 호출 (InitializeBeam 실행됨)
                beam.Launch(_field, LaserData, origin, direction);
                
                // 빔 추적
                if (currentDepth == 0)
                {
                    _activeBeam = beam;
                }
                else
                {
                    _activeChildBeams.Add(beam);
                }
            }
        }

        private void ConfigureMainBeam(PolarLaserProjectile beam)
        {
            beam.SetDamageMultiplier(1f);
            beam.SetWidthMultiplier(1f);
            beam.SetTickRateMultiplier(1f);
        }

        private void ConfigureChildBeam(PolarLaserProjectile beam)
        {
            float multiplier = LaserData.ChildBeamMultiplier;
            
            // 길이: 배율 적용
            float childMaxLength = LaserData.MaxLength * multiplier;
            
            // 지속 시간: 독립 값 사용 (0이면 메인 빔과 동일)
            float childDuration = LaserData.ChildBeamDuration > 0f 
                ? LaserData.ChildBeamDuration 
                : LaserData.Duration;  // 0일 때 메인 빔과 동일
            
            beam.SetChildBeamMultipliers(
                multiplier,      // damage
                multiplier,      // width
                multiplier,      // tickRate
                childMaxLength,  // maxLength
                childDuration    // duration (독립값)
            );
        }

        /// <summary>
        /// 데미지 틱 발생 시 호출되는 콜백 - 반사 확률 체크 및 자식 빔 생성
        /// </summary>
        private void OnBeamDamageTick(Vector2 hitPoint, Vector2 direction, int remainingReflects)
        {
            // 남은 반사 횟수가 없으면 중단
            if (remainingReflects <= 0)
            {
                return;
            }

            // 반사 확률 체크
            float probability = Mathf.Clamp01(LaserData.ReflectProbability);
            
            if (Random.value > probability)
            {
                if (logReflect)
                {
                    Debug.Log($"[PolarLaserWeapon] Reflect SKIPPED by probability ({probability:F2})");
                }
                return;
            }

            // 반사 방향 계산
            Vector2 center = (_field as Component) != null ? ((Component)_field).transform.position : Vector2.zero;
            Vector2 normal = (hitPoint - center).sqrMagnitude > 0f ? (hitPoint - center).normalized : Vector2.right;
            Vector2 reflectDir = Vector2.Reflect(direction, normal).normalized;
            reflectDir = ApplyReflectAngleNoise(reflectDir);

            // 자식 빔 생성
            SpawnBeam(hitPoint, reflectDir, remainingReflects - 1);

            if (logReflect)
            {
                int currentDepth = LaserData.ReflectCount - remainingReflects;
                int newDepth = currentDepth + 1;
                Debug.Log($"[PolarLaserWeapon] Spawned child beam depth={newDepth} (remaining={remainingReflects - 1}) at {hitPoint} " +
                    $"multiplier={LaserData.ChildBeamMultiplier:F2}");
            }
        }

        private Vector2 ApplyReflectAngleNoise(Vector2 dir)
        {
            float range = LaserData != null ? Mathf.Max(0f, LaserData.ReflectAngleRange) : 0f;
            if (range <= 0.001f)
            {
                return dir;
            }

            // [-range/2, +range/2]
            float delta = Random.Range(-range * 0.5f, range * 0.5f);
            float rad = delta * Mathf.Deg2Rad;

            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(
                dir.x * cos - dir.y * sin,
                dir.x * sin + dir.y * cos
            ).normalized;
        }


        /// <summary>
        /// 발사 중지 (빔이 날아가며 소멸)
        /// </summary>
        public void StopFire()
        {
            StopFireInternal();
        }

        private void StopFireInternal()
        {
            if (_activeBeam != null)
            {
                _activeBeam.BeginFlyAway();
                _activeBeam = null;
            }

            if (_activeChildBeams.Count > 0)
            {
                foreach (var childBeam in _activeChildBeams)
                {
                    childBeam?.BeginFlyAway();
                }
                _activeChildBeams.Clear();
            }
        }

        private void OnDisable()
        {
            StopFire();
        }
    }
}
