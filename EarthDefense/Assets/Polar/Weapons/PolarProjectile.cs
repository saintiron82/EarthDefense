using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// Polar 전용 일반 투사체: 섹터 충돌 시 WeaponData.AreaType에 따라 피해 적용.
    /// </summary>
    public sealed class PolarProjectile : PolarProjectileBase
    {
        [SerializeField] private float collisionEpsilon = 0.05f;

        protected override void Tick(float deltaTime)
        {
            radius += speed * deltaTime;
            // 충돌 체크: 반지름이 필드 라인에 도달했다고 가정 (필드에서 반지름을 직접 제공받지 않으므로 간단화)
            // 실제 필드 반경 비교는 필드가 추가 데이터를 제공하도록 확장 가능
            int sectorIndex = field.AngleToSectorIndex(angleDeg);
            ApplyWeaponDamage(sectorIndex);
            isActive = false; // 1회 충돌 후 종료
        }

        protected override void ApplyWeaponDamage(int centerIndex)
        {
            if (weaponData == null || field == null) return;
            float damage = weaponData.Damage;
            field.SetLastWeaponKnockback(weaponData.KnockbackPower);

            switch (weaponData.AreaType)
            {
                case PolarAreaType.Fixed:
                    field.ApplyDamageToSector(centerIndex, damage);
                    break;
                case PolarAreaType.Gaussian:
                    ApplyGaussian(centerIndex, damage);
                    break;
                case PolarAreaType.Explosion:
                    ApplyExplosion(centerIndex, damage);
                    break;
            }

            if (field.EnableWoundSystem)
            {
                field.ApplyWound(centerIndex, weaponData.WoundIntensity);
            }
        }

        private void ApplyGaussian(int centerIndex, float baseDamage)
        {
            int radius = weaponData.DamageRadius;
            field.ApplyDamageToSector(centerIndex, baseDamage);
            for (int offset = 1; offset <= radius; offset++)
            {
                float sigma = Mathf.Max(0.0001f, radius / 3f);
                float gaussian = Mathf.Exp(-offset * offset / (2f * sigma * sigma));
                float d = baseDamage * gaussian;
                int left = Mod(centerIndex - offset, field.SectorCount);
                int right = Mod(centerIndex + offset, field.SectorCount);
                field.ApplyDamageToSector(left, d);
                field.ApplyDamageToSector(right, d);
            }
        }

        private void ApplyExplosion(int centerIndex, float baseDamage)
        {
            int radius = weaponData.DamageRadius;
            field.ApplyDamageToSector(centerIndex, baseDamage);
            for (int offset = 1; offset <= radius; offset++)
            {
                float falloff = weaponData.UseGaussianFalloff
                    ? Mathf.Exp(-offset * offset / (2f * Mathf.Pow(Mathf.Max(0.0001f, radius / 3f), 2)))
                    : 1f - (float)offset / (radius + 1);
                float d = baseDamage * falloff;
                int left = Mod(centerIndex - offset, field.SectorCount);
                int right = Mod(centerIndex + offset, field.SectorCount);
                field.ApplyDamageToSector(left, d);
                field.ApplyDamageToSector(right, d);
            }
        }

        private static int Mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}
