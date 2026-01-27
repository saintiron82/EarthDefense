using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 투사체 충돌 시 발동되는 추가 효과 인터페이스
    /// </summary>
    public interface IPolarProjectileEffect
    {
        /// <summary>
        /// 충돌 시 발동
        /// </summary>
        /// <param name="field">필드</param>
        /// <param name="sectorIndex">충돌 섹터</param>
        /// <param name="position">충돌 위치</param>
        /// <param name="weaponData">무기 데이터</param>
        void OnImpact(IPolarField field, int sectorIndex, Vector2 position, PolarWeaponData weaponData);
    }
}

