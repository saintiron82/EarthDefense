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

        private Vector2 _headPos;
        private readonly Queue<Vector2> _samples = new Queue<Vector2>();
        private bool _headInContact;
        private int _lastSampleFrame = -1;

        // Add near other fields
        private float _tailTravelRemaining;

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
            
            _headPos = _origin;
            _samples.Clear();
            _samples.Enqueue(_headPos);
            _headInContact = false;
            _lastSampleFrame = -1;
            _tailTravelRemaining = 0f;

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
            
            _headPos = Vector2.zero;
            _samples.Clear();
            _headInContact = false;
            _lastSampleFrame = -1;
            _tailTravelRemaining = 0f;
            
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            UpdateHeadAndSamples(deltaTime);
            UpdateBeamVisual();

            // no longer apply tick damage here; tick damage is handled during sampling
            // ApplyTickDamageIfNeeded();

            if (_isFlyingAway && _currentLength <= 0.1f)
            {
                ReturnToPool();
            }
        }

        private void UpdateHeadAndSamples(float deltaTime)
        {
            if (_weaponData == null || _field == null) return;

            float speed = LaserData != null ? LaserData.BeamSpeed : 20f;

            if (_direction.sqrMagnitude > 0.0001f) _direction.Normalize();
            else _direction = Vector2.right;

            // head always advances by speed
            Vector2 prevHead = _headPos;
            _headPos += _direction * speed * deltaTime;

            // wall contact test at current head angle; if beyond wall clamp to wall
            Vector2 center = (_field as Component) != null ? ((Component)_field).transform.position : Vector2.zero;
            Vector2 dirFromCenter = _headPos - center;
            _headInContact = false;

            if (dirFromCenter.sqrMagnitude > 0.0001f)
            {
                float angleDeg = Mathf.Atan2(dirFromCenter.y, dirFromCenter.x) * Mathf.Rad2Deg;
                if (angleDeg < 0f) angleDeg += 360f;
                int sectorIndex = _field.AngleToSectorIndex(angleDeg);
                float sectorRadius = _field.GetSectorRadius(sectorIndex);

                float dist = dirFromCenter.magnitude;
                if (dist >= sectorRadius - 0.02f)
                {
                    _headInContact = true;
                    _headPos = center + dirFromCenter.normalized * sectorRadius;
                }
            }

            // Always sample once per frame
            if (_lastSampleFrame != Time.frameCount)
            {
                _lastSampleFrame = Time.frameCount;
                _samples.Enqueue(_headPos);

                // only keep samples up to max length behind head
                float maxLen = LaserData != null ? LaserData.MaxLength : 0f;
                while (_samples.Count > 2)
                {
                    float tailToHead = (_headPos - _samples.Peek()).magnitude;
                    if (tailToHead <= maxLen + 0.001f) break;
                    _samples.Dequeue();
                }
            }

            // tail advances by speed regardless of head being clamped
            _tailTravelRemaining += speed * deltaTime;

            while (_samples.Count > 1 && _tailTravelRemaining > 0f)
            {
                Vector2 tail = _samples.Peek();
                Vector2 next = GetSecondSample();
                float seg = (next - tail).magnitude;
                if (seg <= 0.0001f)
                {
                    _samples.Dequeue();
                    continue;
                }

                if (_tailTravelRemaining >= seg)
                {
                    _tailTravelRemaining -= seg;
                    _samples.Dequeue();
                }
                else
                {
                    // interpolate within segment
                    float t = _tailTravelRemaining / seg;
                    Vector2 newTail = Vector2.Lerp(tail, next, t);
                    _samples.Dequeue();
                    EnqueueFront(newTail);
                    _tailTravelRemaining = 0f;
                    break;
                }
            }

            if (_samples.Count > 0)
            {
                _origin = _samples.Peek();
            }

            // Damage timing uses TickRate
            float tickRate = Mathf.Max(0.0001f, _weaponData.TickRate);
            float interval = 1f / tickRate;

            if (Time.time >= _nextTickTime)
            {
                _nextTickTime = Time.time + interval;

                if (_headInContact && !_isFlyingAway)
                {
                    ApplyMultiSectorDamage(_headPos);
                    _tickCount++;
                }
            }

            _currentLength = (_headPos - _origin).magnitude;

            if (_isFlyingAway && _currentLength <= 0.1f)
            {
                ReturnToPool();
            }
        }

        private Vector2 GetSecondSample()
        {
            // helper: peek second without allocations
            Vector2 first = default;
            bool gotFirst = false;
            foreach (var p in _samples)
            {
                if (!gotFirst)
                {
                    first = p;
                    gotFirst = true;
                    continue;
                }
                return p;
            }
            return first;
        }

        // Add extension helper inside class
        private void EnqueueFront(Vector2 v)
        {
            // Rebuild small queue: used rarely when interpolating tail; keep minimal allocations.
            var tmp = new List<Vector2>(_samples.Count + 1);
            tmp.Add(v);
            tmp.AddRange(_samples);
            _samples.Clear();
            for (int i = 0; i < tmp.Count; i++)
                _samples.Enqueue(tmp[i]);
        }

        private void UpdateBeamVisual()
        {
            if (lineRenderer == null) return;

            lineRenderer.SetPosition(0, _origin);
            lineRenderer.SetPosition(1, _headPos);
        }

        /// <summary>
        /// 홀드 상태에서 머즐 이동/회전에 맞춰 기원/방향 갱신
        /// </summary>
        public void UpdateOriginDirection(Vector2 origin, Vector2 direction)
        {
            // origin은 tail 기준 입력으로만 사용 (샘플 큐가 비어있을 때 초기값)
            _origin = origin;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : _direction;

            if (_samples.Count == 0)
            {
                _headPos = origin;
                _samples.Enqueue(_headPos);
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
