using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 공통 무기 옵션 프로필 (여러 무기가 공유하는 기본 스탯)
    /// </summary>
    [CreateAssetMenu(fileName = "PolarWeaponOptionProfile", menuName = "EarthDefense/Polar/Weapon Option Profile", order = 50)]
    public class PolarWeaponOptionProfile : ScriptableObject
    {
        [Header("ID")]
        [SerializeField] private string id;

        [Header("Combat")]
        [SerializeField] private float damage = 5f;
        [SerializeField] private float knockbackPower = 0.2f;
        [SerializeField] private PolarAreaType areaType = PolarAreaType.Fixed;
        [SerializeField, Min(0)] private int damageRadius = 0;
        [SerializeField] private bool useGaussianFalloff = true;
        [SerializeField, Range(0f, 1f)] private float woundIntensity = 0.2f;
        [Tooltip("초당 타격 횟수 (레이저/틱 무기) ")]
        [SerializeField, Min(0.1f)] private float tickRate = 10f;

        public string Id => id;
        public float Damage => damage;
        public float KnockbackPower => knockbackPower;
        public PolarAreaType AreaType => areaType;
        public int DamageRadius => Mathf.Max(0, damageRadius);
        public bool UseGaussianFalloff => useGaussianFalloff;
        public float WoundIntensity => woundIntensity;
        public float TickRate => tickRate;

        /// <summary>
        /// JSON으로 내보내기 (직렬화)
        /// </summary>
        public string ToJson(bool prettyPrint = true)
        {
            var data = new WeaponOptionProfileJson
            {
                id = this.id,
                damage = this.damage,
                knockbackPower = this.knockbackPower,
                areaType = this.areaType.ToString(),
                damageRadius = this.damageRadius,
                useGaussianFalloff = this.useGaussianFalloff,
                woundIntensity = this.woundIntensity,
                tickRate = this.tickRate
            };
            return JsonUtility.ToJson(data, prettyPrint);
        }

        /// <summary>
        /// JSON에서 데이터 가져오기 (역직렬화)
        /// </summary>
        public void FromJson(string json)
        {
            var data = JsonUtility.FromJson<WeaponOptionProfileJson>(json);
            this.id = data.id;
            this.damage = data.damage;
            this.knockbackPower = data.knockbackPower;
            this.areaType = System.Enum.TryParse<PolarAreaType>(data.areaType, out var parsed) ? parsed : PolarAreaType.Fixed;
            this.damageRadius = data.damageRadius;
            this.useGaussianFalloff = data.useGaussianFalloff;
            this.woundIntensity = data.woundIntensity;
            this.tickRate = data.tickRate;
        }

        /// <summary>
        /// JSON 직렬화용 데이터 클래스
        /// </summary>
        [System.Serializable]
        private class WeaponOptionProfileJson
        {
            public string id;
            public float damage;
            public float knockbackPower;
            public string areaType;
            public int damageRadius;
            public bool useGaussianFalloff;
            public float woundIntensity;
            public float tickRate;
        }
    }
}
