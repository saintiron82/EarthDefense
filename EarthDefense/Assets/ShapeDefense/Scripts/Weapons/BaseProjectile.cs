using System.Collections.Generic;
using UnityEngine;
using Script.SystemCore.Pool;

namespace ShapeDefense.Scripts.Weapons
{
    /// <summary>
    /// 모든 발사체의 공통 기능을 제공하는 추상 기본 클래스
    /// 무기로부터 스펙(damage, speed, lifetime, maxHits)을 주입받아 동작
    /// </summary>
    public abstract class BaseProjectile : MonoBehaviour, IPoolable
    {
        // IPoolable - 풀에서 자동 주입
        public string PoolBundleId { get; set; }

        [Header("Projectile Type - 타입별 특성")]
        [Tooltip("발사체 타입 (Normal, Fire, Ice 등)")]
        [SerializeField] protected ProjectileType projectileType = ProjectileType.Normal;
        
        [Header("Hit Behavior - 타입별 히트 동작")]
        [Tooltip("같은 대상 재타격 쿨타임")]
        [SerializeField] protected float rehitCooldown = 0.05f;
        
        [Header("Hit Detection - 판정 설정")]
        [Tooltip("히트 판정 반경")]
        [SerializeField] protected float hitRadius = 0.07f;
        
        [Header("Visual - 비주얼")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected Color projectileColor = Color.white;
        
        [Header("Target Layers")]
        [SerializeField] protected LayerMask targetLayers = -1;
        
        // 런타임 데이터 - 무기로부터 주입받음 ⭐
        protected float _damage;      // 무기가 주입
        protected float _speed;       // 무기가 주입
        protected float _lifetime;    // 무기가 주입
        protected int _maxHits;       // 무기가 주입 ⭐
        
        protected float _spawnTime;
        protected GameObject _source;
        protected int _sourceTeamKey;
        protected Vector2 _direction;
        protected bool _isActive;

        // 히트 공통 상태 (콜라이더 없이 스윕 판정 공유)
        protected readonly Dictionary<long, float> _lastHitTimeByTargetId = new();
        protected readonly List<RingSectorDamageMask> _nearbyChunks = new();
        protected readonly List<Health> _nearbyHealths = new();

        // Properties
        public float Speed => _speed;              // 주입받은 값 사용
        public float Damage => _damage;            // 주입받은 값 사용
        public float Lifetime => _lifetime;        // 주입받은 값 사용
        public int MaxHits => _maxHits;            // 주입받은 값 사용 ⭐
        public bool IsActive => _isActive;
        public ProjectileType Type => projectileType;

        protected virtual void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            InitializeProjectile();
        }

        protected virtual void Update()
        {
            if (!_isActive) return;
            
            // 수명 체크 (무기가 주입한 lifetime 사용)
            if (Time.time - _spawnTime >= _lifetime)
            {
                OnLifetimeExpired();
            }
            
            UpdateProjectile();
        }
        
        /// <summary>
        /// 발사 - 무기로부터 스펙 주입받기
        /// </summary>
        /// <param name="direction">발사 방향</param>
        /// <param name="damage">무기가 주입하는 데미지</param>
        /// <param name="speed">무기가 주입하는 속도</param>
        /// <param name="lifetime">무기가 주입하는 수명</param>
        /// <param name="maxHits">무기가 주입하는 관통 횟수</param>
        /// <param name="source">발사 소스</param>
        /// <param name="sourceTeamKey">팀 키</param>
        public virtual void Fire(Vector2 direction, float damage, float speed, float lifetime, 
                                 int maxHits, GameObject source, int sourceTeamKey)
        {
            _direction = direction.normalized;
            _damage = damage;        // ⭐ 무기가 주입
            _speed = speed;          // ⭐ 무기가 주입
            _lifetime = lifetime;    // ⭐ 무기가 주입
            _maxHits = maxHits;      // ⭐ 무기가 주입
            _source = source;
            _sourceTeamKey = sourceTeamKey;
            _spawnTime = Time.time;
            _isActive = true;
            
            OnFired();
        }
        
        /// <summary>
        /// 발사 시 호출 - 각 탄약 타입별 초기화
        /// </summary>
        protected virtual void OnFired() { }

        /// <summary>
        /// 발사체 초기화 (서브클래스에서 오버라이드)
        /// </summary>
        protected virtual void InitializeProjectile() { }

        /// <summary>
        /// 매 프레임 발사체 업데이트 로직 (서브클래스에서 오버라이드)
        /// </summary>
        protected virtual void UpdateProjectile() { }

        /// <summary>
        /// 수명이 다했을 때 호출 (서브클래스에서 오버라이드)
        /// </summary>
        protected virtual void OnLifetimeExpired()
        {
            ReturnToPool();
        }

        /// <summary>
        /// 대상이 유효한 타격 대상인지 확인
        /// </summary>
        protected virtual bool IsValidTarget(Collider2D col)
        {
            if (!col.TryGetComponent<Health>(out var health)) return false;
            if (health.TeamKey == _sourceTeamKey) return false; // 같은 팀은 무시
            return ((1 << col.gameObject.layer) & targetLayers) != 0;
        }

        /// <summary>
        /// 대상에게 데미지 적용
        /// </summary>
        protected virtual void ApplyDamage(Health target, Vector2 hitPoint, Vector2 direction, float damageAmount)
        {
            var damageEvent = new DamageEvent(damageAmount, hitPoint, direction, _source);
            target.TakeDamage(damageEvent);
        }

