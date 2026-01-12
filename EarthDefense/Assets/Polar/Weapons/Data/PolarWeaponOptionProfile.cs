using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 공통 무기 옵션 프로필 (여러 무기가 공유하는 기본 스탯)
    /// </summary>
    [CreateAssetMenu(fileName = "PolarWeaponOptionProfile", menuName = "EarthDefense/Polar/Weapon Option Profile", order = 50)]
    public class PolarWeaponOptionProfile : ScriptableObject
    {
        [Header("Combat")]
        [SerializeField] private float damage = 5f;
        [SerializeField] private float knockbackPower = 0.2f;
        [SerializeField] private PolarAreaType areaType = PolarAreaType.Fixed;
        [SerializeField, Min(0)] private int damageRadius = 0;
        [SerializeField] private bool useGaussianFalloff = true;
        [SerializeField, Range(0f, 1f)] private float woundIntensity = 0.2f;
        [Tooltip("초당 타격 횟수 (레이저/틱 무기) ")]
        [SerializeField, Min(0.1f)] private float tickRate = 10f;

        public float Damage => damage;
        public float KnockbackPower => knockbackPower;
        public PolarAreaType AreaType => areaType;
        public int DamageRadius => Mathf.Max(0, damageRadius);
        public bool UseGaussianFalloff => useGaussianFalloff;
        public float WoundIntensity => woundIntensity;
        public float TickRate => tickRate;
    }
}
