﻿using Polar.Weapons.Data;
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
        
        // 수명 관리
        private float _spawnTime;
        private float _lifetime;
        
        public float Angle => angle;
        public float Radius => radius;

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

            _combatProps = PolarCombatProperties.FromWeaponData(weaponData);

            angle = 0f;
            radius = 0.8f;
            speed = missileData.MissileSpeed;
            
            // 수명 초기화
            _spawnTime = Time.time;
            _lifetime = 10f; // 미사일 기본 수명 10초

            Debug.Log($"[PolarMissile] Launched: Damage={_combatProps.Value.Damage}, AreaType={_combatProps.Value.AreaType}, " +
                     $"CoreRadius={missileData.CoreRadius}, EffectiveRadius={missileData.EffectiveRadius}, MaxRadius={missileData.MaxRadius}");

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
            radius += speed * deltaTime;
            UpdatePosition();
            
            if (CheckCollision())
            {
                Debug.Log($"[PolarMissile] Collision detected at angle={angle:F1}°, radius={radius:F2}");
                SpawnExplosionVFX();
                OnCollision();
                ReturnToPool();
                return;
            }
            
            if (radius > _field.InitialRadius * 2f)
            {
                ReturnToPool();
                return;
            }
            
            // 수명 체크 (메모리 누수 방지)
            if (Time.time - _spawnTime > _lifetime)
            {
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
            if (_field == null) return;

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

            // 미사일 전용 데이터가 있으면 3단계 폭발 시스템 사용
            var missileData = _weaponData as PolarMissileWeaponData;
            if (missileData != null)
            {
                Debug.Log($"[PolarMissile] Applying 3-stage explosion damage at sector {centerIndex}, Base Damage: {props.Damage}");
                ApplyExplosionDamage(centerIndex, props);
            }
            else if (props.AreaType == PolarAreaType.Explosion)
            {
                Debug.Log($"[PolarMissile] Applying explosion damage (fallback) at sector {centerIndex}, Damage: {props.Damage}");
                ApplyExplosionDamage(centerIndex, props);
            }
            else
            {
                Debug.LogWarning($"[PolarMissile] No explosion data! Applying single sector damage: {props.Damage}, AreaType: {props.AreaType}");
                _field.ApplyDamageToSector(centerIndex, props.Damage);
            }

            if (_field.EnableWoundSystem)
            {
                _field.ApplyWound(centerIndex, props.WoundIntensity);
            }
        }

        private void ApplyExplosionDamage(int centerIndex, PolarCombatProperties props)
        {
            var missileData = _weaponData as PolarMissileWeaponData;
            if (missileData == null)
            {
                // Fallback: 단순 단일 섹터 타격
                Debug.LogWarning($"[PolarMissile] MissileData is null! Applying fallback damage: {props.Damage}");
                _field.ApplyDamageToSector(centerIndex, props.Damage);
                return;
            }

            float baseDamage = props.Damage;
            int coreRadius = missileData.CoreRadius;
            int effectiveRadius = missileData.EffectiveRadius;
            int maxRadius = missileData.MaxRadius;

            Debug.Log($"[PolarMissile] 3-Stage Explosion: Core={coreRadius}, Effective={effectiveRadius}, Max={maxRadius}, BaseDamage={baseDamage}");
            int totalSectorsHit = 0;

            // 1. 폭심 (Core) - 풀 데미지 영역
            for (int offset = 0; offset <= coreRadius; offset++)
            {
                float coreDamage = baseDamage * missileData.CoreMultiplier;
                
                if (offset == 0)
                {
                    // 중심
                    _field.ApplyDamageToSector(centerIndex, coreDamage);
                    totalSectorsHit++;
                    Debug.Log($"  [Core] Center sector {centerIndex}: {coreDamage} damage");
                }
                else
                {
                    // 폭심 범위 내
                    int leftIndex = (centerIndex - offset + _field.SectorCount) % _field.SectorCount;
                    int rightIndex = (centerIndex + offset) % _field.SectorCount;
                    
                    _field.ApplyDamageToSector(leftIndex, coreDamage);
                    _field.ApplyDamageToSector(rightIndex, coreDamage);
                    totalSectorsHit += 2;
                }
            }

            // 2. 유효 범위 (Effective) - 의미있는 데미지 영역
            for (int offset = coreRadius + 1; offset <= effectiveRadius; offset++)
            {
                float t = (float)(offset - coreRadius) / (effectiveRadius - coreRadius);
                float falloff = CalculateFalloff(
                    missileData.CoreMultiplier,
                    missileData.EffectiveMinMultiplier,
                    t,
                    missileData.FalloffType
                );
                float damage = baseDamage * falloff;

                int leftIndex = (centerIndex - offset + _field.SectorCount) % _field.SectorCount;
                int rightIndex = (centerIndex + offset) % _field.SectorCount;

                _field.ApplyDamageToSector(leftIndex, damage);
                _field.ApplyDamageToSector(rightIndex, damage);
                totalSectorsHit += 2;
            }

            // 3. 외곽 범위 (Outer) - 감쇠 영역
            for (int offset = effectiveRadius + 1; offset <= maxRadius; offset++)
            {
                float t = (float)(offset - effectiveRadius) / (maxRadius - effectiveRadius);
                float falloff = CalculateFalloff(
                    missileData.EffectiveMinMultiplier,
                    missileData.MaxMinMultiplier,
                    t,
                    missileData.FalloffType
                );
                float damage = baseDamage * falloff;

                int leftIndex = (centerIndex - offset + _field.SectorCount) % _field.SectorCount;
                int rightIndex = (centerIndex + offset) % _field.SectorCount;

                _field.ApplyDamageToSector(leftIndex, damage);
                _field.ApplyDamageToSector(rightIndex, damage);
                totalSectorsHit += 2;
            }

            Debug.Log($"[PolarMissile] Explosion complete: {totalSectorsHit} sectors hit");
        }

        private float CalculateFalloff(float start, float end, float t, ExplosionFalloffType type)
        {
            switch (type)
            {
                case ExplosionFalloffType.Linear:
                    return Mathf.Lerp(start, end, t);

                case ExplosionFalloffType.Smooth:
                    return Mathf.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));

                case ExplosionFalloffType.Exponential:
                    return Mathf.Lerp(start, end, t * t);

                default:
                    return Mathf.Lerp(start, end, t);
            }
        }
    }
}
