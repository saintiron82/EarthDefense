using UnityEngine;
using System.Collections.Generic;
using Script.SystemCore.Pool;
using Polar.Weapons;

namespace Polar.Weapons.Projectiles
{
    /// <summary>
    /// 레이저 투사체 (빔 타입)
    /// - 두 가지 반사 모드 지원:
    ///   1. Segment: 하나의 LineRenderer로 연결된 빔
    ///   2. ChildBeam: 독립 빔들이 부모-자식 관계로 연결
    /// </summary>
    public class PolarLaserProjectile : PolarProjectileBase
    {
        [Header("Visual")]
        [SerializeField] private LineRenderer lineRenderer;
        [Header("Debug")]
        [SerializeField] private bool logTickDamage;
        [SerializeField] private bool showDamageGizmos = true;
        [SerializeField] private Color gizmoHitSectorColor = Color.yellow;
        [SerializeField] private Color gizmoReflectColor = Color.magenta;

        private PolarLaserWeaponData LaserData => _weaponData as PolarLaserWeaponData;

        // Gizmo 디버그용
        private List<int> _lastHitSectors = new List<int>();
        private int _lastCenterSectorIndex;
        private int _lastDamageRadius;
        private bool _hasLastHitData;

        private bool _isFlyingAway;
        private float _currentLength;
        private float _nextTickTime;
        private Vector2 _direction;
        private Vector2 _origin;
        private int _tickCount;

        // ========== 세그먼트 방식 ==========
        private struct ReflectSegment
        {
            public Vector2 start;
            public Vector2 end;
            public Vector2 direction;
            public float damageMultiplier;
        }
        private readonly List<ReflectSegment> _reflectSegments = new List<ReflectSegment>();

        // 세그먼트용 고정 반사 각도 오프셋 (초기화 시 한 번만 계산)
        private float[] _segmentAngleOffsets;

        // ========== 자식 빔 방식 ==========
        private int _remainingReflects;
        private float _damageMultiplier;
        private bool _hasSpawnedReflect;
        private PolarLaserProjectile _childBeam;
        private PolarLaserProjectile _parentBeam;

        // 자식 빔용 고정 반사 각도 오프셋
        private float _reflectAngleOffset;

        // 다중 타격 지원
        private readonly HashSet<int> _hitSectorsThisTick = new HashSet<int>();
        private readonly Dictionary<int, float> _lastHitTimeBySector = new Dictionary<int, float>();
        private const float RehitCooldown = 0.05f;

        #region Launch Methods

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

            _remainingReflects = LaserData.ReflectCount;
            _damageMultiplier = 1f;
            _hasSpawnedReflect = false;
            _childBeam = null;
            _parentBeam = null;

            // 반사 각도 오프셋 초기화 (한 번만)
            InitializeReflectAngles();

            InitializeBeam();
        }

        public void Launch(IPolarField field, PolarWeaponData weaponData, Vector2 origin, Vector2 direction)
        {
            Launch(field, weaponData);
            _origin = origin;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        }

        /// <summary>
        /// 자식 빔용 Launch (ChildBeam 모드)
        /// </summary>
        public void LaunchAsChild(IPolarField field, PolarWeaponData weaponData, Vector2 origin, Vector2 direction,
            int remainingReflects, float damageMultiplier, PolarLaserProjectile parent, float angleOffset)
        {
            Launch(field, weaponData, origin, direction);
            _remainingReflects = remainingReflects;
            _damageMultiplier = damageMultiplier;
            _parentBeam = parent;
            _reflectAngleOffset = angleOffset;
            _hasSpawnedReflect = false;

            if (logTickDamage)
            {
                Debug.Log($"[ChildBeam] Spawned - Remaining: {remainingReflects}, Multiplier: {damageMultiplier:P0}");
            }
        }

