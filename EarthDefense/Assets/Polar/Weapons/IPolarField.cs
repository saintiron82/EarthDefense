namespace Polar.Weapons
{
    /// <summary>
    /// Polar 전투 시스템이 제공해야 하는 최소 인터페이스.
    /// 기존 ShapeDefense 의존 없이 독립 운용을 위한 계약.
    /// </summary>
    public interface IPolarField
    {
        int SectorCount { get; }
        int AngleToSectorIndex(float angleDeg);
        void ApplyDamageToSector(int sectorIndex, float damage);
        void SetLastWeaponKnockback(float power);
        bool EnableWoundSystem { get; }
        void ApplyWound(int sectorIndex, float intensity);
    }
}
