using System.Collections.Generic;
using UnityEngine;

namespace ShapeDefense.Scripts.Polar
{
    /// <summary>
    /// Phase 1 - Step 4: 극좌표 투사체 관리자
    /// 자동 발사, 풀링, 충돌 처리
    /// </summary>
    public class PolarProjectileManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PolarFieldController controller;
        [SerializeField] private GameObject projectilePrefab;
        
        [Header("Spawn Settings")]
        [SerializeField] private float spawnRadius = 0.8f;  // 지구 표면에서 발사
        [SerializeField] private Transform projectileContainer;
        
        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 20;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        // 풀
        private List<PolarProjectile> _projectilePool;
        private float _fireTimer;
        
        // 통계
        private int _totalFired;
        private int _totalHits;

        private void Awake()
        {
            // Disabled: auto-fire/manager no longer used (ShapeDefense weapon system takes over)
            enabled = false;
        }

        /// <summary>
        /// 투사체 풀 초기화
        /// </summary>
        private void InitializePool()
        {
            _projectilePool = new List<PolarProjectile>();
            
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[PolarProjectileManager] Projectile prefab not assigned! Creating simple prefab.");
                projectilePrefab = CreateDefaultProjectilePrefab();
            }
            
            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(projectilePrefab, projectileContainer);
                PolarProjectile projectile = obj.GetComponent<PolarProjectile>();
                
                if (projectile == null)
                {
                    projectile = obj.AddComponent<PolarProjectile>();
                }
                
