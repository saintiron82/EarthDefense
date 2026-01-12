using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 투사체 공용 옵션 프로필 (속도/수명/크기/색상 공유)
    /// </summary>
    [CreateAssetMenu(fileName = "PolarProjectileOptionProfile", menuName = "EarthDefense/Polar/Projectile Option Profile", order = 51)]
    public class PolarProjectileOptionProfile : ScriptableObject
    {
        [SerializeField, Min(1f)] private float speed = 10f;
        [SerializeField, Min(0.1f)] private float lifetime = 3f;
        [SerializeField, Min(0.1f)] private float scale = 0.5f;
        [SerializeField] private Color color = Color.white;

        public float Speed => speed;
        public float Lifetime => lifetime;
        public float Scale => scale;
        public Color Color => color;
    }
}
