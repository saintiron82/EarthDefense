using UnityEngine;
using System.Collections.Generic;

namespace Polar.Weapons.Projectiles
{
    /// <summary>
    /// 레이저 투사체 (빔 타입)
    /// - PolarLaserWeaponData와 매칭
    /// - 독립 로직: 빔 확장/수축, 틱 데미지, 섹터 인덱스 계산
    /// - SGSystem PoolService 완전 통합
    /// - ⭐ 중심 섹터 + BeamWidth 기반 범위 판정 (머신건/미사일과 동일한 로직)
    /// </summary>
    public class PolarLaserProjectile : PolarProjectileBase
    {
        [Header("Visual")]
        [SerializeField] private LineRenderer lineRenderer;
        [Header("Debug")]
        [SerializeField] private bool logTickDamage;
        [SerializeField] private bool showDamageGizmos = true;
        [SerializeField] private Color gizmoHitSectorColor = Color.yellow;

        private PolarLaserWeaponData LaserData => _weaponData as PolarLaserWeaponData;
        
        // Gizmo 디버그용
        private List<int> _lastHitSectors = new List<int>();
        private int _lastCenterSectorIndex;
        private int _lastDamageRadius;
        private bool _hasLastHitData;
        
        private bool _isFlyingAway; // 소멸 과정 (origin 전진 중)
        private float _currentLength;
        private float _nextTickTime;
        private Vector2 _direction;
        private Vector2 _origin;
        private int _tickCount;

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
            _isFlyingAway = false;
            _currentLength = 0f;
            // ...existing code...
            float tickInterval = _weaponData != null ? 1f / Mathf.Max(0.0001f, _weaponData.TickRate) : 0.1f;
            _nextTickTime = Time.time + tickInterval;
            _tickCount = 0;
            _hitSectorsThisTick.Clear();
            _lastHitTimeBySector.Clear();
            
