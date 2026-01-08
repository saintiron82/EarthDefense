using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 머신건 투사체 (일반 탄환 타입)
    /// - PolarMachinegunWeaponData와 매칭
    /// - 독립 로직: 극좌표 이동, 충돌 감지, 가우시안 피해 적용
    /// - SGSystem PoolService 완전 통합
    /// </summary>
    public class PolarMachinegunProjectile : PolarProjectileBase
    {
        [Header("Polar Coordinates")]
        [SerializeField] private float angle;
        [SerializeField] private float radius;
        
        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private LineRenderer lineRenderer;
        
        private PolarMachinegunWeaponData MachinegunData => _weaponData as PolarMachinegunWeaponData;
        
        private float speed;
        private float collisionEpsilon = 0.1f;
        private PolarCombatProperties? _combatProps;
        
        public float Angle => angle;
        public float Radius => radius;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        }

        public override void Launch(IPolarField field, PolarWeaponData weaponData)
        {
            if (weaponData is not PolarMachinegunWeaponData)
            {
                Debug.LogError("[PolarMachinegunProjectile] Requires PolarMachinegunWeaponData!");
                return;
            }

            _field = field;
            _weaponData = weaponData;
            _isActive = true;

            _combatProps = PolarCombatProperties.FromWeaponData(weaponData);

            angle = 0f;
            radius = 0.8f;
            speed = MachinegunData.ProjectileSpeed;

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
            if (lineRenderer != null) lineRenderer.enabled = false;
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
            if (lineRenderer != null) lineRenderer.enabled = false;
        }

        protected override void OnUpdate(float deltaTime)
        {
            radius += speed * deltaTime;
            UpdatePosition();
            
            if (CheckCollision())
            {
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
                spriteRenderer.color = MachinegunData.ProjectileColor;
            }
            
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = true;
                trailRenderer.startColor = MachinegunData.ProjectileColor;
                trailRenderer.endColor = new Color(MachinegunData.ProjectileColor.r, MachinegunData.ProjectileColor.g, MachinegunData.ProjectileColor.b, 0f);
            }
            
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.startColor = MachinegunData.ProjectileColor;
                lineRenderer.endColor = MachinegunData.ProjectileColor;
            }

            transform.localScale = Vector3.one * MachinegunData.ProjectileScale;
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

            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, _field.CenterPosition);
                lineRenderer.SetPosition(1, transform.position);
            }
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

        private void ApplyCombatDamage(int centerIndex, PolarCombatProperties props)
        {
            _field.SetLastWeaponKnockback(props.KnockbackPower);

            // 머신건은 주로 Gaussian 타입
            if (props.AreaType == PolarAreaType.Gaussian)
            {
                ApplyGaussianDamage(centerIndex, props);
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

        private void ApplyGaussianDamage(int centerIndex, PolarCombatProperties props)
        {
            int radius = props.DamageRadius;

            _field.ApplyDamageToSector(centerIndex, props.Damage);

            for (int offset = 1; offset <= radius; offset++)
            {
                float sigma = radius / 3f;
                float gaussian = Mathf.Exp(-offset * offset / (2f * sigma * sigma));
                float damage = props.Damage * gaussian;

                int leftIndex = (centerIndex - offset + _field.SectorCount) % _field.SectorCount;
                int rightIndex = (centerIndex + offset) % _field.SectorCount;

                _field.ApplyDamageToSector(leftIndex, damage);
                _field.ApplyDamageToSector(rightIndex, damage);
            }
        }
    }
}