                projectile.Deactivate();
                _projectilePool.Add(projectile);
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[PolarProjectileManager] Pool initialized with {poolSize} projectiles");
            }
        }

        /// <summary>
        /// 기본 투사체 프리팹 생성 (폴백)
        /// </summary>
        private GameObject CreateDefaultProjectilePrefab()
        {
            GameObject prefab = new GameObject("DefaultProjectile");
            
            // ✅ LineRenderer (레이저 빔 효과)
            LineRenderer lr = prefab.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.15f;
            lr.endWidth = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.yellow;
            lr.endColor = Color.yellow;
            lr.numCornerVertices = 2;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.enabled = false;  // Launch 시 활성화
            
            // Sprite Renderer (투사체 본체)
            SpriteRenderer sr = prefab.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = Color.yellow;
            sr.sortingOrder = 10;  // 장막 위에 표시
            sr.enabled = false;
            
            // Trail Renderer (궤적)
            TrailRenderer tr = prefab.AddComponent<TrailRenderer>();
            tr.time = 0.3f;
            tr.startWidth = 0.08f;
            tr.endWidth = 0.01f;
            tr.material = new Material(Shader.Find("Sprites/Default"));
            tr.startColor = Color.yellow;
            tr.endColor = new Color(1f, 1f, 0f, 0f);
            tr.numCornerVertices = 2;
            tr.numCapVertices = 2;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.emitting = false;
            
            if (showDebugLogs)
            {
                Debug.Log("[PolarProjectileManager] Created default projectile prefab with LineRenderer, SpriteRenderer, and TrailRenderer");
            }
            
            return prefab;
        }

        /// <summary>
        /// 간단한 원형 스프라이트 생성
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            int size = 64;  // ✅ 32 → 64 (더 선명하게)
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    
                    // ✅ 안티앨리어싱 추가
                    float alpha = 1f - Mathf.Clamp01((dist - radius) / 2f);
                    
                    if (alpha > 0f)
                    {
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;  // ✅ 부드럽게
            tex.Apply();
            
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private void Update()
        {
            // Disabled: handled by player weapon system
        }

        /// <summary>
        /// 랜덤 각도로 투사체 발사
        /// </summary>
        private void FireRandomProjectile()
        {
            // 랜덤 각도 (0 ~ 360)
            float randomAngle = Random.Range(0f, 360f);
            
            FireProjectile(randomAngle);
        }

        /// <summary>
        /// 특정 각도로 투사체 발사
        /// </summary>
        public void FireProjectile(float angle)
        {
            PolarProjectile projectile = GetPooledProjectile();
            if (projectile == null)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning("[PolarProjectileManager] No available projectiles in pool!");
                }
                return;
            }
            
            // 발사 (Manager 참조 추가)
            projectile.Launch(
                controller,
                this,  // ✅ Manager 자신 전달
                angle,
                spawnRadius,
                controller.Config.BulletSpeed,
                controller.Config.CollisionEpsilon
            );
            
            _totalFired++;
            
            if (showDebugLogs && _totalFired % 10 == 0)
            {
                Debug.Log($"[PolarProjectileManager] Total fired: {_totalFired}, Hits: {_totalHits}");
            }
        }

        /// <summary>
        /// 풀에서 비활성 투사체 가져오기
        /// </summary>
        private PolarProjectile GetPooledProjectile()
        {
            foreach (PolarProjectile projectile in _projectilePool)
            {
                if (!projectile.IsActive)
                {
                    return projectile;
                }
            }
            return null;
        }

        /// <summary>
        /// 활성 투사체 처리 (충돌 체크)
        /// </summary>
        private void ProcessActiveProjectiles()
        {
            // ✅ 투사체가 자체적으로 충돌 체크하고 OnProjectileCollision 호출
            // 여기서는 중복 체크 제거 (투사체가 알아서 처리)
            
            // 추가 안전 체크만 수행 (선택)
            if (!showDebugLogs) return;
            
            int activeCount = 0;
            foreach (PolarProjectile projectile in _projectilePool)
            {
                if (projectile.IsActive) activeCount++;
            }
            
            // 10초마다 활성 개수 로그
            if (Time.frameCount % 600 == 0)
            {
                Debug.Log($"[PolarProjectileManager] Active projectiles: {activeCount}/{poolSize}");
            }
        }

        /// <summary>
        /// 투사체 충돌 콜백 (PolarProjectile에서 호출)
        /// </summary>
        public void OnProjectileCollision(PolarProjectile projectile, int hitSectorIndex)
        {
            if (projectile == null || !projectile.IsActive) return;

            if (showDebugLogs)
            {
                Debug.Log($"[PolarProjectileManager] OnProjectileCollision: sector={hitSectorIndex}, angle={projectile.Angle:F1}°");
            }

            // 폭발 실행
            ExecuteExplosion(hitSectorIndex);
            
            _totalHits++;
        }

        /// <summary>
        /// 폭발 실행 (HTML executeAreaExplosion 재현)
        /// </summary>
        private void ExecuteExplosion(int centerSectorIndex)
        {
            if (controller == null) return;

            float missileForce = controller.Config.MissileForce;
            int explosionRadius = controller.Config.ExplosionRadius;
            float woundIntensity = controller.Config.ExplosionWoundIntensity;
            
            if (showDebugLogs)
            {
                Debug.Log($"[PolarProjectileManager] Explosion START: sector={centerSectorIndex}, force={missileForce:F2}, radius={explosionRadius}");
            }
            
            // 중앙 섹터 밀어내기
            controller.PushSectorRadius(centerSectorIndex, missileForce);
            
            // 주변 섹터 밀어내기 (거리 감쇄)
            for (int offset = 1; offset <= explosionRadius; offset++)
            {
                float falloff = 1f - (float)offset / (explosionRadius + 1);
                float pushAmount = missileForce * falloff;
                
                int leftIndex = (centerSectorIndex - offset + controller.SectorCount) % controller.SectorCount;
                int rightIndex = (centerSectorIndex + offset) % controller.SectorCount;
                
                controller.PushSectorRadius(leftIndex, pushAmount);
                controller.PushSectorRadius(rightIndex, pushAmount);
            }
            
            // 상처 적용 (선택)
            if (controller.Config.EnableWoundSystem)
            {
                controller.ApplyWound(centerSectorIndex, woundIntensity);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[PolarProjectileManager] Wound applied: sector={centerSectorIndex}, intensity={woundIntensity:F2}");
                }
            }
        }
    }
}
