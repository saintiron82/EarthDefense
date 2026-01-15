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
    /// - ⭐ 세그먼트 기반: 포구 회전 시 각 세그먼트가 독립적인 방향으로 전진
    /// </summary>
    public class PolarLaserProjectile : PolarProjectileBase
    {
        /// <summary>
        /// 레이저 파티클: 매우 짧은 간격으로 연속 발사되는 "점"
        /// - 단발 총알을 매우 빠르게 연속으로 쏘는 것과 동일
        /// - 각 점은 발사된 위치에서 발사된 방향으로 독립적으로 전진
        /// - 점들을 이으면 곡선 레이저 형성
        /// </summary>
        private class LaserParticle
        {
            public Vector2 EmissionPoint;   // 발사된 위치 (고정)
            public Vector2 Direction;       // 발사 방향 (고정)
            public float Age;               // 발사 후 경과 시간
            public bool Blocked;            // 벽에 막혔는지
            public float MaxAge;            // 최대 나이 (벽 충돌 시 설정)

            /// <summary>현재 위치</summary>
            public Vector2 GetPosition(float speed)
            {
                float currentAge = Blocked ? MaxAge : Age;
                return EmissionPoint + Direction * speed * currentAge;
            }

            public LaserParticle(Vector2 emissionPoint, Vector2 direction)
            {
                EmissionPoint = emissionPoint;
                Direction = direction.normalized;
                Age = 0f;
                Blocked = false;
                MaxAge = 0f;
            }
        }
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

        private bool _isFlyingAway; // 소멸 과정 (Tail 전진 중)
        private float _nextTickTime;
        private Vector2 _direction; // 현재 포구 방향
        private int _tickCount;

        // ⭐ 다중 타격 지원
        private readonly HashSet<int> _hitSectorsThisTick = new HashSet<int>();
        private readonly Dictionary<int, float> _lastHitTimeBySector = new Dictionary<int, float>();
        private const float RehitCooldown = 0.05f; // 동일 섹터 재타격 쿨다운

        // ⭐ 파티클 기반 구조 (매우 짧은 간격으로 연속 발사되는 점들)
        private readonly List<LaserParticle> _particles = new List<LaserParticle>();
        private Vector2 _muzzlePos; // 현재 포구 위치
        private float _globalTailAge; // FlyAway 시 Tail의 나이 (모든 파티클 공통)
        private float _timeSinceLastEmission; // 마지막 파티클 발사 후 경과 시간

        [Header("Particle Emission Settings")]
        [SerializeField, Tooltip("파티클 발사 간격 (초) - 작을수록 부드러운 곡선")]
        [Range(0.001f, 0.1f)]
        private float emissionInterval = 0.016f; // ~60 particles/sec

        [SerializeField, Tooltip("최대 파티클 수 (성능 제한)")]
        private int maxParticles = 500;

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

            _muzzlePos = (_field as Component) != null ? ((Component)_field).transform.position : Vector2.zero;
            _direction = Vector2.right;

            InitializeBeam();
        }

        /// <summary>
        /// 특정 방향으로 발사 (오버로드)
        /// </summary>
        public void Launch(IPolarField field, PolarWeaponData weaponData, Vector2 origin, Vector2 direction)
        {
            Launch(field, weaponData);
            _muzzlePos = origin;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        }

        private void InitializeBeam()
        {
            _isFlyingAway = false;
            float tickInterval = _weaponData != null ? 1f / Mathf.Max(0.0001f, _weaponData.TickRate) : 0.1f;
            _nextTickTime = Time.time + tickInterval;
            _tickCount = 0;
            _hitSectorsThisTick.Clear();
            _lastHitTimeBySector.Clear();

            // 파티클 초기화
            _particles.Clear();
            _globalTailAge = 0f;
            _timeSinceLastEmission = 0f;

            // 첫 파티클 즉시 생성
            _particles.Add(new LaserParticle(_muzzlePos, _direction));

            if (logTickDamage)
            {
                Debug.Log($"[PolarLaserProjectile] ========== INITIALIZE BEAM (PARTICLE STREAM) ==========");
                Debug.Log($"  Time: {Time.time:F4}s, Frame: {Time.frameCount}");
                Debug.Log($"  Muzzle: {_muzzlePos}, Direction: {_direction}");
                Debug.Log($"  BeamSpeed: {(LaserData != null ? LaserData.BeamSpeed : 20f):F2}");
                Debug.Log($"  Emission Interval: {emissionInterval:F4}s ({(1f / emissionInterval):F0} particles/sec)");
                Debug.Log($"  TickRate: {_weaponData?.TickRate:F2}");
                Debug.Log($"=======================================================================\n");
            }

            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.startColor = LaserData.BeamColor;
                lineRenderer.endColor = LaserData.BeamColor;
                lineRenderer.startWidth = LaserData.BeamWidth;
                lineRenderer.endWidth = LaserData.BeamWidth;
            }
        }

        public void BeginFlyAway()
        {
            _isFlyingAway = true;

            if (logTickDamage)
            {
                Debug.Log($"[PolarLaserProjectile] ========== BEGIN FLY AWAY (PARTICLE STREAM) ==========");
                Debug.Log($"  Entering disappearance phase (tail will advance)");
                Debug.Log($"  Time: {Time.time:F4}s, Frame: {Time.frameCount}");
                Debug.Log($"  Current: Particles={_particles.Count}, TotalLength={GetTotalLength():F2}");
                Debug.Log($"===================================================================\n");
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
            _direction = Vector2.zero;
            _muzzlePos = Vector2.zero;
            _hitSectorsThisTick.Clear();
            _lastHitTimeBySector.Clear();

            // 파티클 정리
            _particles.Clear();
            _globalTailAge = 0f;
            _timeSinceLastEmission = 0f;

            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            UpdateParticles(deltaTime);
            UpdateBeamVisual();

            // 전체 길이 확인 및 소멸
            float totalLength = GetTotalLength();
            if (_isFlyingAway && totalLength <= 0.1f)
            {
                ReturnToPool();
            }
        }

        /// <summary>
        /// 파티클 기반 빔 업데이트 (매우 빠르게 연속 발사되는 점들)
        /// </summary>
        private void UpdateParticles(float deltaTime)
        {
            if (_weaponData == null || _field == null)
            {
                if (logTickDamage)
                {
                    Debug.LogError($"[UpdateParticles] Early return! weaponData={(_weaponData != null)}, field={(_field != null)}");
                }
                return;
            }

            float speed = LaserData != null ? LaserData.BeamSpeed : 20f;
            Vector2 center = (_field as Component) != null ? ((Component)_field).transform.position : Vector2.zero;

            // 1. FlyAway가 아닐 때: 새 파티클 지속 발사
            if (!_isFlyingAway)
            {
                _timeSinceLastEmission += deltaTime;

                // emission interval마다 새 파티클 생성 (방향 변경과 무관)
                while (_timeSinceLastEmission >= emissionInterval)
                {
                    if (_particles.Count < maxParticles)
                    {
                        _particles.Add(new LaserParticle(_muzzlePos, _direction));
                        _timeSinceLastEmission -= emissionInterval;

                        if (logTickDamage && _particles.Count % 100 == 0)
                        {
                            Debug.Log($"[Emission] Total particles: {_particles.Count}");
                        }
                    }
                    else
                    {
                        // 최대 파티클 수 도달: 가장 오래된 것 제거
                        _particles.RemoveAt(0);
                        _particles.Add(new LaserParticle(_muzzlePos, _direction));
                        _timeSinceLastEmission -= emissionInterval;

                        if (logTickDamage && Time.frameCount % 60 == 0)
                        {
                            Debug.LogWarning($"[Emission] Max particles reached: {maxParticles}");
                        }
                    }
                }
            }

            // 2. 모든 파티클의 Age 증가 (각자 독립적으로 전진)
            foreach (var particle in _particles)
            {
                if (!particle.Blocked)
                {
                    particle.Age += deltaTime;
                }
            }

            // 3. 마지막 파티클의 벽 충돌 체크
            if (_particles.Count > 0)
            {
                LaserParticle lastParticle = _particles[_particles.Count - 1];
                if (!lastParticle.Blocked)
                {
                    Vector2 particlePos = lastParticle.GetPosition(speed);
                    Vector2 dirFromCenter = particlePos - center;

                    if (dirFromCenter.sqrMagnitude > 0.0001f)
                    {
                        float angleDeg = Mathf.Atan2(dirFromCenter.y, dirFromCenter.x) * Mathf.Rad2Deg;
                        if (angleDeg < 0f) angleDeg += 360f;
                        int sectorIndex = _field.AngleToSectorIndex(angleDeg);
                        float sectorRadius = _field.GetSectorRadius(sectorIndex);

                        float dist = dirFromCenter.magnitude;
                        if (dist >= sectorRadius - 0.02f)
                        {
                            // 벽에 닿음: MaxAge 설정
                            Vector2 clampedPos = center + dirFromCenter.normalized * sectorRadius;
                            float clampedDist = (clampedPos - lastParticle.EmissionPoint).magnitude;
                            lastParticle.MaxAge = clampedDist / speed;
                            lastParticle.Blocked = true;

                            if (logTickDamage && Time.frameCount % 60 == 0)
                            {
                                Debug.Log($"[Particle] Last particle blocked! MaxAge={lastParticle.MaxAge:F2}");
                            }
                        }
                    }
                }
            }

            // 4. FlyAway 시 globalTailAge 증가
            if (_isFlyingAway)
            {
                _globalTailAge += deltaTime;

                // 완전히 소진된 파티클 제거
                while (_particles.Count > 0)
                {
                    LaserParticle firstParticle = _particles[0];
                    float headAge = firstParticle.Blocked ? firstParticle.MaxAge : firstParticle.Age;

                    if (_globalTailAge >= headAge)
                    {
                        // 이 파티클 완전히 소진
                        _particles.RemoveAt(0);

                        if (logTickDamage && _particles.Count % 50 == 0)
                        {
                            Debug.Log($"[FlyAway] Particle consumed. Remaining: {_particles.Count}");
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // 5. 데미지 틱 적용
            ApplyTickDamage();
        }

        /// <summary>
        /// 전체 빔 길이 계산 (첫 파티클 Tail ~ 마지막 파티클 Head)
        /// </summary>
        private float GetTotalLength()
        {
            if (_particles.Count == 0) return 0f;

            float speed = LaserData != null ? LaserData.BeamSpeed : 20f;

            // Tail 위치: 첫 파티클의 위치 (globalTailAge 고려)
            Vector2 tailPos = _particles[0].EmissionPoint + _particles[0].Direction * speed * _globalTailAge;

            // Head 위치: 마지막 파티클의 위치
            LaserParticle last = _particles[_particles.Count - 1];
            Vector2 headPos = last.GetPosition(speed);

            return Vector2.Distance(tailPos, headPos);
        }

        /// <summary>
        /// Head 위치 계산 (마지막 파티클의 위치)
        /// </summary>
        private Vector2 GetHeadPos()
        {
            if (_particles.Count == 0) return _muzzlePos;
            float speed = LaserData != null ? LaserData.BeamSpeed : 20f;
            return _particles[_particles.Count - 1].GetPosition(speed);
        }

        /// <summary>
        /// Tail 위치 계산 (첫 파티클의 Tail, FlyAway 시 globalTailAge 고려)
        /// </summary>
        private Vector2 GetTailPos()
        {
            if (_particles.Count == 0) return _muzzlePos;
            float speed = LaserData != null ? LaserData.BeamSpeed : 20f;
            return _particles[0].EmissionPoint + _particles[0].Direction * speed * _globalTailAge;
        }

        /// <summary>
        /// 틱 데미지 적용
        /// </summary>
        private void ApplyTickDamage()
        {
            float tickRate = Mathf.Max(0.0001f, _weaponData.TickRate);
            float interval = 1f / tickRate;

            if (Time.time >= _nextTickTime)
            {
                _nextTickTime = Time.time + interval;

                // 마지막 파티클이 벽에 닿았고 FlyAway가 아닐 때만 데미지
                if (_particles.Count > 0 && _particles[_particles.Count - 1].Blocked && !_isFlyingAway)
                {
                    Vector2 headPos = GetHeadPos();
                    ApplyMultiSectorDamage(headPos);
                    _tickCount++;
                }
            }
        }


        /// <summary>
        /// 파티클 기반 레이저 렌더링
        ///
        /// 렌더링 모델:
        /// - 각 파티클은 독립적으로 발사된 "점" (단발 총알을 매우 빠르게 연사)
        /// - 파티클들의 위치를 LineRenderer로 연결하면 곡선 형성
        /// - 포구가 회전하면 새로 발사되는 파티클들이 새 방향으로 가므로 자연스러운 곡선
        /// </summary>
        private void UpdateLineRendererWithAdaptiveSampling()
        {
            if (lineRenderer == null || _particles.Count == 0)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.positionCount = 0;
                }
                return;
            }

            float speed = LaserData != null ? LaserData.BeamSpeed : 20f;

            // 모든 파티클의 현재 위치를 렌더 포인트로 사용
            List<Vector2> renderPoints = new List<Vector2>(_particles.Count);

            foreach (var particle in _particles)
            {
                Vector2 particlePos = particle.GetPosition(speed);

                // FlyAway 중일 때: Tail보다 뒤에 있는 파티클은 스킵
                if (_isFlyingAway)
                {
                    float particleAge = particle.Blocked ? particle.MaxAge : particle.Age;
                    if (particleAge < _globalTailAge)
                    {
                        continue; // 이미 소진된 부분
                    }
                }

                renderPoints.Add(particlePos);
            }

            // 디버그: 렌더 정보 확인
            if (logTickDamage && Time.frameCount % 60 == 0)
            {
                Vector2 tailPos = GetTailPos();
                Vector2 headPos = GetHeadPos();
                float totalLength = GetTotalLength();

                Debug.Log($"[Render] Particles: {_particles.Count}, RenderPoints: {renderPoints.Count}, " +
                          $"Tail: {tailPos}, Head: {headPos}, TotalLength: {totalLength:F2}");
            }

            // LineRenderer 적용
            lineRenderer.positionCount = renderPoints.Count;
            for (int i = 0; i < renderPoints.Count; i++)
            {
                lineRenderer.SetPosition(i, renderPoints[i]);
            }
        }

        private void UpdateBeamVisual()
        {
            if (lineRenderer == null) return;

            UpdateLineRendererWithAdaptiveSampling();
        }

        /// <summary>
        /// 홀드 상태에서 머즐 이동/회전에 맞춰 포구 위치/방향 갱신
        /// 파티클은 emission interval마다 자동 생성되므로 방향만 업데이트
        /// </summary>
        public void UpdateOriginDirection(Vector2 origin, Vector2 direction)
        {
            // FlyAway 중에는 업데이트하지 않음
            if (_isFlyingAway) return;

            _muzzlePos = origin;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;

            // 새 파티클은 UpdateParticles()에서 emissionInterval마다 자동 생성됨
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
            // 세그먼트 레이저: Head 위치 기반 각도 사용
            Vector2 headPos = GetHeadPos();
            Vector2 headDirFromCenter = (headPos - center).normalized;
            float beamAngle = Mathf.Atan2(headDirFromCenter.y, headDirFromCenter.x) * Mathf.Rad2Deg;
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
        /// 파티클 기반: 파티클들의 경로를 연결하여 곡선 표시
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDamageGizmos || !_hasLastHitData || _field == null) return;

            Vector2 center = (_field as Component) != null
                ? ((Component)_field).transform.position
                : Vector2.zero;

            // 1. 파티클 경로 라인 (녹색)
            if (_particles.Count > 1)
            {
                float speed = LaserData != null ? LaserData.BeamSpeed : 20f;
                Gizmos.color = Color.green;

                for (int i = 0; i < _particles.Count - 1; i++)
                {
                    Vector2 start = _particles[i].GetPosition(speed);
                    Vector2 end = _particles[i + 1].GetPosition(speed);
                    Gizmos.DrawLine(start, end);
                }

                // 2. 빔 시작점과 끝점 표시
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(GetTailPos(), 0.1f);
                Gizmos.DrawWireSphere(GetHeadPos(), 0.15f);

                // 파티클 위치 표시 (흰색 작은 점, 10개마다)
                Gizmos.color = Color.white;
                for (int i = 0; i < _particles.Count; i += 10)
                {
                    Vector2 pos = _particles[i].GetPosition(speed);
                    Gizmos.DrawWireSphere(pos, 0.03f);
                }
            }

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
            Vector3 legendPos = GetTailPos() + Vector2.up * 2f;
            UnityEditor.Handles.Label(legendPos,
                $"Center Sector: #{_lastCenterSectorIndex}\n" +
                $"Damage Radius: {_lastDamageRadius} sectors\n" +
                $"Sectors Hit: {_lastHitSectors.Count}\n" +
                $"Particles: {_particles.Count}\n" +
                $"Total Length: {GetTotalLength():F2}");
#endif
        }
    }
}
