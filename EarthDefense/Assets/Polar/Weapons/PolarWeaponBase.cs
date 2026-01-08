using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// Polar 무기 추상 베이스 클래스 (완전 상속 구조 + SGSystem 통합 + Arm 방식)
    /// - 공통: 발사 타이밍, 리소스 로딩, Arm 관리
    /// - 개별: 타입별 발사 로직
    /// - SGSystem: PoolService 전용
    /// - Arm: PolarWeaponArm을 통한 Muzzle 관리
    /// </summary>
    public abstract class PolarWeaponBase : MonoBehaviour
    {
        [SerializeField] protected PolarWeaponData weaponData;
        
        [Header("Arm Reference")]
        [Tooltip("이 무기가 부착된 암 (Arm Transform 노드)")]
        [SerializeField] protected PolarWeaponArm weaponArm;

        protected IPolarField _field;
        protected float _nextFireTime;
        protected Vector2 _currentAimTarget;  // 현재 조준점 (선택 사항)

        /// <summary>
        /// 머즐 (발사 지점) - Arm을 통해 접근
        /// </summary>
        public Transform Muzzle => weaponArm != null ? weaponArm.Muzzle : transform;

        /// <summary>
        /// 무기 초기화
        /// </summary>
        public virtual void Initialize(IPolarField field, PolarWeaponData data = null)
        {
            _field = field;
            if (data != null) weaponData = data;
            
            // WeaponArm 재확인 (자식에서)
            if (weaponArm == null)
            {
                weaponArm = GetComponentInChildren<PolarWeaponArm>();
            }
            
            OnInitialized();
        }

        /// <summary>
        /// 발사 가능 여부
        /// </summary>
        public virtual bool CanFire => Time.time >= _nextFireTime;

        /// <summary>
        /// 발사 (타입별 구현 필요)
        /// </summary>
        public abstract void Fire();

        /// <summary>
        /// 조준 업데이트 (선택 사항)
        /// - PolarAngle 모드: SetAimAngle(angle) 사용
        /// - MouseFollow 모드: SetAimFromWorldPosition(mousePos) 사용
        /// </summary>
        public virtual void UpdateAim(Vector2 worldPosition)
        {
            _currentAimTarget = worldPosition;
            
            // WeaponArm 회전 처리
            if (weaponArm != null)
            {
                weaponArm.SetAimFromWorldPosition(worldPosition);
            }
        }

        /// <summary>
        /// 각도로 조준 업데이트 (극좌표 전용)
        /// </summary>
        public virtual void UpdateAimAngle(float angleInDegrees)
        {
            if (weaponArm != null)
            {
                weaponArm.SetAimAngle(angleInDegrees);
            }
        }

        /// <summary>
        /// 초기화 후 호출 (타입별 추가 로직)
        /// </summary>
        protected virtual void OnInitialized() { }

        /// <summary>
        /// 쿨다운 설정
        /// </summary>
        protected void SetCooldown(float cooldown)
        {
            _nextFireTime = Time.time + cooldown;
        }

        /// <summary>
        /// 필드 중심점 획득
        /// </summary>
        protected Vector3 GetFieldCenter()
        {
            return (_field as Component) != null 
                ? ((Component)_field).transform.position 
                : Vector3.zero;
        }

        protected virtual void Reset()
        {
            // 자식에서 WeaponArm 자동 찾기
            if (weaponArm == null)
            {
                weaponArm = GetComponentInChildren<PolarWeaponArm>();
            }
        }
    }
}
