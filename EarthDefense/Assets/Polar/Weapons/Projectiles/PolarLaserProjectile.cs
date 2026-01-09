using UnityEngine;
using System.Collections.Generic;

namespace Polar.Weapons
{
    /// <summary>
    /// 레이저 투사체 (빔 타입)
    /// - PolarLaserWeaponData와 매칭
    /// - 독립 로직: 빔 확장/수축, 틱 데미지, 섹터 인덱스 계산
    /// - SGSystem PoolService 완전 통합
    /// - ⭐ 빔 폭 범위 내 다중 섹터 타격 지원
    /// </summary>
    public class PolarLaserProjectile : PolarProjectileBase
    {
        [Header("Visual")]
        [SerializeField] private LineRenderer lineRenderer;
        [Header("Debug")]
        [SerializeField] private bool logTickDamage = false;

        private PolarLaserWeaponData LaserData => _weaponData as PolarLaserWeaponData;
        
        private bool _isRetracting;
        private float _currentLength;
        private float _nextTickTime;
        private Vector2 _direction;
        private Vector2 _origin;

        // ⭐ 다중 타격 지원
        private readonly HashSet<int> _hitSectorsThisTick = new HashSet<int>();
        private readonly Dictionary<int, float> _lastHitTimeBySector = new Dictionary<int, float>();
        private const float RehitCooldown = 0.05f; // 동일 섹터 재타격 쿨다운

        public override void Launch(IPolarField field, PolarWeaponData weaponData)
        {
            if (weaponData is not PolarLaserWeaponData)
            {
                Debug.LogError("[PolarLaserProjectile] Requires PolarLaserWeaponData!");
                return;
            }

            _field = field;
            _weaponData = weaponData;
            _isActive = true;

            _origin = (_field as Component) != null ? ((Component)_field).transform.position : Vector2.zero;
            _direction = Vector2.right;
            
            InitializeBeam();
        }

        /// <summary>
        /// 특정 방향으로 발사 (오버로드)
        /// </summary>
        public void Launch(IPolarField field, PolarWeaponData weaponData, Vector2 origin, Vector2 direction)
        {
            Launch(field, weaponData);
            _origin = origin;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        }

        private void InitializeBeam()
        {
            _isRetracting = false;
            _currentLength = 0f;
            _nextTickTime = Time.time;
            _hitSectorsThisTick.Clear();
            _lastHitTimeBySector.Clear();
            
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.startColor = LaserData.BeamColor;
                lineRenderer.endColor = LaserData.BeamColor;
                lineRenderer.startWidth = LaserData.BeamWidth;
                lineRenderer.endWidth = LaserData.BeamWidth;
                lineRenderer.positionCount = 2;
            }
        }

        public void BeginRetract()
        {
            _isRetracting = true;
        }

