using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// Polar 전용 투사체 베이스: 극좌표 섹터에 직접 피해를 적용.
    /// </summary>
    public abstract class PolarProjectileBase : MonoBehaviour
    {
        protected IPolarField field;
        protected PolarWeaponData weaponData;
        protected float angleDeg;
        protected float radius;
        protected float speed;
        protected bool isActive;

        public virtual void Launch(IPolarField polarField, PolarWeaponData data, float launchAngleDeg, float startRadius, float projectileSpeed)
        {
            field = polarField;
            weaponData = data;
            angleDeg = launchAngleDeg;
            radius = startRadius;
            speed = projectileSpeed;
            isActive = true;
            OnLaunched();
        }

        protected virtual void Update()
        {
            if (!isActive || field == null) return;
            Tick(Time.deltaTime);
        }

        protected abstract void Tick(float deltaTime);
        protected abstract void ApplyWeaponDamage(int sectorIndex);
        protected virtual void OnLaunched() { }
    }
}
