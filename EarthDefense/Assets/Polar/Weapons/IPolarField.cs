namespace Polar.Weapons
{
    /// <summary>
    /// Polar 전투 시스템이 제공해야 하는 최소 인터페이스.
    /// 기존 ShapeDefense 의존 없이 독립 운용을 위한 계약.
    /// </summary>
    public interface IPolarField
    {
        int SectorCount { get; }
        float InitialRadius { get; }
        UnityEngine.Vector3 CenterPosition { get; }
        bool EnableWoundSystem { get; }
        
        int AngleToSectorIndex(float angleDeg);
        float SectorIndexToAngle(int sectorIndex);
        float GetSectorRadius(int sectorIndex);
        void ApplyDamageToSector(int sectorIndex, float damage);
        void SetLastWeaponKnockback(float power);
        void ApplyWound(int sectorIndex, float intensity);
    }
}
