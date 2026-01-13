using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 불릿 무기 전용 데이터
    /// - 단발 투사체 타입
    /// - 중속, 중간 데미지
    /// - Linear 감쇠 범위 피해
    /// </summary>
    [CreateAssetMenu(fileName = "BulletWeaponData", menuName = "EarthDefense/Polar/BulletWeaponData", order = 4)]
    public class PolarBulletWeaponData : PolarWeaponData
    {
        [Header("Bullet Projectile")]
        [Tooltip("투사체 색상")]
        [SerializeField] private Color bulletColor = new Color(1f, 0.8f, 0.2f); // 황금색
        
        [Tooltip("투사체 크기")]
        [SerializeField] [Range(0.05f, 0.3f)] private float bulletScale = 0.15f;
        
        [Tooltip("투사체 속도 (units/s)")]
        [SerializeField] [Range(5f, 15f)] private float bulletSpeed = 10f;
        
        [Header("Visual Effects")]
        [Tooltip("발사 이펙트 프리팹 (선택)")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        
        [Tooltip("충돌 이펙트 프리팹 (선택)")]
        [SerializeField] private GameObject impactEffectPrefab;
        
        [Header("Audio")]
        [Tooltip("발사 사운드 ID")]
        [SerializeField] private string fireSoundId = "weapon_bullet_fire";
        
        [Tooltip("충돌 사운드 ID")]
        [SerializeField] private string impactSoundId = "weapon_bullet_impact";

        public Color BulletColor => bulletColor;
        public float BulletScale => bulletScale;
        public float BulletSpeed => bulletSpeed;
        public GameObject MuzzleFlashPrefab => muzzleFlashPrefab;
        public GameObject ImpactEffectPrefab => impactEffectPrefab;
        public string FireSoundId => fireSoundId;
        public string ImpactSoundId => impactSoundId;

        public override string ToJson(bool prettyPrint = true)
        {
            var data = new BulletWeaponDataJson
            {
                // Base
                baseData = base.ToJson(false),
                // Bullet Specific
                bulletColor = new[] { this.bulletColor.r, this.bulletColor.g, this.bulletColor.b, this.bulletColor.a },
                bulletScale = this.bulletScale,
                bulletSpeed = this.bulletSpeed,
                fireSoundId = this.fireSoundId,
                impactSoundId = this.impactSoundId
            };

            return JsonUtility.ToJson(data, prettyPrint);
        }

        public override void FromJson(string json)
        {
            var data = JsonUtility.FromJson<BulletWeaponDataJson>(json);

            // Base - baseData가 있으면 중첩 구조, 없으면 평면 구조
            if (!string.IsNullOrEmpty(data.baseData))
            {
                base.FromJson(data.baseData);
            }
            else
            {
                base.FromJson(json);
            }

            // Bullet Specific (값이 있으면 적용)
            if (data.bulletColor != null && data.bulletColor.Length == 4)
            {
                this.bulletColor = new Color(data.bulletColor[0], data.bulletColor[1], data.bulletColor[2], data.bulletColor[3]);
            }
            if (data.bulletScale > 0) this.bulletScale = data.bulletScale;
            if (data.bulletSpeed > 0) this.bulletSpeed = data.bulletSpeed;
            if (!string.IsNullOrEmpty(data.fireSoundId)) this.fireSoundId = data.fireSoundId;
            if (!string.IsNullOrEmpty(data.impactSoundId)) this.impactSoundId = data.impactSoundId;
        }

        [System.Serializable]
        private class BulletWeaponDataJson
        {
            public string baseData;
            public float[] bulletColor;
            public float bulletScale;
            public float bulletSpeed;
            public string fireSoundId;
            public string impactSoundId;
        }
    }
}