        public override void Deactivate()
        {
            base.Deactivate();
            
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// 풀 반환 시 추가 정리
        /// </summary>
        protected override void OnPoolReturn()
        {
            _isRetracting = false;
            _currentLength = 0f;
            _direction = Vector2.zero;
            _origin = Vector2.zero;
            _hitSectorsThisTick.Clear();
            _lastHitTimeBySector.Clear();
            
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            UpdateBeamLength(deltaTime);
            UpdateBeamVisual();
            ApplyTickDamageIfNeeded();

            // ✅ 리트랙트 완료 시 풀 반환
            if (_isRetracting && _currentLength <= 0f)
            {
                ReturnToPool();
            }
        }

        private void UpdateBeamLength(float deltaTime)
        {
            if (_isRetracting)
            {
                _currentLength = Mathf.MoveTowards(_currentLength, 0f, LaserData.RetractSpeed * deltaTime);
                return;
            }

            float targetLength = ComputeTargetLength();
            float speed = targetLength >= _currentLength ? LaserData.ExtendSpeed : LaserData.RetractSpeed;
            _currentLength = Mathf.MoveTowards(_currentLength, targetLength, speed * deltaTime);
        }

        private float ComputeTargetLength()
        {
            if (_field == null) return 0f;

            Vector2 center = (_field as Component) != null 
                ? (Vector2)((Component)_field).transform.position 
                : Vector2.zero;

            // 방향 기준 각도로 섹터 조회
            float angleDeg = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            if (angleDeg < 0f) angleDeg += 360f;
            int sectorIndex = _field.AngleToSectorIndex(angleDeg);
            float sectorRadius = _field.GetSectorRadius(sectorIndex);

            // 머즐이 중심에서 떨어져 있으면 남은 길이를 계산
            float originAlongDir = Vector2.Dot(_origin - center, _direction);
            float availableLength = Mathf.Max(0f, sectorRadius - originAlongDir);

            return Mathf.Min(LaserData.MaxLength, availableLength);
        }

        private void UpdateBeamVisual()
        {
            if (lineRenderer == null) return;

            Vector2 end = _origin + _direction * _currentLength;
            lineRenderer.SetPosition(0, _origin);
            lineRenderer.SetPosition(1, end);
        }

        /// <summary>
        /// 홀드 상태에서 머즐 이동/회전에 맞춰 기원/방향 갱신
        /// </summary>
        public void UpdateOriginDirection(Vector2 origin, Vector2 direction)
        {
            _origin = origin;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : _direction;
        }

        private void ApplyTickDamageIfNeeded()
        {
            // 리트랙트 중에는 피해 틱을 중단해 중복 타격을 방지
            if (_weaponData == null || _isRetracting) return;
            if (Time.time < _nextTickTime || _currentLength <= 0f) return;

            Vector2 hitPoint = _origin + _direction * _currentLength;
            
            // ⭐ 다중 섹터 타격
            ApplyMultiSectorDamage(hitPoint);

            _nextTickTime = Time.time + 1f / Mathf.Max(0.0001f, _weaponData.TickRate);
        }

        /// <summary>
        /// ⭐ 첫 히트 지점 기준 빔 폭 범위 내 모든 섹터에 데미지 적용
        /// </summary>
        private void ApplyMultiSectorDamage(Vector2 hitPoint)
        {
            if (_field == null || _weaponData == null) return;

            Vector2 center = (_field as Component) != null 
                ? (Vector2)((Component)_field).transform.position 
                : Vector2.zero;

            Vector2 dir = hitPoint - center;
            if (dir.sqrMagnitude <= Mathf.Epsilon) return;

            float hitAngleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (hitAngleDeg < 0f) hitAngleDeg += 360f;
            
            int centerSectorIndex = _field.AngleToSectorIndex(hitAngleDeg);
            float beamRadius = LaserData.BeamWidth / 2f;
            float damagePerTick = _weaponData.Damage / Mathf.Max(0.0001f, _weaponData.TickRate);

            _hitSectorsThisTick.Clear();

            // 1. 중심 섹터 타격
            if (CanHitSector(centerSectorIndex))
            {
                ApplySectorDamage(centerSectorIndex, damagePerTick);
                _hitSectorsThisTick.Add(centerSectorIndex);
            }

            // 2. 빔 폭 범위 내 인접 섹터 검색
            FindAndDamageNearbySectors(hitPoint, center, hitAngleDeg, beamRadius, damagePerTick);

            if (logTickDamage)
            {
                Debug.Log($"[PolarLaserProjectile] Multi-hit: center={centerSectorIndex}, total={_hitSectorsThisTick.Count}, angle={hitAngleDeg:F1}°, beamWidth={LaserData.BeamWidth:F2}");
            }
        }

        /// <summary>
        /// 빔 폭 범위 내 추가 섹터 검색 및 타격
        /// </summary>
        private void FindAndDamageNearbySectors(Vector2 hitPoint, Vector2 center, float centerAngle, float searchRadius, float damagePerTick)
        {
            if (_field == null) return;

            int sectorCount = _field.SectorCount;
            float degreesPerSector = 360f / sectorCount;

            // 검색 범위: 빔 폭에 따른 각도 범위 계산
            float hitRadius = Vector2.Distance(hitPoint, center);
            if (hitRadius <= Mathf.Epsilon) return;

            // 빔 폭에 해당하는 각도 범위 (호의 길이 = 반지름 × 각도(라디안))
            // searchAngle ≈ 2 × arcsin(searchRadius / hitRadius)
            float searchAngleRad = 2f * Mathf.Asin(Mathf.Clamp01(searchRadius / hitRadius));
            float searchAngleDeg = searchAngleRad * Mathf.Rad2Deg;
            
            // 검색할 섹터 개수 (양쪽)
            int searchRange = Mathf.CeilToInt(searchAngleDeg / degreesPerSector);

            // 중심 섹터 기준 양쪽 검색
            int centerSectorIndex = _field.AngleToSectorIndex(centerAngle);
            
            for (int offset = -searchRange; offset <= searchRange; offset++)
            {
                if (offset == 0) continue; // 중심 섹터는 이미 처리됨

                int sectorIndex = (centerSectorIndex + offset + sectorCount) % sectorCount;
                
                if (_hitSectorsThisTick.Contains(sectorIndex)) continue;
                if (!CanHitSector(sectorIndex)) continue;

                // 해당 섹터가 실제로 빔 범위 내에 있는지 확인
                float sectorAngle = sectorIndex * degreesPerSector;
                Vector2 sectorDir = new Vector2(Mathf.Cos(sectorAngle * Mathf.Deg2Rad), Mathf.Sin(sectorAngle * Mathf.Deg2Rad));
                Vector2 sectorPoint = center + sectorDir * hitRadius;

                // 히트 포인트로부터의 거리 체크
                if (Vector2.Distance(sectorPoint, hitPoint) <= searchRadius)
                {
                    ApplySectorDamage(sectorIndex, damagePerTick);
                    _hitSectorsThisTick.Add(sectorIndex);
                }
            }
        }

        /// <summary>
        /// 섹터 재타격 쿨다운 체크
        /// </summary>
        private bool CanHitSector(int sectorIndex)
        {
            if (_lastHitTimeBySector.TryGetValue(sectorIndex, out float lastTime))
            {
                if (Time.time - lastTime < RehitCooldown)
                {
                    return false;
                }
            }

            _lastHitTimeBySector[sectorIndex] = Time.time;
            return true;
        }

        /// <summary>
        /// 단일 섹터에 데미지 적용
        /// </summary>
        private void ApplySectorDamage(int sectorIndex, float damage)
        {
            if (_field == null) return;

            _field.SetLastWeaponKnockback(_weaponData.KnockbackPower);
            _field.ApplyDamageToSector(sectorIndex, damage);

            if (_field.EnableWoundSystem)
            {
                _field.ApplyWound(sectorIndex, _weaponData.WoundIntensity);
            }
        }
    }
}
