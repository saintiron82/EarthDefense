using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 미사일 투사체 (폭발 타입)
    /// - PolarMissileWeaponData와 매칭
    /// - 독립 로직: 극좌표 이동, 충돌 감지, 폭발 범위 피해 적용
    /// - SGSystem PoolService 완전 통합
    /// </summary>
    public class PolarMissileProjectile : PolarProjectileBase
    {
        [Header("Polar Coordinates")]
        [SerializeField] private float angle;
        [SerializeField] private float radius;
        
        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private GameObject explosionVFX;
        
        private PolarMissileWeaponData MissileData => _weaponData as PolarMissileWeaponData;
        
        private float speed;
        private float collisionEpsilon = 0.1f;
        private PolarCombatProperties? _combatProps;
        
        public float Angle => angle;
        public float Radius => radius;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
        }

        public override void Launch(IPolarField field, PolarWeaponData weaponData)
        {
            if (weaponData is not PolarMissileWeaponData)
            {
                Debug.LogError("[PolarMissileProjectile] Requires PolarMissileWeaponData!");
                return;
            }

            _field = field;
            _weaponData = weaponData;
            _isActive = true;

            _combatProps = PolarCombatProperties.FromWeaponData(weaponData);

            angle = 0f;
            radius = 0.8f;
            speed = MissileData.MissileSpeed;

            ActivateVisuals();
            UpdatePosition();
        }

        /// <summary>
        /// 특정 각도로 발사 (오버로드)
        /// </summary>
        public void Launch(IPolarField field, PolarWeaponData weaponData, float launchAngle, float startRadius)
        {
            Launch(field, weaponData);
            angle = launchAngle;
            radius = startRadius;
            UpdatePosition();
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
            angle = 0f;
            radius = 0f;
            speed = 0f;
            _combatProps = null;
            
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
                trailRenderer.Clear();
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            radius += speed * deltaTime;
            UpdatePosition();
            
            if (CheckCollision())
            {
                SpawnExplosionVFX();
                OnCollision();
                ReturnToPool();  // ✅ Deactivate() → ReturnToPool()
                return;
            }
            
            if (radius > _field.InitialRadius * 2f)
            {
                ReturnToPool();  // ✅ 범위 이탈 시 풀 반환
                return;
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

        private void UpdatePosition()
        {
            if (_field == null) return;

            float angleRad = angle * Mathf.Deg2Rad;
            Vector3 polarPos = new Vector3(
                Mathf.Cos(angleRad) * radius,
                Mathf.Sin(angleRad) * radius,
                0f
            );
            
            transform.position = _field.CenterPosition + polarPos;

            // 미사일 회전 (진행 방향)
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private bool CheckCollision()
        {
            if (_field == null) return false;

            int sectorIndex = _field.AngleToSectorIndex(angle);
            float sectorRadius = _field.GetSectorRadius(sectorIndex);
            
            return radius >= (sectorRadius - collisionEpsilon);
        }

        private void OnCollision()
        {
            if (_field == null)
            {
                return;
            }

            int hitSectorIndex = _field.AngleToSectorIndex(angle);

            if (_combatProps.HasValue)
            {
                ApplyCombatDamage(hitSectorIndex, _combatProps.Value);
            }
        }

        private void SpawnExplosionVFX()
        {
            if (MissileData.ExplosionVFXPrefab == null) return;

            var vfx = Instantiate(MissileData.ExplosionVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 2f); // 2초 후 자동 제거
        }

        private void ApplyCombatDamage(int centerIndex, PolarCombatProperties props)
        {
            _field.SetLastWeaponKnockback(props.KnockbackPower);

            // 미사일은 주로 Explosion 타입
            if (props.AreaType == PolarAreaType.Explosion)
            {
                ApplyExplosionDamage(centerIndex, props);
            }
            else
            {
                _field.ApplyDamageToSector(centerIndex, props.Damage);
            }

            if (_field.EnableWoundSystem)
            {
                _field.ApplyWound(centerIndex, props.WoundIntensity);
            }
        }

        private void ApplyExplosionDamage(int centerIndex, PolarCombatProperties props)
        {
            int radius = props.DamageRadius;

            _field.ApplyDamageToSector(centerIndex, props.Damage);

            for (int offset = 1; offset <= radius; offset++)
            {
                float falloff = props.UseGaussianFalloff
                    ? Mathf.Exp(-offset * offset / (2f * (radius / 3f) * (radius / 3f)))
                    : 1f - (float)offset / (radius + 1);

                float damage = props.Damage * falloff;

                int leftIndex = (centerIndex - offset + _field.SectorCount) % _field.SectorCount;
                int rightIndex = (centerIndex + offset) % _field.SectorCount;

                _field.ApplyDamageToSector(leftIndex, damage);
                _field.ApplyDamageToSector(rightIndex, damage);
            }
        }
    }
}
