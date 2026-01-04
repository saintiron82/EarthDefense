using UnityEngine;

namespace ShapeDefense.Scripts.Data
{
    [CreateAssetMenu(fileName = "LaserWeaponData", menuName = "ShapeDefense/Data/Weapon Data/LaserWeaponData", order = 1)]
    public class LaserWeaponData : WeaponData
    {
        [Header("Laser Settings")]
        [SerializeField] private float laserDuration = 2f;
        [SerializeField] private float damageTickRate = 10f;
        [SerializeField] private float laserWidth = 0.1f;
        [SerializeField] private float laserExtendSpeed = 50f;
        [SerializeField] private float laserRetractSpeed = 70f;
        [SerializeField] private float beamMaxLength = 50f;
        [SerializeField, Range(1, 64)] private int sweepSteps = 12;
        [SerializeField, Min(0f)] private float sweepEpsilon = 0f;
        [SerializeField, Min(0f)] private float hitRadius = 0.07f;
        [SerializeField, Min(0f)] private float rehitCooldown = 0.05f;
        [SerializeField] private Color laserColor = Color.cyan;
        [SerializeField] private int beamMaxHits = 1;
        
        public float LaserDuration => laserDuration;
        public float DamageTickRate => damageTickRate;
        public float LaserWidth => laserWidth;
        public float LaserExtendSpeed => laserExtendSpeed;
        public float LaserRetractSpeed => laserRetractSpeed;
        public float BeamMaxLength => beamMaxLength;
        public int SweepSteps => sweepSteps;
        public float SweepEpsilon => sweepEpsilon;
        public float HitRadius => hitRadius;
        public float RehitCooldown => rehitCooldown;
        public Color LaserColor => laserColor;
        public int BeamMaxHits => beamMaxHits;
    }
}