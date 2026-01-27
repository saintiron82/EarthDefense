using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// Polar 무기 베이스 클래스
    /// - 공통: 초기화, 필드 관리, Muzzle 관리
    /// - 개별: 발사 로직 (Fire 메서드)
    /// </summary>
    public abstract class PolarWeaponBase : MonoBehaviour
    {
        [SerializeField] protected PolarWeaponData weaponData;
        [SerializeField] protected Transform muzzle;

        protected IPolarField _field;
        protected Transform Muzzle => muzzle != null ? muzzle : transform;

        /// <summary>
        /// 무기 초기화
        /// </summary>
        public virtual void Initialize(IPolarField field, PolarWeaponData data = null)
        {
            _field = field;
            if (data != null) weaponData = data;
            OnInitialized();
        }

        /// <summary>
        /// 초기화 후 호출 (하위 클래스 전용)
        /// </summary>
        protected virtual void OnInitialized() { }

        /// <summary>
        /// 발사 (하위 클래스에서 구현)
        /// </summary>
        public abstract void Fire(float angleDeg, float startRadius);

        /// <summary>
        /// 필드 중심점 획득
        /// </summary>
        protected Vector3 GetFieldCenter()
        {
            return (_field as Component) != null 
                ? ((Component)_field).transform.position 
                : Vector3.zero;
        }
    }
}