        /// <summary>
        /// 오브젝트 풀로 반환하거나 파괴
        /// </summary>
        protected virtual void ReturnToPool()
        {
            _isActive = false;
            
            if (PoolService.Instance != null && !string.IsNullOrEmpty(PoolBundleId))
            {
                PoolService.Instance.Return(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }


        #region IPoolable Implementation

        public virtual void OnSpawnFromPool()
        {
            _spawnTime = Time.time;
            _isActive = true;
            ResetHitState();
        }

        public virtual void OnReturnToPool()
        {
            _source = null;
            _isActive = false;
            ResetHitState();
        }

        #endregion

        #region Hit Utilities

        protected readonly struct HitResult
        {
            public readonly Vector2 Point;
            public readonly RingSectorDamageMask Chunk;
            public readonly Health Health;
            public readonly int AngleIndex;
            public readonly int RadiusIndex;

            public HitResult(Vector2 point, RingSectorDamageMask chunk, Health health, int angleIndex = -1, int radiusIndex = -1)
            {
                Point = point;
                Chunk = chunk;
                Health = health;
                AngleIndex = angleIndex;
                RadiusIndex = radiusIndex;
            }
        }

        protected void ResetHitState()
        {
            _lastHitTimeByTargetId.Clear();
        }

        /// <summary>
        /// 진행 구간(from→to)을 샘플링하여 Chunk/Health를 스윕 판정한다.
        /// </summary>
        protected bool TrySweepHit(Vector2 from, Vector2 to, Vector2 direction, int sweepSteps, float sweepEpsilon, float radius, out HitResult hit)
        {
            hit = default;

            if (SectorManager.Instance == null)
            {
                return false;
            }

            var dirNorm = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            var angleDeg = Mathf.Atan2(dirNorm.y, dirNorm.x) * Mathf.Rad2Deg;

            if (!SectorManager.Instance.TryGetSector(angleDeg, out var sector) || sector == null)
            {
                return false;
            }

            _nearbyChunks.Clear();
            int chunkCount = sector.GetChunksInStackOrder(_nearbyChunks);

            float dist = Vector2.Distance(from, to);
            if (dist <= Mathf.Epsilon)
            {
                return false;
            }

            int dynamicSteps = Mathf.CeilToInt(dist / Mathf.Max(radius * 0.5f, 0.01f));
            int steps = Mathf.Max(1, Mathf.Max(sweepSteps, dynamicSteps));

            for (int c = 0; c < chunkCount; c++)
            {
                var mask = _nearbyChunks[c];
                if (mask == null) continue;

                for (int i = 1; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Vector2 p = Vector2.Lerp(from, to, t);
                    if (sweepEpsilon > 0f) p += dirNorm * sweepEpsilon;

                    if (mask.TryGetCellFromWorldPoint(p, out var aIdx, out var rIdx))
                    {
                        var cellIdx = mask.AngleCells * rIdx + aIdx;
                        if (!mask.IsCellDestroyed(cellIdx))
                        {
                            hit = new HitResult(p, mask, null, aIdx, rIdx);
                            return true;
                        }
                    }
                }
            }

            DamageableRegistry.PruneIfNeeded();
            _nearbyHealths.Clear();
            DamageableRegistry.QueryHealthsInLine(from, to, _nearbyHealths);

            float distHealth = Vector2.Distance(from, to);
            if (distHealth <= Mathf.Epsilon) return false;

            int healthSteps = Mathf.Max(1, sweepSteps);
            for (int i = 1; i <= healthSteps; i++)
            {
                float t = i / (float)healthSteps;
                Vector2 p = Vector2.Lerp(from, to, t);
                float proj = Vector2.Dot(p - from, dirNorm);
                if (proj < 0f || proj > distHealth) continue;

                for (int h = 0; h < _nearbyHealths.Count; h++)
                {
                    var target = _nearbyHealths[h];
                    if (target == null || target.IsDead) continue;
                    if (Vector2.Distance(p, target.transform.position) <= radius)
                    {
                        hit = new HitResult(p, null, target);
                        return true;
                    }
                }
            }

            return false;
        }

        protected bool CanHit(in HitResult hit)
        {
            long targetId;
            if (hit.Health != null)
            {
                targetId = hit.Health.UniqueId;
            }
            else if (hit.Chunk != null)
            {
                if (hit.AngleIndex < 0 || hit.RadiusIndex < 0) return false;
                targetId = hit.Chunk.GetCellUniqueId(hit.AngleIndex, hit.RadiusIndex);
            }
            else
            {
                return false;
            }

            if (_lastHitTimeByTargetId.TryGetValue(targetId, out var lastTime))
            {
                if (Time.time - lastTime < rehitCooldown) return false;
            }

            _lastHitTimeByTargetId[targetId] = Time.time;
            return true;
        }

        protected void ApplyHit(in HitResult hit, Vector2 direction, float damageAmount, HitEffect hitEffect, bool debugLogHits)
        {
            if (hit.Health != null)
            {
                var e = new DamageEvent(damageAmount, hit.Point, direction, _source);
                if (debugLogHits)
                {
                    float distFromProjectile = Vector2.Distance(hit.Point, transform.position);
                    var targetPos = (Vector2)hit.Health.transform.position;
                    float distToTarget = Vector2.Distance(hit.Point, targetPos);
                    Debug.Log($"[Projectile→Health] target={hit.Health.name} hitPoint={hit.Point} distFromProjectile={distFromProjectile:F2} distToTarget={distToTarget:F2}");
                }

                hit.Health.TakeDamage(e);
                hitEffect?.Trigger(e, hit.Health.gameObject);
                return;
            }

            if (hit.Chunk != null)
            {
                hit.Chunk.DamageByCellIndex(hit.AngleIndex, hit.RadiusIndex, damageAmount);
                if (hitEffect != null)
                {
                    var e = new DamageEvent(damageAmount, hit.Point, direction, _source);
                    hitEffect.Trigger(e, hit.Chunk.gameObject);
                }
            }
        }
 
         #endregion
     }
 }
