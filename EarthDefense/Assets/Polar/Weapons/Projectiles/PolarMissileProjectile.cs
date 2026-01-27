using Polar.Weapons.Data;
using UnityEngine;

namespace Polar.Weapons.Projectiles
{
    /// <summary>
    /// 미사일 투사체 (폭발 타입)
    /// - PolarMissileWeaponData와 매칭
    /// - 독립 로직: 극좌표 이동, 충돌 감지, 3단계 폭발 범위 피해 적용
    /// - SGSystem PoolService 완전 통합
    /// - 폭발 시스템: 폭심 (Core) → 유효 범위 (Effective) → 외곽 (Outer)
    /// </summary>
    public class PolarMissileProjectile : PolarProjectileBase
    {
        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        
        private PolarMissileWeaponData MissileData => _weaponData as PolarMissileWeaponData;

        // 수명 관리
        private float _spawnTime;
        private float _lifetime;

        // 공개 프로퍼티 (베이스 필드 사용)
        public float Angle => _angleDeg;
        public float Radius => _radius;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
        }

        public override void Launch(IPolarField field, PolarWeaponData weaponData)
        {
            if (weaponData is not PolarMissileWeaponData missileData)
            {
                Debug.LogError("[PolarMissileProjectile] Requires PolarMissileWeaponData!");
                return;
            }

            _field = field;
            _weaponData = weaponData;
            _isActive = true;

            _spawnTime = Time.time;
            _lifetime = missileData.MissileLifetime;

            if (_angleDeg == 0f && _radius == 0f && _speed == 0f)
            {
                Debug.LogWarning("[PolarMissile] Launch called without angle/radius! Using defaults.");
                _angleDeg = 0f;
                _radius = 0.8f;
                _speed = missileData.MissileSpeed;
                UpdatePolarPosition();
            }

            ActivateVisuals();
        }

        /// <summary>
        /// 특정 각도로 발사 (오버로드)
        /// </summary>
        public void Launch(IPolarField field, PolarWeaponData weaponData, float launchAngle, float startRadius)
        {
            Launch(field, weaponData);

            var missileData = weaponData as PolarMissileWeaponData;
            if (missileData == null)
            {
                Debug.LogError("[PolarMissileProjectile] Launch aborted: invalid weapon data");
                ReturnToPool();
                return;
            }

            LaunchPolar(field, weaponData, launchAngle, startRadius, missileData.MissileSpeed);
        }

        public override void Deactivate()
        {
            base.Deactivate();
            
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (trailRenderer != null) trailRenderer.emitting = false;
        }

        /// <summary>
        /// 풀 반환 시 추가 정리
        /// </summary>
        protected override void OnPoolReturn()
        {
            _spawnTime = 0f;
            _lifetime = 0f;

            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
                trailRenderer.Clear();
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (_field == null)
            {
                ReturnToPool();
                return;
            }

            if (UpdatePolarMovementAndCheckWallCollision(deltaTime, out int sectorIndex, out _))
            {
                Debug.Log($"[PolarMissile] Impact: sector={sectorIndex}, angle={_angleDeg:F1}°, radius={_radius:F2}, policy={_weaponData?.ImpactPolicy.hitResponse}");

                // 베이스가 정책(데미지/관통/무데미지/Effect)을 처리.
                // 여기서는 소멸 여부만: 관통이면 _hasReachedWall가 false로 돌아가므로 계속 진행.
                if (_hasReachedWall)
                {
                    ReturnToPool();
                }
                return;
            }

            // 화면 밖/안전장치
            if (_radius > _field.InitialRadius * 2f)
            {
                ReturnToPool();
                return;
            }

            // 수명 체크
            if (_lifetime > 0f && Time.time - _spawnTime > _lifetime)
            {
                // 수명 종료도 OnDestroy 트리거로 처리 가능
                ReturnToPool();
            }
        }

        private void ActivateVisuals()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = MissileData.MissileColor;
            }
            
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = true;
                trailRenderer.startColor = MissileData.MissileColor;
                trailRenderer.endColor = new Color(MissileData.MissileColor.r, MissileData.MissileColor.g, MissileData.MissileColor.b, 0f);
            }

            transform.localScale = Vector3.one * MissileData.MissileScale;
        }
    }
}
