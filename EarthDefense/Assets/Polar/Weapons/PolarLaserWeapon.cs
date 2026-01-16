using UnityEngine;
using Script.SystemCore.Pool;
using Polar.Weapons.Projectiles;
using System.Collections.Generic;

namespace Polar.Weapons
{
    public class PolarLaserWeapon : PolarWeaponBase
    {
        private PolarLaserWeaponData LaserData => weaponData as PolarLaserWeaponData;

        private PolarLaserProjectile _activeBeam;
        private readonly List<PolarLaserProjectile> _activeChildBeams = new List<PolarLaserProjectile>();

        private bool _isFiring;

        [Header("Debug")]
        [SerializeField] private bool logReflect;

        public override void Fire()
        {
            if (_field == null || LaserData == null) return;

            Vector2 origin = Muzzle.position;
            Vector2 direction = Muzzle.right;

            if (LaserData.ReflectMode == LaserReflectMode.ChildBeam)
            {
                if (_activeBeam != null && _activeBeam.IsActive)
                {
                    _activeBeam.UpdateOriginDirection(origin, direction);
                }

                UpdateChildBeamSegments(origin, direction);
                return;
            }

            if (_activeBeam != null && _activeBeam.IsActive)
            {
                _activeBeam.UpdateOriginDirection(origin, direction);
            }
        }

        /// <summary>
        /// 발사 시작(클릭/눌림 이벤트)
        /// - 이미 빔이 있으면 즉시 새 빔을 만들기 위해 기존 빔은 FlyAway로 전환
        /// </summary>
        public void StartFire()
        {
            if (_field == null || LaserData == null) return;

            Vector2 origin = Muzzle.position;
            Vector2 direction = Muzzle.right;

            StopFireInternal();

            if (LaserData.ReflectMode == LaserReflectMode.ChildBeam)
            {
                if (logReflect)
                {
                    Debug.Log($"[PolarLaserWeapon] StartFire ChildBeam reflectCount={LaserData.ReflectCount}, bundle(main)={LaserData.ProjectileBundleId}, bundle(child)={LaserData.ChildBeamProjectileBundleId}");
                }

                SpawnBeam(origin, direction);
                SpawnChildBeamSegments(origin, direction);
            }
            else
            {
                SpawnBeam(origin, direction);
            }
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
                beam.SetDamageMultiplier(1f);
                beam.Launch(_field, LaserData, origin, direction);
                _activeBeam = beam;
            }
        }

        private void SpawnChildBeamSegments(Vector2 origin, Vector2 direction)
        {
            if (PoolService.Instance == null)
            {
                Debug.LogError("[PolarLaserWeapon] PoolService not available!");
                return;
            }

            string bundleId = !string.IsNullOrEmpty(LaserData.ChildBeamProjectileBundleId)
                ? LaserData.ChildBeamProjectileBundleId
                : LaserData.ProjectileBundleId;

            if (logReflect)
            {
                Debug.Log($"[PolarLaserWeapon] ChildBeam bundle resolved: '{bundleId}' (child='{LaserData.ChildBeamProjectileBundleId}', main='{LaserData.ProjectileBundleId}')");
            }

            if (string.IsNullOrEmpty(bundleId))
            {
                Debug.LogError("[PolarLaserWeapon] Projectile bundle id is empty!");
                return;
            }

            int reflectCount = Mathf.Max(0, LaserData.ReflectCount);
            if (reflectCount == 0)
            {
                if (logReflect)
                {
                    Debug.Log("[PolarLaserWeapon] ChildBeam reflectCount=0 (no reflected segments spawned)");
                }
                return;
            }

            _activeChildBeams.Clear();
            for (int i = 0; i < reflectCount; i++)
            {
                int bounceIndex = i + 1;

                PolarLaserProjectile seg = PoolService.Instance.Get<PolarLaserProjectile>(bundleId, origin, Quaternion.identity);
                if (seg == null)
                {
                    if (logReflect)
                    {
                        Debug.LogWarning($"[PolarLaserWeapon] Spawn reflected segment#{bounceIndex} FAILED (bundle='{bundleId}')");
                    }
                    continue;
                }

                float dmgMul = Mathf.Pow(LaserData.ReflectDamageMultiplier, bounceIndex);
                seg.SetDamageMultiplier(dmgMul);
                _activeChildBeams.Add(seg);

                if (logReflect)
                {
                    Debug.Log(
                        $"[PolarLaserWeapon] Spawn reflected segment#{bounceIndex} OK " +
                        $"instance={seg.GetInstanceID()} activeSelf={seg.gameObject.activeSelf} activeInHierarchy={seg.gameObject.activeInHierarchy} IsActive={seg.IsActive} " +
                        $"pos={seg.transform.position} bundle='{seg.PoolBundleId}'"
                    );
                }
            }

            UpdateChildBeamSegments(origin, direction);

            if (logReflect)
            {
                for (int i = 0; i < _activeChildBeams.Count; i++)
                {
                    var seg = _activeChildBeams[i];
                    if (seg == null) continue;

                    Debug.Log(
                        $"[PolarLaserWeapon] PostUpdate segment#{i + 1} instance={seg.GetInstanceID()} activeInHierarchy={seg.gameObject.activeInHierarchy} IsActive={seg.IsActive} " +
                        $"start={seg.SegmentStartPoint} end={seg.SegmentEndPoint}"
                    );
                }
            }
        }

