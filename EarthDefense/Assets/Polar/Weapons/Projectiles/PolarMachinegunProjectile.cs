using UnityEngine;

namespace Polar.Weapons.Projectiles
{
    /// <summary>
    /// 머신건 투사체 (일반 탄환 타입)
    /// - PolarMachinegunWeaponData와 매칭
    /// - 독립 로직: 극좌표 이동, 충돌 감지, 단일 섹터 타격
    /// - SGSystem PoolService 완전 통합
    /// - 빠른 연사, 정확한 단일 타격
    /// </summary>
    public class PolarMachinegunProjectile : PolarProjectileBase
    {
        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private LineRenderer lineRenderer;
        
        private PolarMachinegunWeaponData MachinegunData => _weaponData as PolarMachinegunWeaponData;

        // 수명 관리
        private float _spawnTime;
        private float _lifetime;
        
        public float Angle => _angleDeg;
        public float Radius => _radius;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        }

        public override void Launch(IPolarField field, PolarWeaponData weaponData)
        {
            if (weaponData is not PolarMachinegunWeaponData machinegunData)
            {
                Debug.LogError("[PolarMachinegunProjectile] Requires PolarMachinegunWeaponData!");
                return;
            }

            _field = field;
            _weaponData = weaponData;
            _isActive = true;

            // 수명 초기화
            _spawnTime = Time.time;
            _lifetime = machinegunData.ProjectileLifetime;

            // ⚠️ 기본 각도/반경 설정 (오버로드 호출 안될 때 대비)
            if (_angleDeg == 0f && _radius == 0f && _speed == 0f)
            {
                Debug.LogWarning("[PolarMachinegun] Launch called without angle/radius! Using defaults.");
                _angleDeg = 0f;
                _radius = 0.8f;
                _speed = machinegunData.ProjectileSpeed;
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

            // Launch가 타입 불일치로 실패하면 _weaponData가 세팅되지 않을 수 있으니 방어
            if (MachinegunData == null)
            {
                Debug.LogError("[PolarMachinegunProjectile] Launch aborted: invalid weapon data");
                ReturnToPool();
                return;
            }

            LaunchPolar(field, weaponData, launchAngle, startRadius, MachinegunData.ProjectileSpeed);
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
            _spawnTime = 0f;
            _lifetime = 0f;
            
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
            if (UpdatePolarMovementAndCheckWallCollision(deltaTime, out int _, out _))
            {
                // 벽 충돌 처리(Effect/데미지/관통 여부)는 베이스가 수행
                // 소멸 여부만 여기서 결정: 관통 정책이면 _hasReachedWall가 false로 되돌려져 다음 프레임 계속 진행
                if (_hasReachedWall)
                {
                    ReturnToPool();
                }
                return;
            }
            
            if (_radius > _field.InitialRadius * 2f)
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
    }
}
