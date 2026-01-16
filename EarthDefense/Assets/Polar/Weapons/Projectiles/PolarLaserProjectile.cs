using UnityEngine;
using System.Collections.Generic;

namespace Polar.Weapons.Projectiles
{
    public class PolarLaserProjectile : PolarProjectileBase
    {
        private class LaserParticle
        {
            public Vector2 EmissionPoint;
            public Vector2 Direction;
            public float Age;
            public bool Blocked;
            public float MaxAge;

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

        private enum HitscanState
        {
            Warmup = 0,
            Active = 1,
            FadeOut = 2
        }

        private enum BeamRenderMode
        {
            ParticleStream = 0,
            HitscanToWall = 1,
            FixedSegment = 2
        }

        [Header("Visual")]
        [SerializeField] private LineRenderer lineRenderer;
        [Header("Debug")]
        [SerializeField] private bool logTickDamage;
        [SerializeField] private bool showDamageGizmos = true;
        [SerializeField] private Color gizmoHitSectorColor = Color.yellow;

        private PolarLaserWeaponData LaserData => _weaponData as PolarLaserWeaponData;

        private List<int> _lastHitSectors = new List<int>();
        private int _lastCenterSectorIndex;
        private int _lastDamageRadius;
        private bool _hasLastHitData;

        private bool _isFlyingAway;
        private float _nextTickTime;
        private Vector2 _direction;
        private int _tickCount;

        private readonly HashSet<int> _hitSectorsThisTick = new HashSet<int>();
        private readonly Dictionary<int, float> _lastHitTimeBySector = new Dictionary<int, float>();
        private const float RehitCooldown = 0.05f;

        private readonly List<LaserParticle> _particles = new List<LaserParticle>();
        private Vector2 _muzzlePos;
        private float _globalTailAge;
        private float _timeSinceLastEmission;

        [Header("Particle Emission Settings")]
        [SerializeField, Tooltip("파티클 발사 간격 (초) - 작을수록 부드러운 곡선")]
        [Range(0.001f, 0.1f)]
        private float emissionInterval = 0.016f;

        [SerializeField, Tooltip("최대 파티클 수 (성능 제한)")]
        private int maxParticles = 500;

        private BeamRenderMode _renderMode;

        // Hitscan/segment 상태
        private HitscanState _hitscanState;
        private float _hitscanStateAge;
        private Vector2 _hitscanEndPoint;
        private float _hitscanMaxAlpha;
        private float _hitscanMinAlpha;
        private float _hitscanMaxWidth;
        private float _hitscanMinWidth;

        // Segment origin (child-beam에서도 사용)
        private Vector2 _segmentStartPoint;

        public Vector2 SegmentStartPoint => _segmentStartPoint;
        public Vector2 SegmentEndPoint => _hitscanEndPoint;

        private float _damageMultiplier = 1f;

        /// <summary>
        /// 반사/특수 처리용 데미지 배율. 1.0 = 원본.
        /// </summary>
        public void SetDamageMultiplier(float multiplier)
        {
            _damageMultiplier = Mathf.Max(0f, multiplier);
        }

        private void Awake()
        {
            // Prefab에서 누락된 레퍼런스 보정
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = GetComponentInChildren<LineRenderer>();
                }
            }
        }

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

        public void Launch(IPolarField field, PolarWeaponData weaponData, Vector2 origin, Vector2 direction)
        {
            Launch(field, weaponData);
            _muzzlePos = origin;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        }

        /// <summary>
        /// ChildBeam(반사)용: 한 구간(시작-끝)을 고정 렌더링하는 빔 세그먼트
        /// </summary>
        public void LaunchSegment(IPolarField field, PolarWeaponData weaponData, Vector2 startPoint, Vector2 endPoint)
        {
            if (weaponData is not PolarLaserWeaponData)
            {
                Debug.LogError("[PolarLaserProjectile] Requires PolarLaserWeaponData!");
                return;
            }

            _field = field;
            _weaponData = weaponData;
            _isActive = true;

            _renderMode = BeamRenderMode.FixedSegment;

            _segmentStartPoint = startPoint;
            _hitscanEndPoint = endPoint;
            _muzzlePos = startPoint;
            _direction = (endPoint - startPoint).sqrMagnitude > 0f ? (endPoint - startPoint).normalized : Vector2.right;

            InitializeBeam();
        }

