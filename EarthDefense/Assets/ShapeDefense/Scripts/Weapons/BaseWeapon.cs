using UnityEngine;
using ShapeDefense.Scripts.Data;

namespace ShapeDefense.Scripts.Weapons
{
    /// <summary>
    /// 모든 무기의 공통 기능을 제공하는 추상 기본 클래스
    /// WeaponData로부터 스펙을 주입받아 동작
    /// WeaponArm(Transform 노드)을 통해 Muzzle에 접근
    /// </summary>
    public abstract class BaseWeapon : MonoBehaviour
    {
        
        [Header("Arm Reference")]
        [Tooltip("이 무기가 부착된 암 (Arm Transform 노드)")]
        [SerializeField] protected WeaponArm weaponArm;
        
        [Header("Target Layers")]
        [SerializeField] protected LayerMask targetLayers = -1;
        
        // Runtime - WeaponData로부터 주입받는 값들
        protected WeaponData _weaponData;
        protected GameObject _source;
        protected int _sourceTeamKey;
        protected float _nextFireTime;
        protected Vector2 _currentAimTarget;
        protected bool _isFiring;
        
        // Properties - WeaponData로부터 값 가져오기
        public Transform Muzzle => weaponArm != null ? weaponArm.Muzzle : transform;
        public bool CanFire => Time.time >= _nextFireTime;
        public float FireRate => _weaponData?.FireRate ?? 10f;
        public float ProjectileDamage => _weaponData?.ProjectileDamage ?? 10f;
        public float ProjectileSpeed => _weaponData?.ProjectileSpeed ?? 20f;
        public float ProjectileLifetime => _weaponData?.ProjectileLifetime ?? 3f;
        public int ProjectileMaxHits => _weaponData?.ProjectileMaxHits ?? 1;
        public FireMode CurrentFireMode => _weaponData?.FireMode ?? FireMode.Manual;
        public string ProjectileBundleId => _weaponData?.ProjectileBundleId ?? "";
        public WeaponData WeaponData => _weaponData;
        
        protected virtual void Reset()
        {
            // 자식에서 WeaponArm 자동 찾기
            if (weaponArm == null)
            {
                weaponArm = GetComponentInChildren<WeaponArm>();
            }
            
            
        }
        
        protected virtual void Update()
        {
            // 자동 발사 모드 처리
            if (CurrentFireMode == FireMode.Automatic && _isFiring)
            {
                if (CanFire)
                {
                    Fire(_currentAimTarget);
                }
            }
        }
        
        /// <summary>
        /// 무기 초기화 - WeaponData 주입
        /// </summary>
        public virtual void Initialize(WeaponData weaponData, GameObject source, int sourceTeamKey)
        {
            _weaponData = weaponData;
            _source = source;
            _sourceTeamKey = sourceTeamKey;
            
            // WeaponArm 재확인 (자식에서)
            if (weaponArm == null)
            {
                weaponArm = GetComponentInChildren<WeaponArm>();
            }
            
            LoadProjectilePool();
            OnInitialized();
        }

        protected abstract void LoadProjectilePool();
        
        
        /// <summary>
        /// 초기화 완료 후 호출 - 서브클래스에서 오버라이드 가능
        /// </summary>
        protected virtual void OnInitialized() { }
        
        /// <summary>
        /// 발사 시작 (자동 모드용)
        /// </summary>
        public virtual void StartFire()
        {
            _isFiring = true;
        }
        
        /// <summary>
        /// 발사 중지 (자동 모드용)
        /// </summary>
        public virtual void StopFire()
        {
            _isFiring = false;
        }
        
        /// <summary>
        /// 조준 업데이트 - 무기가 마우스 방향을 추적
        /// </summary>
        public virtual void UpdateAim(Vector2 worldMousePosition)
        {
            _currentAimTarget = worldMousePosition;
            
            // WeaponArm 회전 처리
            if (weaponArm != null)
            {
                Vector2 armPos = weaponArm.transform.position;
                Vector2 dir = (worldMousePosition - armPos);
                
                if (dir.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    
                    // DirectMouseFollow 타입이면 즉시 회전
                    if (weaponArm.BehaviorType == ArmBehaviorType.DirectMouseFollow)
                    {
                        weaponArm.SetAimDirection(angle);
                    }
                }
            }
        }
        
        /// <summary>
        /// 발사 - 각 무기가 구현해야 함
        /// </summary>
        public virtual void Fire(Vector2 worldMousePosition)
        {
            // 기본 구현: 발사 방향 계산
            Vector2 startPos = Muzzle.position;
            Vector2 direction = (worldMousePosition - startPos).normalized;
            
            FireInternal(direction);
        }
        
        /// <summary>
        /// 실제 발사 로직 - 각 무기가 구현
        /// </summary>
        protected abstract void FireInternal(Vector2 direction);
        
        /// <summary>
        /// 대상이 유효한 타격 대상인지 확인
        /// </summary>
        protected virtual bool IsValidTarget(Health health)
        {
            if (health == null) return false;
            return health.TeamKey != _sourceTeamKey;
        }
    }
}