            if (logTickDamage)
            {
                Debug.Log($"[PolarLaserProjectile] ========== INITIALIZE BEAM ==========");
                Debug.Log($"  Time: {Time.time:F4}s, Frame: {Time.frameCount}");
                Debug.Log($"  Origin: {_origin}, Direction: {_direction}");
                Debug.Log($"  TickRate: {_weaponData?.TickRate:F2}, TickInterval: {tickInterval:F4}s");
                Debug.Log($"  NextTickTime: {_nextTickTime:F4}s (delay: {tickInterval:F4}s)");
                Debug.Log($"========================================================\n");
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
            
            if (logTickDamage)
            {
                Debug.Log($"[PolarLaserProjectile] ========== BEGIN FLY AWAY ==========");
                Debug.Log($"  Entering disappearance phase (origin will advance)");
                Debug.Log($"  Time: {Time.time:F4}s, Frame: {Time.frameCount}");
                Debug.Log($"  Current: Origin={_origin}, Length={_currentLength:F2}");
                Debug.Log($"==========================================================\n");
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

        /// <summary>
        /// 풀 반환 시 추가 정리
        /// </summary>
        protected override void OnPoolReturn()
        {
            _isFlyingAway = false;
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

            // ✅ FlyAway 완료 시 풀 반환 (빔 길이가 거의 0이 되면)
            if (_isFlyingAway && _currentLength <= 0.1f)
            {
                ReturnToPool();
            }
        }

        private void UpdateBeamLength(float deltaTime)
        {
            if (_isFlyingAway)
            {
                // 소멸 과정: origin 전진, length 감소 (자연스럽게 사라짐)
                float flySpeed = LaserData != null ? LaserData.RetractSpeed : 70f;
                _origin += _direction * flySpeed * deltaTime;
                _currentLength = Mathf.Max(0f, _currentLength - flySpeed * deltaTime);
                return;
            }

            // 정상 상태: 타겟 길이로 확장/축소
            float targetLength = ComputeTargetLength();
            float speed = targetLength >= _currentLength ? LaserData.ExtendSpeed : LaserData.RetractSpeed;
            _currentLength = Mathf.MoveTowards(_currentLength, targetLength, speed * deltaTime);
        }

        private float ComputeTargetLength()
        {
            if (_field == null) return 0f;

            Vector2 center = (_field as Component) != null 
                ? ((Component)_field).transform.position 
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

            // 상태와 무관하게 항상 origin + direction * length
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
            if (_weaponData == null || _currentLength <= 0f) return;
            
            // 틱 간격 체크 (상태와 무관)
            if (Time.time < _nextTickTime) return;

            // 상태와 무관하게 항상 origin + direction * length
            Vector2 hitPoint = _origin + _direction * _currentLength;
            
            if (logTickDamage)
            {
                string state = _isFlyingAway ? "FLYING AWAY" : "HOLDING";
                Debug.Log($"[PolarLaserProjectile] ========== TICK #{_tickCount} START ({state}) ==========");
                Debug.Log($"  Time: {Time.time:F4}s, Frame: {Time.frameCount}");
                Debug.Log($"  Origin: {_origin}, Direction: {_direction}");
                Debug.Log($"  CurrentLength: {_currentLength:F2}, HitPoint: {hitPoint}");
            }
            
            // 다중 섹터 타격
            ApplyMultiSectorDamage(hitPoint);

            _tickCount++;
            _nextTickTime = Time.time + 1f / Mathf.Max(0.0001f, _weaponData.TickRate);
            
            if (logTickDamage)
            {
                Debug.Log($"[PolarLaserProjectile] ========== TICK #{_tickCount - 1} END ==========\n");
            }
        }

        /// <summary>
        /// ⭐ 빔 중심 섹터 + BeamWidth 기반 범위 판정 (머신건/미사일과 동일한 패턴)
        /// - O(1) 복잡도: 섹터 수와 무관하게 일정한 연산량
        /// - 코드 일관성: 모든 Polar 무기가 동일한 판정 로직 사용
        /// </summary>
        private void ApplyMultiSectorDamage(Vector2 hitPoint)
        {
            if (_field == null || _weaponData == null) return;

            Vector2 center = (_field as Component) != null 
                ? ((Component)_field).transform.position 
                : Vector2.zero;

            // 1. 빔 중심 방향의 섹터 특정 (O(1))
            float beamAngle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            if (beamAngle < 0f) beamAngle += 360f;
            int centerSectorIndex = _field.AngleToSectorIndex(beamAngle);
            
            // 2. BeamWidth 기반 영향 범위 계산
            float beamRadius = LaserData.BeamWidth / 2f;
            float avgSectorRadius = _field.GetSectorRadius(centerSectorIndex);
            float beamArcAngle = (beamRadius / avgSectorRadius) * Mathf.Rad2Deg;
            float sectorAngleSize = 360f / _field.SectorCount;
            int damageRadius = Mathf.Max(0, Mathf.CeilToInt(beamArcAngle / sectorAngleSize));
            
            float damagePerTick = _weaponData.Damage / Mathf.Max(0.0001f, _weaponData.TickRate);

            if (logTickDamage)
            {
                Debug.Log($"  [BeamDamage] Center sector: {centerSectorIndex}, Damage radius: {damageRadius} sectors");
                Debug.Log($"  [BeamDamage] BeamWidth: {LaserData.BeamWidth:F3}, BeamArcAngle: {beamArcAngle:F2}°");
                Debug.Log($"  [BeamDamage] DamagePerTick: {damagePerTick:F2}");
            }

            _hitSectorsThisTick.Clear();

            // Gizmo 디버그용 데이터 저장
            if (showDamageGizmos)
            {
                _lastCenterSectorIndex = centerSectorIndex;
                _lastDamageRadius = damageRadius;
                _lastHitSectors.Clear();
                _hasLastHitData = true;
            }

            // 넉백 파워 한 번만 설정 (중복 호출 방지)
            _field.SetLastWeaponKnockback(_weaponData.KnockbackPower);
            
            // 3. 중심 섹터 타격
            if (CanHitSector(centerSectorIndex))
            {
                ApplySectorDamage(centerSectorIndex, damagePerTick);
                _hitSectorsThisTick.Add(centerSectorIndex);
            }
            
            // 4. 주변 섹터 타격 (범위 내에서만)
            // 빔은 균일한 강도이므로 감쇠 없이 동일 데미지 적용
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

            if (logTickDamage)
            {
                Debug.Log($"[PolarLaserProjectile] Tick #{_tickCount}: total sectors hit={_hitSectorsThisTick.Count}, beamWidth={LaserData.BeamWidth:F2}");
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

            // 넉백 파워는 ApplyMultiSectorDamage()에서 한 번만 설정됨
            _field.ApplyDamageToSector(sectorIndex, damage);

            if (_field.EnableWoundSystem)
            {
                _field.ApplyWound(sectorIndex, _weaponData.WoundIntensity);
            }

            // Gizmo용 타격 섹터 기록
            if (showDamageGizmos && _hasLastHitData)
            {
                _lastHitSectors.Add(sectorIndex);
            }
        }

        /// <summary>
        /// Gizmo로 실제 데미지 범위 시각화 (중심 섹터 + 범위)
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDamageGizmos || !_hasLastHitData || _field == null) return;

            Vector2 center = (_field as Component) != null 
                ? ((Component)_field).transform.position 
                : Vector2.zero;

            // 1. 빔 라인 (녹색)
            Vector2 beamEnd = _origin + _direction * _currentLength;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_origin, beamEnd);

            // 2. 빔 시작점과 끝점 표시
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_origin, 0.1f);
            Gizmos.DrawWireSphere(beamEnd, 0.15f);

