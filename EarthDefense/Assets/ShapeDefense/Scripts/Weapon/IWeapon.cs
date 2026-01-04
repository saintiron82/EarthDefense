using UnityEngine;

namespace ShapeDefense.Scripts.Weapon
{
    /// <summary>
    /// 무기 시스템 인터페이스 (향후 확장용)
    /// 
    /// 사용 예정:
    /// - SectorWeapon: 원형 섹터 방식 (현재 SectorSpawner)
    /// - LinearWeapon: 직선 발사 (미사일, 레이저)
    /// - SpiralWeapon: 나선형 발사
    /// - RandomWeapon: 랜덤 산개
    /// </summary>
    public interface IWeapon
    {
        /// <summary>
        /// 무기 초기화
        /// </summary>
        void Initialize(WeaponConfig config);

        /// <summary>
        /// 무기 발사
        /// </summary>
        void Fire(Vector3 direction);

        /// <summary>
        /// 연속 발사 시작
        /// </summary>
        void StartFire();

        /// <summary>
        /// 연속 발사 중지
        /// </summary>
        void StopFire();

        /// <summary>
        /// 무기 업그레이드
        /// </summary>
        void Upgrade(int level);
    }

    /// <summary>
    /// 투사체 인터페이스 (청크, 미사일, 레이저 등)
    /// </summary>
    public interface IProjectile
    {
        /// <summary>
        /// 투사체 초기화
        /// </summary>
        void Initialize(ProjectileConfig config);

        /// <summary>
        /// 투사체 발사
        /// </summary>
        void Launch(Vector3 position, Vector3 direction, float speed);

        /// <summary>
        /// 타겟 설정 (유도 미사일용)
        /// </summary>
        void SetTarget(Transform target);
    }

    /// <summary>
    /// 무기 설정 데이터 (향후 ScriptableObject로 확장)
    /// </summary>
    [System.Serializable]
    public class WeaponConfig
    {
        public string WeaponName;
        public WeaponType Type;
        public float FireRate;
        public float Damage;
        public float Range;
        public int MaxAmmo;
        public ProjectileConfig ProjectileConfig;
    }

    /// <summary>
    /// 투사체 설정 데이터
    /// </summary>
    [System.Serializable]
    public class ProjectileConfig
    {
        public string PrefabId;
        public float Speed;
        public float Lifetime;
        public float Radius;
        public int PenetrationCount;
        public bool IsHoming;
    }

    /// <summary>
    /// 무기 타입 (확장용)
    /// </summary>
    public enum WeaponType
    {
        Sector,     // 원형 섹터 (현재)
        Linear,     // 직선 발사
        Spiral,     // 나선형
        Random,     // 랜덤 산개
        Homing,     // 유도 미사일
        Laser,      // 레이저 빔
        Shield,     // 방어막
        Drone       // 드론 소환
    }
}

