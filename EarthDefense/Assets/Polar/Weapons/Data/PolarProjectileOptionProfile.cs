using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 투사체 공용 옵션 프로필 (속도/수명/크기/색상 공유)
    /// </summary>
    [CreateAssetMenu(fileName = "PolarProjectileOptionProfile", menuName = "EarthDefense/Polar/Projectile Option Profile", order = 51)]
    public class PolarProjectileOptionProfile : ScriptableObject
    {
        [Header("ID")]
        [SerializeField] private string id;

        [Header("Projectile")]
        [SerializeField, Min(1f)] private float speed = 10f;
        [SerializeField, Min(0.1f)] private float lifetime = 3f;
        [SerializeField, Min(0.1f)] private float scale = 0.5f;
        [SerializeField] private Color color = Color.white;

        public string Id => id;
        public float Speed => speed;
        public float Lifetime => lifetime;
        public float Scale => scale;
        public Color Color => color;

        /// <summary>
        /// JSON으로 내보내기 (직렬화)
        /// </summary>
        public string ToJson(bool prettyPrint = true)
        {
            var data = new ProjectileOptionProfileJson
            {
                id = this.id,
                speed = this.speed,
                lifetime = this.lifetime,
                scale = this.scale,
                color = new float[] { this.color.r, this.color.g, this.color.b, this.color.a }
            };
            return JsonUtility.ToJson(data, prettyPrint);
        }

        /// <summary>
        /// JSON에서 데이터 가져오기 (역직렬화)
        /// </summary>
        public void FromJson(string json)
        {
            var data = JsonUtility.FromJson<ProjectileOptionProfileJson>(json);
            this.id = data.id;
            this.speed = data.speed;
            this.lifetime = data.lifetime;
            this.scale = data.scale;
            if (data.color != null && data.color.Length >= 4)
            {
                this.color = new Color(data.color[0], data.color[1], data.color[2], data.color[3]);
            }
        }

        /// <summary>
        /// JSON 직렬화용 데이터 클래스
        /// </summary>
        [System.Serializable]
        private class ProjectileOptionProfileJson
        {
            public string id;
            public float speed;
            public float lifetime;
            public float scale;
            public float[] color;
        }
    }
}