            // 3. 중심 섹터 표시 (빨간색 - 강조)
            float centerAngle = _field.SectorIndexToAngle(_lastCenterSectorIndex);
            Vector2 centerDir = new Vector2(Mathf.Cos(centerAngle * Mathf.Deg2Rad), Mathf.Sin(centerAngle * Mathf.Deg2Rad));
            float centerRadius = _field.GetSectorRadius(_lastCenterSectorIndex);
            Vector2 centerPoint = center + centerDir * centerRadius;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(centerPoint, 0.15f);
            Gizmos.DrawLine(center, centerPoint);

            // 4. 타격된 모든 섹터 표시
            foreach (int sectorIndex in _lastHitSectors)
            {
                if (sectorIndex == _lastCenterSectorIndex) continue; // 중심은 이미 표시됨

                float sectorAngle = _field.SectorIndexToAngle(sectorIndex);
                Vector2 sectorDir = new Vector2(Mathf.Cos(sectorAngle * Mathf.Deg2Rad), Mathf.Sin(sectorAngle * Mathf.Deg2Rad));
                float sectorRadius = _field.GetSectorRadius(sectorIndex);
                Vector2 sectorPoint = center + sectorDir * sectorRadius;

                // 타격된 섹터는 노란색
                Gizmos.color = gizmoHitSectorColor;
                
                // 섹터 위치에 작은 구체
                Gizmos.DrawWireSphere(sectorPoint, 0.1f);
                
                // 중심에서 섹터까지 라인 (얇게)
                Gizmos.DrawLine(center, sectorPoint);
                
                // 섹터 인덱스 텍스트 (UnityEditor에서만)
#if UNITY_EDITOR
                UnityEditor.Handles.Label(sectorPoint, $"#{sectorIndex}");
#endif
            }

            // 5. 중심점 표시 (흰색)
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(center, 0.2f);
            
            // 6. 범례 표시
#if UNITY_EDITOR
            Vector3 legendPos = _origin + Vector2.up * 2f;
            UnityEditor.Handles.Label(legendPos, 
                $"Center Sector: #{_lastCenterSectorIndex}\n" +
                $"Damage Radius: {_lastDamageRadius} sectors\n" +
                $"Sectors Hit: {_lastHitSectors.Count}\n" +
                $"Beam Length: {_currentLength:F2}");
#endif
        }
    }
}
