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
    }
}