        /// <summary>
        /// ChildBeam(반사)용: 세그먼트 시작/끝을 갱신.
        /// - Warmup/Active/FadeOut 상태는 유지된다.
        /// - FadeOut 중에는 갱신하지 않는다.
        /// </summary>
        public void UpdateSegment(Vector2 startPoint, Vector2 endPoint)
        {
            if (!_isActive)
            {
                return;
            }

            if (_renderMode != BeamRenderMode.FixedSegment)
            {
                return;
            }

            if (_hitscanState == HitscanState.FadeOut)
            {
                return;
            }

            _segmentStartPoint = startPoint;
            _hitscanEndPoint = endPoint;
            _muzzlePos = startPoint;
            _direction = (endPoint - startPoint).sqrMagnitude > 0f ? (endPoint - startPoint).normalized : Vector2.right;
        }

        private void InitializeBeam()
        {
            _isFlyingAway = false;
            float tickInterval = _weaponData != null ? 1f / Mathf.Max(0.0001f, _weaponData.TickRate) : 0.1f;
            _nextTickTime = Time.time + tickInterval;
            _tickCount = 0;
            _hitSectorsThisTick.Clear();
            _lastHitTimeBySector.Clear();

            _particles.Clear();
            _globalTailAge = 0f;
            _timeSinceLastEmission = 0f;

            _hitscanState = HitscanState.Warmup;
            _hitscanStateAge = 0f;

            // FixedSegment는 LaunchSegment에서 세팅한 start/end를 유지해야 한다.
            if (_renderMode != BeamRenderMode.FixedSegment)
            {
                _segmentStartPoint = _muzzlePos;
                _hitscanEndPoint = _muzzlePos;
            }

            float beamWidth = LaserData != null ? LaserData.BeamWidth : 0.1f;
            _hitscanMaxWidth = beamWidth;
            _hitscanMinWidth = beamWidth * (LaserData != null ? LaserData.HitscanStartWidthMultiplier : 0.7f);

            Color c = LaserData != null ? LaserData.BeamColor : Color.cyan;
            _hitscanMaxAlpha = c.a;
            _hitscanMinAlpha = Mathf.Clamp01(LaserData != null ? LaserData.HitscanStartAlpha : 0.2f);

            // 기본은 ParticleStream. 단, FixedSegment는 이미 설정됨.
            if (_renderMode != BeamRenderMode.FixedSegment)
            {
                _renderMode = BeamRenderMode.ParticleStream;
                if (LaserData != null && LaserData.FireMode == PolarLaserFireMode.HitscanWarmup)
                {
                    _renderMode = BeamRenderMode.HitscanToWall;
                }
            }

            _particles.Add(new LaserParticle(_muzzlePos, _direction));

            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.startColor = LaserData.BeamColor;
                lineRenderer.endColor = LaserData.BeamColor;
                lineRenderer.startWidth = LaserData.BeamWidth;
                lineRenderer.endWidth = LaserData.BeamWidth;
            }
            else
            {
                if (_renderMode == BeamRenderMode.FixedSegment)
                {
                    Debug.LogWarning($"[PolarLaserProjectile] LineRenderer is missing on {gameObject.name}. Reflected segment cannot render.");
                }
            }
        }

        public void BeginFlyAway()
        {
            if (_renderMode == BeamRenderMode.HitscanToWall || _renderMode == BeamRenderMode.FixedSegment)
            {
                BeginHitscanFadeOut();
                return;
            }

            _isFlyingAway = true;
        }

        private void BeginHitscanFadeOut()
        {
            if (_hitscanState == HitscanState.FadeOut)
            {
                return;
            }

            _hitscanState = HitscanState.FadeOut;
            _hitscanStateAge = 0f;
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

            _particles.Clear();
            _globalTailAge = 0f;
            _timeSinceLastEmission = 0f;

            _renderMode = BeamRenderMode.ParticleStream;
            _hitscanState = HitscanState.Warmup;
            _hitscanStateAge = 0f;
            _hitscanEndPoint = Vector2.zero;
            _segmentStartPoint = Vector2.zero;

            _damageMultiplier = 1f;

            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (_renderMode == BeamRenderMode.HitscanToWall || _renderMode == BeamRenderMode.FixedSegment)
            {
                UpdateHitscanLike(deltaTime);
                return;
            }

            UpdateParticles(deltaTime);
            UpdateBeamVisual();

            float totalLength = GetTotalLength();
            if (_isFlyingAway && totalLength <= 0.1f)
            {
                ReturnToPool();
            }
        }

        private void UpdateHitscanLike(float deltaTime)
        {
            if (_weaponData == null || _field == null)
            {
                return;
            }

            _hitscanStateAge += deltaTime;

            if (_renderMode == BeamRenderMode.HitscanToWall)
            {
                _segmentStartPoint = _muzzlePos;
                _hitscanEndPoint = CalculateHitscanEndPoint(_muzzlePos, _direction);
            }
            else
            {
                // FixedSegment: segment start/end은 LaunchSegment에서 고정
            }

            UpdateHitscanVisual();

            if (_hitscanState == HitscanState.Warmup)
            {
                float warmup = LaserData != null ? LaserData.HitscanWarmupDuration : 0f;
                if (_hitscanStateAge >= warmup)
                {
                    _hitscanState = HitscanState.Active;
                    _hitscanStateAge = 0f;

                    float tickInterval = _weaponData != null ? 1f / Mathf.Max(0.0001f, _weaponData.TickRate) : 0.1f;
                    _nextTickTime = Time.time + tickInterval;
                }
            }

            if (_hitscanState == HitscanState.Active)
            {
                ApplyHitscanTickDamage();
            }
            else if (_hitscanState == HitscanState.FadeOut)
            {
                float fade = LaserData != null ? LaserData.HitscanFadeOutDuration : 0f;
                if (_hitscanStateAge >= fade)
                {
                    ReturnToPool();
                }
            }
        }

        private Vector2 CalculateHitscanEndPoint(Vector2 origin, Vector2 direction)
        {
            if (_field == null)
            {
                return origin;
            }

            Vector2 center = (_field as Component) != null
                ? ((Component)_field).transform.position
                : Vector2.zero;

            Vector2 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;

            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angleDeg < 0f) angleDeg += 360f;

            int sectorIndex = _field.AngleToSectorIndex(angleDeg);
            float sectorRadius = _field.GetSectorRadius(sectorIndex);

            return center + dir * sectorRadius;
        }

        private void UpdateHitscanVisual()
        {
            if (lineRenderer == null)
            {
                if (_renderMode == BeamRenderMode.FixedSegment)
                {
                    // 프레임마다 찍히지 않도록(스팸 방지): Warmup 시작 프레임에만 경고
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.LogWarning($"[PolarLaserProjectile] LineRenderer is null (FixedSegment). Check prefab reference for {gameObject.name}.");
                    }
                }
                return;
            }

            float warmup = LaserData != null ? LaserData.HitscanWarmupDuration : 0f;
            float fade = LaserData != null ? LaserData.HitscanFadeOutDuration : 0f;

            float alpha;
            float width;

            if (_hitscanState == HitscanState.Warmup)
            {
                float t = warmup <= 0f ? 1f : Mathf.Clamp01(_hitscanStateAge / warmup);
                alpha = Mathf.Lerp(_hitscanMinAlpha, _hitscanMaxAlpha, t);
                width = Mathf.Lerp(_hitscanMinWidth, _hitscanMaxWidth, t);
            }
            else if (_hitscanState == HitscanState.FadeOut)
            {
                float t = fade <= 0f ? 1f : Mathf.Clamp01(_hitscanStateAge / fade);
                alpha = Mathf.Lerp(_hitscanMaxAlpha, 0f, t);
                width = Mathf.Lerp(_hitscanMaxWidth, _hitscanMinWidth, t);
            }
            else
            {
                alpha = _hitscanMaxAlpha;
                width = _hitscanMaxWidth;
            }

            Color baseColor = LaserData != null ? LaserData.BeamColor : Color.cyan;
            Color c = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, _segmentStartPoint);
            lineRenderer.SetPosition(1, _hitscanEndPoint);
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }

        private void ApplyHitscanTickDamage()
        {
            float tickRate = Mathf.Max(0.0001f, _weaponData.TickRate);
            float interval = 1f / tickRate;

            if (Time.time < _nextTickTime)
            {
                return;
            }

            _nextTickTime = Time.time + interval;

            ApplyMultiSectorDamage(_hitscanEndPoint);
            _tickCount++;
        }

        private void UpdateParticles(float deltaTime)
        {
            if (_weaponData == null || _field == null)
            {
                return;
            }

            float speed = LaserData != null ? LaserData.BeamSpeed : 20f;
            Vector2 center = (_field as Component) != null ? ((Component)_field).transform.position : Vector2.zero;

            if (!_isFlyingAway)
            {
                _timeSinceLastEmission += deltaTime;

                while (_timeSinceLastEmission >= emissionInterval)
                {
                    if (_particles.Count < maxParticles)
                    {
                        _particles.Add(new LaserParticle(_muzzlePos, _direction));
                        _timeSinceLastEmission -= emissionInterval;
                    }
                    else
                    {
                        _particles.RemoveAt(0);
                        _particles.Add(new LaserParticle(_muzzlePos, _direction));
                        _timeSinceLastEmission -= emissionInterval;
                    }
                }
            }

            foreach (var particle in _particles)
            {
                if (!particle.Blocked)
                {
                    particle.Age += deltaTime;
                }
            }

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
                            Vector2 clampedPos = center + dirFromCenter.normalized * sectorRadius;
                            float clampedDist = (clampedPos - lastParticle.EmissionPoint).magnitude;
                            lastParticle.MaxAge = clampedDist / speed;
                            lastParticle.Blocked = true;
                        }
                    }
                }
            }

            if (_isFlyingAway)
            {
                _globalTailAge += deltaTime;

                while (_particles.Count > 0)
                {
                    LaserParticle firstParticle = _particles[0];
                    float headAge = firstParticle.Blocked ? firstParticle.MaxAge : firstParticle.Age;

                    if (_globalTailAge >= headAge)
                    {
                        _particles.RemoveAt(0);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            ApplyTickDamage();
        }

        private float GetTotalLength()
        {
            if (_particles.Count == 0) return 0f;

            float speed = LaserData != null ? LaserData.BeamSpeed : 20f;

            Vector2 tailPos = _particles[0].EmissionPoint + _particles[0].Direction * speed * _globalTailAge;

            LaserParticle last = _particles[_particles.Count - 1];
            Vector2 headPos = last.GetPosition(speed);

            return Vector2.Distance(tailPos, headPos);
        }

        private Vector2 GetHeadPos()
        {
            if (_particles.Count == 0) return _muzzlePos;
            float speed = LaserData != null ? LaserData.BeamSpeed : 20f;
            return _particles[_particles.Count - 1].GetPosition(speed);
        }

        private void ApplyTickDamage()
        {
            float tickRate = Mathf.Max(0.0001f, _weaponData.TickRate);
            float interval = 1f / tickRate;

            if (Time.time >= _nextTickTime)
            {
                _nextTickTime = Time.time + interval;

                if (_particles.Count > 0 && _particles[_particles.Count - 1].Blocked && !_isFlyingAway)
                {
                    Vector2 headPos = GetHeadPos();
                    ApplyMultiSectorDamage(headPos);
                    _tickCount++;
                }
            }
        }

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

            List<Vector2> renderPoints = new List<Vector2>(_particles.Count);

            foreach (var particle in _particles)
            {
                Vector2 particlePos = particle.GetPosition(speed);

                if (_isFlyingAway)
                {
                    float particleAge = particle.Blocked ? particle.MaxAge : particle.Age;
                    if (particleAge < _globalTailAge)
                    {
                        continue;
                    }
                }

                renderPoints.Add(particlePos);
            }

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

        public void UpdateOriginDirection(Vector2 origin, Vector2 direction)
        {
            if (_isFlyingAway) return;

            if (_renderMode == BeamRenderMode.FixedSegment)
            {
                return;
            }

            if (_renderMode == BeamRenderMode.HitscanToWall && _hitscanState == HitscanState.FadeOut)
            {
                return;
            }

            _muzzlePos = origin;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        }

        private void ApplyMultiSectorDamage(Vector2 hitPoint)
        {
            if (_field == null || _weaponData == null) return;

            Vector2 center = (_field as Component) != null
                ? ((Component)_field).transform.position
                : Vector2.zero;

            Vector2 headPos;
            if (_renderMode == BeamRenderMode.HitscanToWall || _renderMode == BeamRenderMode.FixedSegment)
            {
                headPos = _hitscanEndPoint;
            }
            else
            {
                headPos = GetHeadPos();
            }

            Vector2 headDirFromCenter = (headPos - center).normalized;
            float beamAngle = Mathf.Atan2(headDirFromCenter.y, headDirFromCenter.x) * Mathf.Rad2Deg;
            if (beamAngle < 0f) beamAngle += 360f;
            int centerSectorIndex = _field.AngleToSectorIndex(beamAngle);

            float beamRadius = LaserData.BeamWidth / 2f;
            float avgSectorRadius = _field.GetSectorRadius(centerSectorIndex);
            float beamArcAngle = (beamRadius / avgSectorRadius) * Mathf.Rad2Deg;
            float sectorAngleSize = 360f / _field.SectorCount;
            int damageRadius = Mathf.Max(0, Mathf.CeilToInt(beamArcAngle / sectorAngleSize));

            float damagePerTick = (_weaponData.Damage / Mathf.Max(0.0001f, _weaponData.TickRate)) * _damageMultiplier;

            _hitSectorsThisTick.Clear();

            if (showDamageGizmos)
            {
                _lastCenterSectorIndex = centerSectorIndex;
                _lastDamageRadius = damageRadius;
                _lastHitSectors.Clear();
                _hasLastHitData = true;
            }

            _field.SetLastWeaponKnockback(_weaponData.KnockbackPower);

            if (CanHitSector(centerSectorIndex))
            {
                ApplySectorDamage(centerSectorIndex, damagePerTick);
                _hitSectorsThisTick.Add(centerSectorIndex);
            }

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

        private void OnDrawGizmos()
        {
            if (!showDamageGizmos || !_hasLastHitData || _field == null) return;

            Vector2 center = (_field as Component) != null
                ? ((Component)_field).transform.position
                : Vector2.zero;

            if (_renderMode == BeamRenderMode.HitscanToWall || _renderMode == BeamRenderMode.FixedSegment)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(_segmentStartPoint, _hitscanEndPoint);

                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(_segmentStartPoint, 0.08f);
                Gizmos.DrawWireSphere(_hitscanEndPoint, 0.12f);
            }
            else
            {
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
                }
            }

            float centerAngle = _field.SectorIndexToAngle(_lastCenterSectorIndex);
            Vector2 centerDir = new Vector2(Mathf.Cos(centerAngle * Mathf.Deg2Rad), Mathf.Sin(centerAngle * Mathf.Deg2Rad));
            float centerRadius = _field.GetSectorRadius(_lastCenterSectorIndex);
            Vector2 centerPoint = center + centerDir * centerRadius;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(centerPoint, 0.15f);
            Gizmos.DrawLine(center, centerPoint);

            foreach (int sectorIndex in _lastHitSectors)
            {
                if (sectorIndex == _lastCenterSectorIndex) continue;

                float sectorAngle = _field.SectorIndexToAngle(sectorIndex);
                Vector2 sectorDir = new Vector2(Mathf.Cos(sectorAngle * Mathf.Deg2Rad), Mathf.Sin(sectorAngle * Mathf.Deg2Rad));
                float sectorRadius = _field.GetSectorRadius(sectorIndex);
                Vector2 sectorPoint = center + sectorDir * sectorRadius;

                Gizmos.color = gizmoHitSectorColor;
                Gizmos.DrawWireSphere(sectorPoint, 0.1f);
                Gizmos.DrawLine(center, sectorPoint);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(sectorPoint, $"#{sectorIndex}");
#endif
            }

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(center, 0.2f);
        }
    }
}
