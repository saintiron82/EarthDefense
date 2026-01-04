using UnityEngine;

namespace ShapeDefense.Scripts.Data
{
    /// <summary>
    /// 무기 데이터 - 무기의 모든 스펙 정의
    /// BaseWeapon은 이 데이터를 주입받아 사용
    /// ScriptableObject로 에셋 파일로 생성하여 관리
    /// </summary>
    [CreateAssetMenu(fileName = "New_WeaponData", menuName = "ShapeDefense/Data/Weapon Data/Bullet", order = 0)]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string id = "";
        [SerializeField] private string weaponName = "New Weapon";
        [SerializeField, TextArea(2, 4)] private string description = "";
        [SerializeField] private Sprite icon;
        
        [Header("Weapon Bundle")]
        [Tooltip("무기 프리팹 번들 ID (Arm + Weapon 구조) - Resources/Weapons/")]
        [SerializeField] private string weaponBundleId = "";
        
        [Header("Fire Settings")]
        [Tooltip("연사 속도 (발/초)")]
        [SerializeField] private float fireRate = 10f;
        
        [Tooltip("발사 모드 (Manual, Automatic)")]
        [SerializeField] private FireMode fireMode = FireMode.Manual;
        
        [Header("Projectile Specs")]
        [Tooltip("발사체 번들 ID - Resources/Projectiles/")]
        [SerializeField] private string projectileBundleId = "";
        
        [Tooltip("발사체 데미지")]
        [SerializeField] private float projectileDamage = 10f;
        
        [Tooltip("발사체 속도")]
        [SerializeField] private float projectileSpeed = 20f;
        
        [Tooltip("발사체 수명 (사거리)")]
        [SerializeField] private float projectileLifetime = 3f;
        
        [Tooltip("발사체 관통 횟수")]
        [SerializeField] private int projectileMaxHits = 1;
        
        [Header("Level Info")]
        [Tooltip("해금 레벨 (0 = 처음부터 사용 가능)")]
        [SerializeField] private int unlockLevel;
        
        [Tooltip("획득 비용")]
        [SerializeField] private int unlockCost;
        
        // Properties - Basic
        public string Id => id;
        public string WeaponName => weaponName;
        public string Description => description;
        public Sprite Icon => icon;
        
        // Properties - Weapon Bundle
        public string WeaponBundleId => weaponBundleId;
        
        // Properties - Fire Settings
        public float FireRate => fireRate;
        public FireMode FireMode => fireMode;
        
        // Properties - Projectile
        public string ProjectileBundleId => projectileBundleId;
        public float ProjectileDamage => projectileDamage;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public int ProjectileMaxHits => projectileMaxHits;
        
        // Properties - Level
        public int UnlockLevel => unlockLevel;
        public int UnlockCost => unlockCost;
    }
    
    /// <summary>
    /// 발사 모드
    /// </summary>
    public enum FireMode
    {
        Manual,      // 수동: 클릭할 때마다 1발
        Automatic    // 자동: 버튼을 누르고 있으면 연속 발사
    }
}


