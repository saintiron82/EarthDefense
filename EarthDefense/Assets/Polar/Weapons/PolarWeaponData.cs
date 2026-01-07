using UnityEngine;

namespace Polar.Weapons
{
    public enum PolarAreaType { Fixed, Gaussian, Explosion }

    [CreateAssetMenu(fileName = "PolarWeaponData", menuName = "EarthDefense/Polar/WeaponData", order = 1)]
    public class PolarWeaponData : ScriptableObject
    {
        [Header("ID & UI")]
        [SerializeField] private string id;
        [SerializeField] private string weaponName;
        [SerializeField] private Sprite icon;

        [Header("Bundles")]
        [SerializeField] private string weaponBundleId;
        [SerializeField] private string projectileBundleId;
        [SerializeField] private string beamBundleId;

        [Header("Combat")]
        [SerializeField] private float damage = 5f;
        [SerializeField] private float knockbackPower = 0.2f;
        [SerializeField] private PolarAreaType areaType = PolarAreaType.Fixed;
        [SerializeField, Min(0)] private int damageRadius = 0;
        [SerializeField] private bool useGaussianFalloff = true;
        [SerializeField, Range(0f, 1f)] private float woundIntensity = 0.2f;

        [Header("Beam (optional)")]
        [SerializeField, Min(0.1f)] private float tickRate = 10f;

        public string Id => id;
        public string WeaponName => weaponName;
        public Sprite Icon => icon;
        public string WeaponBundleId => weaponBundleId;
        public string ProjectileBundleId => projectileBundleId;
        public string BeamBundleId => beamBundleId;
        public float Damage => damage;
        public float KnockbackPower => knockbackPower;
        public PolarAreaType AreaType => areaType;
        public int DamageRadius => Mathf.Max(0, damageRadius);
        public bool UseGaussianFalloff => useGaussianFalloff;
        public float WoundIntensity => woundIntensity;
        public float TickRate => tickRate;
    }
}