        /// <summary>
        /// 반사 각도 오프셋 초기화 (세그먼트 모드용)
        /// </summary>
        private void InitializeReflectAngles()
        {
            int maxReflects = LaserData != null ? LaserData.ReflectCount : 0;
            float angleRange = LaserData != null ? LaserData.ReflectAngleRange : 0f;

            _segmentAngleOffsets = new float[maxReflects];
            for (int i = 0; i < maxReflects; i++)
            {
                if (angleRange > 0f)
                {
                    _segmentAngleOffsets[i] = Random.Range(-angleRange * 0.5f, angleRange * 0.5f);
                }
                else
                {
                    _segmentAngleOffsets[i] = 0f;
                }
            }

            // 자식 빔용 오프셋도 초기화
            if (angleRange > 0f)
            {
                _reflectAngleOffset = Random.Range(-angleRange * 0.5f, angleRange * 0.5f);
            }
            else
            {
                _reflectAngleOffset = 0f;
            }
        }

        #endregion

        #region Lifecycle

        private void InitializeBeam()
        {
            _isFlyingAway = false;
            _currentLength = 0f;
            float tickInterval = _weaponData != null ? 1f / Mathf.Max(0.0001f, _weaponData.TickRate) : 0.1f;
            _nextTickTime = Time.time + tickInterval;
            _tickCount = 0;
            _hitSectorsThisTick.Clear();
            _lastHitTimeBySector.Clear();
            _reflectSegments.Clear();

            if (logTickDamage)
            {
                Debug.Log($"[PolarLaserProjectile] Initialize - Mode: {LaserData.ReflectMode}, ReflectCount: {LaserData.ReflectCount}");
            }

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

        public void BeginFlyAway()
        {
            _isFlyingAway = true;

            // 자식 빔도 FlyAway
            if (_childBeam != null && _childBeam._isActive)
            {
                _childBeam.BeginFlyAway();
            }

            if (logTickDamage)
            {
                Debug.Log($"[PolarLaserProjectile] BeginFlyAway - Length: {_currentLength:F2}");
            }
        }

        public override void Deactivate()
        {
            base.Deactivate();

            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        protected override void OnPoolReturn()
        {
            _isFlyingAway = false;
            _currentLength = 0f;
            _direction = Vector2.zero;
            _origin = Vector2.zero;
            _remainingReflects = 0;
            _damageMultiplier = 1f;
            _hasSpawnedReflect = false;
            _childBeam = null;
            _parentBeam = null;
            _pulseTimer = 0f;
            _hitSectorsThisTick.Clear();
            _lastHitTimeBySector.Clear();
            _reflectSegments.Clear();

            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        #endregion

        #region Update

        protected override void OnUpdate(float deltaTime)
        {
            UpdateBeamLength(deltaTime);

            // 모드에 따라 분기
            if (LaserData.ReflectMode == LaserReflectMode.Segment)
            {
                UpdateSegmentMode();
            }
            else
            {
                UpdateChildBeamMode();
            }

            UpdateBeamVisual();
            ApplyTickDamageIfNeeded();

            // FlyAway 완료 시 풀 반환
            if (_isFlyingAway && _currentLength <= 0.1f)
            {
                ReturnToPool();
            }
        }

        private void UpdateBeamLength(float deltaTime)
        {
            // 펄스는 길이 고정
            if (_pulseTimer > 0f) return;

            float speed = LaserData != null ? LaserData.BeamSpeed : 20f;

            if (_isFlyingAway)
            {
                _origin += _direction * speed * deltaTime;
                _currentLength = Mathf.Max(0f, _currentLength - speed * deltaTime);
                return;
            }

            float targetLength = ComputeTargetLength();
            _currentLength = Mathf.MoveTowards(_currentLength, targetLength, speed * deltaTime);
        }

        private float ComputeTargetLength()
        {
            if (_field == null) return 0f;

            Vector2 center = _field.CenterPosition;

            float angleDeg = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            if (angleDeg < 0f) angleDeg += 360f;
            int sectorIndex = _field.AngleToSectorIndex(angleDeg);
            float sectorRadius = _field.GetSectorRadius(sectorIndex);

            float originAlongDir = Vector2.Dot(_origin - center, _direction);
            float availableLength = Mathf.Max(0f, sectorRadius - originAlongDir);

            return Mathf.Min(LaserData.MaxLength, availableLength);
        }

        #endregion

        #region Segment Mode (방식 1)

        /// <summary>
        /// 세그먼트 모드: 모든 반사를 하나의 LineRenderer로 연결
        /// - 고정 각도 반사: 좌/우 교대 회전
        /// - 필드 경계까지 Ray trace
        /// </summary>
        private void UpdateSegmentMode()
        {
            _reflectSegments.Clear();
            if (_field == null || LaserData == null || _currentLength <= 0f) return;
            if (_isFlyingAway) return;

            Vector2 center = _field.CenterPosition;
            int maxReflects = LaserData.ReflectCount;

            // 첫 번째 세그먼트
            Vector2 currentStart = _origin;
            Vector2 currentDir = _direction;
            float currentMultiplier = 1f;

            Vector2 hitPoint = currentStart + currentDir * _currentLength;

            _reflectSegments.Add(new ReflectSegment
            {
                start = currentStart,
                end = hitPoint,
                direction = currentDir,
                damageMultiplier = currentMultiplier
            });

            if (maxReflects <= 0) return;

            // 기본 반사 각도 (reflectAngleRange가 0이면 60도 사용)
            float baseReflectAngle = LaserData.ReflectAngleRange > 0f ? LaserData.ReflectAngleRange : 60f;

            for (int i = 0; i < maxReflects; i++)
            {
                // 충돌점 기준 좌우 섹터 반경 비교
                Vector2 hitDir = (hitPoint - center).normalized;
                float hitAngle = Mathf.Atan2(hitDir.y, hitDir.x) * Mathf.Rad2Deg;
                if (hitAngle < 0f) hitAngle += 360f;
                int hitSectorIndex = _field.AngleToSectorIndex(hitAngle);

                // 좌우 섹터 반경 확인
                int leftSector = (hitSectorIndex + 1) % _field.SectorCount;
                int rightSector = (hitSectorIndex - 1 + _field.SectorCount) % _field.SectorCount;
                float leftRadius = _field.GetSectorRadius(leftSector);
                float rightRadius = _field.GetSectorRadius(rightSector);

                // 더 밀린 쪽(반경이 작은 쪽)으로 반사
                bool reflectToRight = rightRadius < leftRadius;
                float rotationAngle = reflectToRight
                    ? (180f + baseReflectAngle)   // 우측 반사
                    : (180f - baseReflectAngle);  // 좌측 반사

                // 미리 계산된 랜덤 오프셋 추가
                if (_segmentAngleOffsets != null && i < _segmentAngleOffsets.Length)
                {
                    rotationAngle += _segmentAngleOffsets[i];
                }

                // 현재 방향을 회전
                float rad = rotationAngle * Mathf.Deg2Rad;
                Vector2 reflectedDir = new Vector2(
                    currentDir.x * Mathf.Cos(rad) - currentDir.y * Mathf.Sin(rad),
                    currentDir.x * Mathf.Sin(rad) + currentDir.y * Mathf.Cos(rad)
                );
                reflectedDir = reflectedDir.normalized;

                // 필드 경계까지의 거리 계산
                float segmentLength = TraceRayToFieldBoundary(hitPoint, reflectedDir, center, LaserData.MaxLength);

                if (segmentLength <= 0.1f) break;

                currentMultiplier *= LaserData.ReflectDamageMultiplier;
                Vector2 nextHitPoint = hitPoint + reflectedDir * segmentLength;

                _reflectSegments.Add(new ReflectSegment
                {
                    start = hitPoint,
                    end = nextHitPoint,
                    direction = reflectedDir,
                    damageMultiplier = currentMultiplier
                });

                // 다음 반사 준비
                currentDir = reflectedDir;
                hitPoint = nextHitPoint;
            }
        }

        /// <summary>
        /// Ray를 따라가며 필드 경계와의 충돌점 찾기
        /// </summary>
        private float TraceRayToFieldBoundary(Vector2 origin, Vector2 dir, Vector2 center, float maxDist)
        {
            const float stepSize = 0.5f;
            float prevDist = (origin - center).magnitude;
            bool wasInside = true; // 시작점은 경계 위 또는 안쪽

            for (float t = stepSize; t <= maxDist; t += stepSize)
            {
                Vector2 point = origin + dir * t;
                float distFromCenter = (point - center).magnitude;

                // 이 지점의 섹터 반경 확인
                float angle = Mathf.Atan2(point.y - center.y, point.x - center.x) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;
                int sectorIndex = _field.AngleToSectorIndex(angle);
                float sectorRadius = _field.GetSectorRadius(sectorIndex);

                // 경계를 넘었는지 확인 (안쪽 → 바깥쪽)
                bool isOutside = distFromCenter >= sectorRadius;

                if (wasInside && isOutside)
                {
                    // 경계 교차점 찾음 - 더 정밀하게 보간
                    return t - stepSize * 0.5f;
                }

                // 중심을 지나서 반대편으로 갈 때 (거리가 감소했다가 다시 증가)
                if (distFromCenter < prevDist * 0.5f)
                {
                    wasInside = true; // 중심 근처 통과, 다시 안쪽으로 진입
                }

                prevDist = distFromCenter;
            }

            return maxDist;
        }

        /// <summary>
        /// Ray-Circle Intersection: 광선이 원과 교차하는 거리 계산
        /// - rayOrigin: 광선 시작점
        /// - rayDir: 광선 방향 (normalized)
        /// - circleCenter: 원 중심
        /// - radius: 원 반경
        /// - 반환값: 교차 거리 (교차 없으면 -1)
        /// </summary>
        private float CalculateRayCircleIntersection(Vector2 rayOrigin, Vector2 rayDir, Vector2 circleCenter, float radius)
        {
            // 공식: |P + t*D - C|² = R²
            // 전개: t² + 2*t*(V·D) + |V|² - R² = 0  (V = P - C)
            Vector2 oc = rayOrigin - circleCenter;
            float b = 2f * Vector2.Dot(oc, rayDir);
            float c = Vector2.Dot(oc, oc) - radius * radius;

            float discriminant = b * b - 4f * c;
            if (discriminant < 0f) return -1f; // 교차 없음

            float sqrtDisc = Mathf.Sqrt(discriminant);
            float t1 = (-b - sqrtDisc) * 0.5f;
            float t2 = (-b + sqrtDisc) * 0.5f;

            // 양수인 t 반환 (전방 교차점)
            if (t1 > 0.01f) return t1;
            if (t2 > 0.01f) return t2;
            return -1f;
        }

        #endregion

        #region ChildBeam Mode (방식 2) - 확률적 틱 펄스

        // 펄스 설정
        private const float PulseProbability = 0.5f;  // 틱당 펄스 발생 확률
        private const float PulseLength = 3f;         // 펄스 길이
        private const float PulseDuration = 0.15f;    // 펄스 지속 시간
        private float _pulseTimer;

        /// <summary>
        /// ChildBeam 모드: 틱마다 확률적으로 짧은 펄스 발사
        /// </summary>
        private void UpdateChildBeamMode()
        {
            // 펄스 타이머 감소 및 이동 (펄스로 스폰된 경우)
            if (_pulseTimer > 0f)
            {
                // 펄스 이동 (BeamSpeed로 전진)
                float moveSpeed = LaserData != null ? LaserData.BeamSpeed : 20f;
                _origin += _direction * moveSpeed * Time.deltaTime;

                // 벽 충돌 체크
                if (_field != null)
                {
                    Vector2 center = _field.CenterPosition;
                    Vector2 beamEnd = _origin + _direction * _currentLength;
                    float distFromCenter = (beamEnd - center).magnitude;

                    // 끝점 기준 섹터 반경 확인
                    float endAngle = Mathf.Atan2(beamEnd.y - center.y, beamEnd.x - center.x) * Mathf.Rad2Deg;
                    if (endAngle < 0f) endAngle += 360f;
                    int sectorIndex = _field.AngleToSectorIndex(endAngle);
                    float sectorRadius = _field.GetSectorRadius(sectorIndex);

                    // 벽에 닿으면 충돌 처리
                    if (distFromCenter >= sectorRadius)
                    {
                        // 벽에 맞춰 길이 조정
                        float originDist = (_origin - center).magnitude;
                        _currentLength = Mathf.Max(0.1f, sectorRadius - originDist);

                        // 충돌점에서 데미지 적용
                        Vector2 hitPoint = _origin + _direction * _currentLength;
                        _field.SetLastWeaponKnockback(_weaponData.KnockbackPower);
                        ApplyDamageAtPoint(hitPoint, _direction, _damageMultiplier);

                        // 펄스 종료 (1단만 튀고 끝)
                        _pulseTimer = 0f;
                        ReturnToPool();
                        return;
                    }
                }

                _pulseTimer -= Time.deltaTime;
                if (_pulseTimer <= 0f)
                {
                    ReturnToPool();
                }
            }
        }

        /// <summary>
        /// 틱 데미지 시 펄스 발사 시도 (ChildBeam 모드 전용)
        /// </summary>
        private void TrySpawnReflectPulse()
        {
            if (LaserData.ReflectMode != LaserReflectMode.ChildBeam) return;
            if (_remainingReflects <= 0) return;
            if (PoolService.Instance == null || string.IsNullOrEmpty(LaserData.ProjectileBundleId)) return;

            // 확률 체크
            if (Random.value > PulseProbability) return;

            Vector2 center = _field.CenterPosition;
            Vector2 hitPoint = _origin + _direction * _currentLength;

            // 충돌점 기준 좌우 섹터 반경 비교 (Segment 모드와 동일한 로직)
            Vector2 hitDir = (hitPoint - center).normalized;
            float hitAngle = Mathf.Atan2(hitDir.y, hitDir.x) * Mathf.Rad2Deg;
            if (hitAngle < 0f) hitAngle += 360f;
            int hitSectorIndex = _field.AngleToSectorIndex(hitAngle);

            int leftSector = (hitSectorIndex + 1) % _field.SectorCount;
            int rightSector = (hitSectorIndex - 1 + _field.SectorCount) % _field.SectorCount;
            float leftRadius = _field.GetSectorRadius(leftSector);
            float rightRadius = _field.GetSectorRadius(rightSector);

            // 반사 각도 계산
            float baseReflectAngle = LaserData.ReflectAngleRange > 0f ? LaserData.ReflectAngleRange : 60f;
            bool reflectToRight = rightRadius < leftRadius;
            float rotationAngle = reflectToRight
                ? (180f + baseReflectAngle)
                : (180f - baseReflectAngle);

            // 랜덤 오프셋 추가
            rotationAngle += Random.Range(-10f, 10f);

            // 반사 방향 계산
            float rad = rotationAngle * Mathf.Deg2Rad;
            Vector2 pulseDir = new Vector2(
                _direction.x * Mathf.Cos(rad) - _direction.y * Mathf.Sin(rad),
                _direction.x * Mathf.Sin(rad) + _direction.y * Mathf.Cos(rad)
            );
            pulseDir = pulseDir.normalized;

            // 펄스 스폰
            PolarLaserProjectile pulse = PoolService.Instance.Get<PolarLaserProjectile>(
                LaserData.ProjectileBundleId,
                hitPoint,
                Quaternion.identity
            );

            if (pulse != null)
            {
                float newMultiplier = _damageMultiplier * LaserData.ReflectDamageMultiplier;
                pulse.LaunchAsPulse(
                    _field,
                    _weaponData,
                    hitPoint,
                    pulseDir,
                    PulseLength,
                    PulseDuration,
                    _remainingReflects - 1,
                    newMultiplier
                );

                if (logTickDamage)
                {
                    Debug.Log($"[Pulse] Spawned at {hitPoint}, Dir: {pulseDir}, Remaining: {_remainingReflects - 1}");
                }
            }
        }

        /// <summary>
        /// 벽 충돌 시 연쇄 펄스 스폰 (확률 체크 없음)
        /// </summary>
        private void TrySpawnReflectPulseAtPoint(Vector2 hitPoint)
        {
            if (LaserData == null) return;
            if (PoolService.Instance == null || string.IsNullOrEmpty(LaserData.ProjectileBundleId)) return;

            Vector2 center = _field.CenterPosition;

            // 충돌점 기준 좌우 섹터 반경 비교
            Vector2 hitDir = (hitPoint - center).normalized;
            float hitAngle = Mathf.Atan2(hitDir.y, hitDir.x) * Mathf.Rad2Deg;
            if (hitAngle < 0f) hitAngle += 360f;
            int hitSectorIndex = _field.AngleToSectorIndex(hitAngle);

            int leftSector = (hitSectorIndex + 1) % _field.SectorCount;
            int rightSector = (hitSectorIndex - 1 + _field.SectorCount) % _field.SectorCount;
            float leftRadius = _field.GetSectorRadius(leftSector);
            float rightRadius = _field.GetSectorRadius(rightSector);

            // 반사 각도 계산
            float baseReflectAngle = LaserData.ReflectAngleRange > 0f ? LaserData.ReflectAngleRange : 60f;
            bool reflectToRight = rightRadius < leftRadius;
            float rotationAngle = reflectToRight
                ? (180f + baseReflectAngle)
                : (180f - baseReflectAngle);

            // 랜덤 오프셋 추가
            rotationAngle += Random.Range(-10f, 10f);

            // 반사 방향 계산
            float rad = rotationAngle * Mathf.Deg2Rad;
            Vector2 pulseDir = new Vector2(
                _direction.x * Mathf.Cos(rad) - _direction.y * Mathf.Sin(rad),
                _direction.x * Mathf.Sin(rad) + _direction.y * Mathf.Cos(rad)
            );
            pulseDir = pulseDir.normalized;

            // 펄스 스폰
            PolarLaserProjectile pulse = PoolService.Instance.Get<PolarLaserProjectile>(
                LaserData.ProjectileBundleId,
                hitPoint,
                Quaternion.identity
            );

            if (pulse != null)
            {
                float newMultiplier = _damageMultiplier * LaserData.ReflectDamageMultiplier;
                pulse.LaunchAsPulse(
                    _field,
                    _weaponData,
                    hitPoint,
                    pulseDir,
                    PulseLength,
                    PulseDuration,
                    _remainingReflects - 1,
                    newMultiplier
                );

                if (logTickDamage)
                {
                    Debug.Log($"[Pulse Chain] Spawned at {hitPoint}, Dir: {pulseDir}, Remaining: {_remainingReflects - 1}");
                }
            }
        }

        /// <summary>
        /// 펄스로 발사 (짧은 지속시간)
        /// </summary>
        public void LaunchAsPulse(IPolarField field, PolarWeaponData weaponData, Vector2 origin, Vector2 direction,
            float length, float duration, int remainingReflects, float damageMultiplier)
        {
            _field = field;
            _weaponData = weaponData;
            _isActive = true;
            _origin = origin;
            _direction = direction.normalized;
            _currentLength = length;
            _remainingReflects = remainingReflects;
            _damageMultiplier = damageMultiplier;
            _pulseTimer = duration;
            _isFlyingAway = false;

            // 반사 각도 초기화
            InitializeReflectAngles();

            float tickInterval = _weaponData != null ? 1f / Mathf.Max(0.0001f, _weaponData.TickRate) : 0.1f;
            _nextTickTime = Time.time + tickInterval;

            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                // 펄스는 더 밝고 얇게
                Color pulseColor = LaserData.BeamColor;
                pulseColor.a = 0.8f;
                lineRenderer.startColor = pulseColor;
                lineRenderer.endColor = pulseColor;
                lineRenderer.startWidth = LaserData.BeamWidth * damageMultiplier;
                lineRenderer.endWidth = LaserData.BeamWidth * damageMultiplier * 0.5f;
                lineRenderer.positionCount = 2;
            }
        }

        #endregion

        #region Visual

        private void UpdateBeamVisual()
        {
            if (lineRenderer == null) return;
            if (LaserData == null) return;

            if (LaserData.ReflectMode == LaserReflectMode.Segment && _reflectSegments.Count > 1)
            {
                // 세그먼트 모드: 모든 포인트 연결
                lineRenderer.positionCount = _reflectSegments.Count + 1;
                for (int i = 0; i < _reflectSegments.Count; i++)
                {
                    lineRenderer.SetPosition(i, _reflectSegments[i].start);
                }
                lineRenderer.SetPosition(_reflectSegments.Count, _reflectSegments[_reflectSegments.Count - 1].end);

                // 반사될수록 두께 감소 (widthCurve 사용)
                UpdateSegmentWidthCurve();
            }
            else
            {
                // 단일 빔 또는 자식 빔 모드
                lineRenderer.positionCount = 2;
                Vector2 end = _origin + _direction * _currentLength;
                lineRenderer.SetPosition(0, _origin);
                lineRenderer.SetPosition(1, end);
            }
        }

        /// <summary>
        /// 세그먼트별 두께 감소 처리
        /// damageMultiplier를 두께에도 적용 (반사될수록 가늘어짐)
        /// </summary>
        private void UpdateSegmentWidthCurve()
        {
            if (lineRenderer == null || _reflectSegments.Count == 0) return;

            float baseWidth = LaserData.BeamWidth;
            int pointCount = _reflectSegments.Count + 1;

            // 각 포인트의 누적 거리 계산
            float[] distances = new float[pointCount];
            distances[0] = 0f;
            float totalLength = 0f;

            for (int i = 0; i < _reflectSegments.Count; i++)
            {
                float segLen = (_reflectSegments[i].end - _reflectSegments[i].start).magnitude;
                totalLength += segLen;
                distances[i + 1] = totalLength;
            }

            if (totalLength <= 0f) return;

            // AnimationCurve로 두께 설정
            Keyframe[] keys = new Keyframe[pointCount];
            float currentMultiplier = 1f;

            for (int i = 0; i < pointCount; i++)
            {
                float t = distances[i] / totalLength; // 0 ~ 1 정규화
                float width = baseWidth * currentMultiplier;
                keys[i] = new Keyframe(t, width);

                // 다음 세그먼트의 multiplier 적용
                if (i < _reflectSegments.Count)
                {
                    currentMultiplier = _reflectSegments[i].damageMultiplier;
                }
            }

            lineRenderer.widthCurve = new AnimationCurve(keys);
        }

        public void UpdateOriginDirection(Vector2 origin, Vector2 direction)
        {
            _origin = origin;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : _direction;
        }

        #endregion

        #region Damage

        private void ApplyTickDamageIfNeeded()
        {
            if (_weaponData == null || _currentLength <= 0f) return;
            if (Time.time < _nextTickTime) return;

            _hitSectorsThisTick.Clear();
            _lastHitSectors.Clear();
            _hasLastHitData = true;
            _field.SetLastWeaponKnockback(_weaponData.KnockbackPower);

            if (LaserData.ReflectMode == LaserReflectMode.Segment && _reflectSegments.Count > 0)
            {
                // 세그먼트 모드: 각 세그먼트마다 데미지 적용
                foreach (var segment in _reflectSegments)
                {
                    ApplyDamageAtPoint(segment.end, segment.direction, segment.damageMultiplier);
                }
            }
            else
            {
                // ChildBeam/펄스 모드: 자신의 끝점에 데미지
                Vector2 hitPoint = _origin + _direction * _currentLength;
                ApplyDamageAtPoint(hitPoint, _direction, _damageMultiplier);

                // 펄스가 아닌 메인 빔일 때만 펄스 스폰 시도
                if (_pulseTimer <= 0f)
                {
                    TrySpawnReflectPulse();
                }
            }

            _tickCount++;
            _nextTickTime = Time.time + 1f / Mathf.Max(0.0001f, _weaponData.TickRate);
        }

        private void ApplyDamageAtPoint(Vector2 hitPoint, Vector2 beamDirection, float multiplier)
        {
            if (_field == null || _weaponData == null) return;

            // hitPoint 위치 기준으로 섹터 계산 (beamDirection이 아님)
            Vector2 center = _field.CenterPosition;
            Vector2 hitDir = (hitPoint - center).normalized;
            float hitAngle = Mathf.Atan2(hitDir.y, hitDir.x) * Mathf.Rad2Deg;
            if (hitAngle < 0f) hitAngle += 360f;
            int centerSectorIndex = _field.AngleToSectorIndex(hitAngle);

            float beamRadius = LaserData.BeamWidth / 2f;
            float avgSectorRadius = _field.GetSectorRadius(centerSectorIndex);
            float beamArcAngle = (beamRadius / avgSectorRadius) * Mathf.Rad2Deg;
            float sectorAngleSize = 360f / _field.SectorCount;
            int damageRadius = Mathf.Max(0, Mathf.CeilToInt(beamArcAngle / sectorAngleSize));

            float baseDamagePerTick = _weaponData.Damage / Mathf.Max(0.0001f, _weaponData.TickRate);
            float damagePerTick = baseDamagePerTick * multiplier;

            if (showDamageGizmos)
            {
                _lastCenterSectorIndex = centerSectorIndex;
                _lastDamageRadius = damageRadius;
            }

            // 중심 섹터 타격
            if (CanHitSector(centerSectorIndex))
            {
                ApplySectorDamage(centerSectorIndex, damagePerTick);
                _hitSectorsThisTick.Add(centerSectorIndex);
            }

            // 주변 섹터 타격
            for (int offset = 1; offset <= damageRadius; offset++)
            {
                int leftIndex = (centerSectorIndex - offset + _field.SectorCount) % _field.SectorCount;
                int rightIndex = (centerSectorIndex + offset) % _field.SectorCount;

                if (CanHitSector(leftIndex))
                {
                    ApplySectorDamage(leftIndex, damagePerTick);
                    _hitSectorsThisTick.Add(leftIndex);
                }

                if (CanHitSector(rightIndex))
                {
                    ApplySectorDamage(rightIndex, damagePerTick);
                    _hitSectorsThisTick.Add(rightIndex);
                }
            }
        }

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

        private void ApplySectorDamage(int sectorIndex, float damage)
        {
            if (_field == null) return;

            _field.ApplyDamageToSector(sectorIndex, damage);

            if (_field.EnableWoundSystem)
            {
                _field.ApplyWound(sectorIndex, _weaponData.WoundIntensity);
            }

            if (showDamageGizmos && _hasLastHitData)
            {
                _lastHitSectors.Add(sectorIndex);
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!showDamageGizmos || !_hasLastHitData || _field == null) return;

            Vector2 center = _field.CenterPosition;

            // 모드에 따른 빔 시각화
            if (LaserData != null && LaserData.ReflectMode == LaserReflectMode.Segment && _reflectSegments.Count > 1)
            {
                // 세그먼트 모드: 연결된 빔
                for (int i = 0; i < _reflectSegments.Count; i++)
                {
                    var seg = _reflectSegments[i];
                    Gizmos.color = i == 0 ? Color.green : Color.Lerp(gizmoReflectColor, Color.cyan, (float)i / _reflectSegments.Count);
                    Gizmos.DrawLine(seg.start, seg.end);
                    Gizmos.DrawWireSphere(seg.end, 0.1f);

#if UNITY_EDITOR
                    UnityEditor.Handles.Label(seg.end + Vector2.up * 0.2f, $"#{i} ({seg.damageMultiplier:P0})");
#endif
                }
            }
            else
            {
                // 자식 빔 모드 또는 단일 빔
                Vector2 beamEnd = _origin + _direction * _currentLength;
                Gizmos.color = _damageMultiplier < 1f ? Color.cyan : Color.green;
                Gizmos.DrawLine(_origin, beamEnd);
                Gizmos.DrawWireSphere(_origin, 0.1f);
                Gizmos.DrawWireSphere(beamEnd, 0.15f);
            }

            // 타격 섹터 표시
            foreach (int sectorIndex in _lastHitSectors)
            {
                float sectorAngle = _field.SectorIndexToAngle(sectorIndex);
                Vector2 sectorDir = new Vector2(Mathf.Cos(sectorAngle * Mathf.Deg2Rad), Mathf.Sin(sectorAngle * Mathf.Deg2Rad));
                float sectorRadius = _field.GetSectorRadius(sectorIndex);
                Vector2 sectorPoint = center + sectorDir * sectorRadius;

                Gizmos.color = gizmoHitSectorColor;
                Gizmos.DrawWireSphere(sectorPoint, 0.08f);
            }

            // 중심점
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(center, 0.2f);

#if UNITY_EDITOR
            Vector3 legendPos = _origin + Vector2.up * 2f;
            string modeStr = LaserData != null ? LaserData.ReflectMode.ToString() : "Unknown";
            string info = $"Mode: {modeStr}\n";
            if (LaserData != null && LaserData.ReflectMode == LaserReflectMode.Segment)
            {
                info += $"Segments: {_reflectSegments.Count}\n";
            }
            else
            {
                info += $"Remaining: {_remainingReflects}\n";
                info += $"Multiplier: {_damageMultiplier:P0}\n";
            }
            info += $"Sectors Hit: {_lastHitSectors.Count}";
            UnityEditor.Handles.Label(legendPos, info);
#endif
        }

        #endregion
    }
}