        private void UpdateChildBeamSegments(Vector2 origin, Vector2 direction)
        {
            if (_field == null || LaserData == null)
            {
                return;
            }

            if (_activeChildBeams.Count == 0)
            {
                return;
            }

            Vector2 center = (_field as Component) != null ? ((Component)_field).transform.position : Vector2.zero;

            Vector2 dir0 = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;

            // 0번 충돌점(벽)
            Vector2 hit0 = CalculateWallEndPoint(center, dir0);

            // 총 길이 예산: MaxLength
            float remaining = Mathf.Max(0f, LaserData.MaxLength);

            // base segment 길이 차감 (muzzle -> first wall)
            remaining = Mathf.Max(0f, remaining - Vector2.Distance(origin, hit0));

            // 첫 반사 방향(완전 반사 + 옵션으로 각도 랜덤 편차)
            Vector2 normal0 = (hit0 - center).sqrMagnitude > 0f ? (hit0 - center).normalized : Vector2.right;
            Vector2 currentDir = Vector2.Reflect(dir0, normal0).normalized;
            currentDir = ApplyReflectAngleNoise(currentDir);

            Vector2 currentStart = hit0;

            for (int i = 0; i < _activeChildBeams.Count; i++)
            {
                PolarLaserProjectile seg = _activeChildBeams[i];
                if (seg == null) continue;

                if (remaining <= 0.001f)
                {
                    // 남은 길이가 없으면 더 이상 렌더링/업데이트하지 않음
                    seg.BeginFlyAway();
                    continue;
                }

                // 벽까지의 최대 도달점
                Vector2 wallEnd = CalculateWallEndPoint(center, currentDir);

                // 이번 세그먼트 실제 길이 = 남은 길이 한도 내로 clamp
                float maxThis = Vector2.Distance(currentStart, wallEnd);
                float segLen = Mathf.Min(remaining, maxThis);

                Vector2 end = currentStart + currentDir * segLen;

                // 세그먼트 렌더 업데이트
                seg.LaunchSegment(_field, LaserData, currentStart, end);

                remaining -= segLen;

                if (logReflect && Time.frameCount % 30 == 0)
                {
                    int bounceIndex = i + 1;
                    Debug.Log($"[PolarLaserWeapon] ReflectUpdate#{bounceIndex} start={currentStart} end={end} dir={currentDir} remaining={remaining:F2}");
                }

                currentStart = end;

                // 벽에 닿은 경우에만 다음 반사 계산(아직 벽에 못 닿았으면 종료)
                if (segLen + 0.0001f < maxThis)
                {
                    break;
                }

                Vector2 normal = (currentStart - center).sqrMagnitude > 0f ? (currentStart - center).normalized : Vector2.right;
                currentDir = Vector2.Reflect(currentDir, normal).normalized;
                currentDir = ApplyReflectAngleNoise(currentDir);
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

        private Vector2 CalculateWallEndPoint(Vector2 center, Vector2 dir)
        {
            Vector2 n = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.right;

            float angleDeg = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg;
            if (angleDeg < 0f) angleDeg += 360f;

            int sectorIndex = _field.AngleToSectorIndex(angleDeg);
            float sectorRadius = _field.GetSectorRadius(sectorIndex);

            return center + n * sectorRadius;
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
                if (logReflect)
                {
                    Debug.Log($"[PolarLaserWeapon] StopFire ChildBeam segments={_activeChildBeams.Count}");
                }

                for (int i = 0; i < _activeChildBeams.Count; i++)
                {
                    if (_activeChildBeams[i] != null)
                    {
                        _activeChildBeams[i].BeginFlyAway();
                    }
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
