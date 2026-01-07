using UnityEngine;
using ShapeDefense.Scripts.Polar;

namespace Polar.Weapons
{
    /// <summary>
    /// ShapeDefense의 PolarFieldController를 IPolarField 계약에 맞춰 래핑하는 어댑터.
    /// </summary>
    public sealed class PolarFieldAdapter : MonoBehaviour, IPolarField
    {
        [SerializeField] private PolarFieldController controller;

        public int SectorCount => controller != null ? controller.SectorCount : 0;
        public bool EnableWoundSystem => controller != null && controller.Config != null && controller.Config.EnableWoundSystem;

        private void Reset()
        {
            if (controller == null)
            {
                controller = GetComponent<PolarFieldController>();
            }
        }

        public int AngleToSectorIndex(float angleDeg)
        {
            return controller != null ? controller.AngleToSectorIndex(angleDeg) : -1;
        }

        public void ApplyDamageToSector(int sectorIndex, float damage)
        {
            if (controller == null) return;
            controller.ApplyDamageToSector(sectorIndex, damage);
        }

        public void SetLastWeaponKnockback(float power)
        {
            if (controller == null) return;
            controller.SetLastWeaponKnockback(power);
        }

        public void ApplyWound(int sectorIndex, float intensity)
        {
            if (controller == null) return;
            controller.ApplyWound(sectorIndex, intensity);
        }
    }
}
