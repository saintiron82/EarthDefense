using System.Collections.Generic;
using UnityEngine;

namespace ShapeDefense.Scripts.Weapons
{
    /// <summary>
    /// 폭발 미사일: 목표 지점에 도달하거나 적과 충돌 시 범위 내 모든 적에게 동시 데미지
    /// </summary>
    public sealed class ExplosiveBullet : BaseProjectile
    {
        [Header("Projectile Settings")]
        [SerializeField] private float speed = 10f;
        
        [Header("Explosion Settings")]
        [SerializeField] private float explosionRadius = 2f;
        [SerializeField] private GameObject explosionEffectPrefab;
        
        [Header("Debug")]
        [SerializeField] private bool showExplosionRadius = true;
        [SerializeField] private Color explosionRadiusColor = new Color(1f, 0.5f, 0f, 0.3f);

        private float _runtimeRadius;

        private readonly Collider2D[] _overlapBuffer = new Collider2D[32];

        protected override void Awake()
        {
            base.Awake();
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = projectileColor;
            }
        }

        /// <summary>
        /// 발사 시 초기화 - 폭발 반경 설정 및 회전
        /// </summary>
        protected override void OnFired()
        {
            _runtimeRadius = explosionRadius;
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = projectileColor;
            }
            
            // 미사일 회전 (진행 방향으로)
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        protected override void UpdateProjectile()
        {
            // 이동
            transform.position += (Vector3)(_direction * (Speed * Time.deltaTime));
        }

        protected override void OnLifetimeExpired()
        {
            Explode();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 적과 충돌 시 폭발
            if (IsValidTarget(other))
            {
                Explode();
            }
        }

        private void Explode()
        {
            if (!_isActive) return;
            
            Vector2 explosionCenter = transform.position;
            
            // 범위 내 모든 콜라이더 검색 (NonAlloc)
            int hitCount = Physics2D.OverlapCircleNonAlloc(explosionCenter, _runtimeRadius, _overlapBuffer, targetLayers);
            
            HashSet<Health> damagedTargets = new HashSet<Health>();
            
            for (int i = 0; i < hitCount; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null) continue;
                if (!col.TryGetComponent<Health>(out var health)) continue;
                if (health.TeamKey == _sourceTeamKey) continue; // 같은 팀은 무시
                if (damagedTargets.Contains(health)) continue; // 중복 방지
                
                // 거리 기반 감쇠 (선택적)
                float distance = Vector2.Distance(explosionCenter, col.transform.position);
                float damageMultiplier = 1f - (distance / _runtimeRadius) * 0.3f; // 최대 30% 감쇠
                float finalDamage = Damage * damageMultiplier;
                
                Vector2 direction = ((Vector2)col.transform.position - explosionCenter).normalized;
                ApplyDamage(health, explosionCenter, direction, finalDamage);
                
                damagedTargets.Add(health);
            }
            
            // 폭발 이펙트 생성
            if (explosionEffectPrefab != null)
            {
                var effect = Instantiate(explosionEffectPrefab, explosionCenter, Quaternion.identity);
                effect.transform.localScale = Vector3.one * _runtimeRadius;
            }
            
            // 자기 자신 제거/반환
            ReturnToPool();
        }

        // sealed 클래스에서 OnDrawGizmosSelected를 재정의하지 않고 별도 메서드로 유지
        private void OnDrawGizmos()
        {
            if (!showExplosionRadius) return;
            Gizmos.color = explosionRadiusColor;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
