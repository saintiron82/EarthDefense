using UnityEngine;

namespace Polar.Field
{
    /// <summary>
    /// Phase 1 - Step 1: 극좌표 필드 시뮬레이션을 위한 설정 데이터
    /// 180개 섹터의 반지름 데이터와 중력 가속도를 정의합니다.
    /// Phase 2 - Step 1: 적(Enemy) 시스템 설정 추가 (저항력, 넉백)
    /// </summary>
    [CreateAssetMenu(fileName = "PolarDataConfig", menuName = "Polar/Config", order = 0)]
    public class PolarDataConfig : ScriptableObject
    {
        [Header("Sector Configuration")]
        [Tooltip("극좌표 섹터 개수 (고정값)")]
        [SerializeField] private int sectorCount = 180;

        [Header("Radius Settings")]
        [Tooltip("지구 반지름 - 이 값 이하로 침식되면 게임 오버 (Unity 월드 단위)")]
        [SerializeField] private float earthRadius = 0.5f;

        [Tooltip("초기 반지름 - 게임 시작 시 모든 섹터의 반지름")]
        [SerializeField] private float initialRadius = 5.0f;

        [Header("Gravity Simulation")]
        [Tooltip("기본 중력 가속도 (초당 반지름 감소량)")]
        [SerializeField] private float baseGravityAccel = 1.0f;

        [Tooltip("단계별 중력 배율 증가 (예: 1.5 = 1.5배씩 증가)")]
        [SerializeField] private float gravityMultiplierPerStage = 1.5f;

        [Tooltip("단계 전환 시간 (초)")]
        [SerializeField] private float stageTransitionTime = 30f;

        [Header("Organic Pulsation (맥동 효과)")]
        [Tooltip("맥동 활성화")]
        [SerializeField] private bool enablePulsation = true;
        
        [Tooltip("맥동 진폭 (반지름 변화량)")]
        [SerializeField, Range(0.01f, 1.0f)] private float pulsationAmplitude = 0.08f;
        
        [Tooltip("맥동 속도 (Hz)")]
        [SerializeField, Range(0.5f, 5f)] private float pulsationFrequency = 1.2f;
        
        [Tooltip("섹터별 위상차 (0=동시, 1=순차)")]
        [SerializeField, Range(0f, 1f)] private float phaseOffset = 0.3f;
        
        [Tooltip("노이즈 기반 불규칙 맥동")]
        [SerializeField] private bool usePerlinNoise = true;
        
        [Tooltip("노이즈 스케일")]
        [SerializeField, Range(0.1f, 5f)] private float noiseScale = 2.0f;

        [Header("Push & Carving (밀어내기)")]
        [Tooltip("마우스 클릭 시 반지름 증가량 (초당)")]
        [SerializeField, Range(0.1f, 10f)] private float pushPower = 2.0f;
        
        [Tooltip("영향을 받는 주변 섹터 범위 (섹터 개수)")]
        [SerializeField, Range(1, 30)] private int smoothingRadius = 8;
        
        [Tooltip("평활화 강도 (0=영향 없음, 1=완전 전파)")]
        [SerializeField, Range(0f, 1f)] private float smoothingStrength = 0.8f;
        
        [Header("Neighbor Smoothing (좌우 평활화)")]
        [Tooltip("좌우 평활화 활성화 (톱니 제거, 파동 전파)")]
        [SerializeField] private bool enableNeighborSmoothing = true;
        
        [Tooltip("이웃 섹터 영향력 (0=없음, 0.1=강함) - HTML 기본값: 0.05")]
        [SerializeField, Range(0f, 0.2f)] private float neighborSmoothingStrength = 0.05f;
        
        [Header("Global Restoration (전역 복원)")]
        [Tooltip("전역 복원 활성화 (원형으로 수렴)")]
        [SerializeField] private bool enableGlobalRestoration = true;
        
        [Tooltip("전역 복원력 (0=없음, 0.1=빠름) - 좌우 평활화를 보조하는 약한 힘")]
        [SerializeField, Range(0f, 0.1f)] private float globalRestorationStrength = 0.005f;

        [Header("Wound & Recovery Lag (상처 시스템)")]
        [Tooltip("상처 시스템 활성화")]
        [SerializeField] private bool enableWoundSystem = true;
        
        [Tooltip("상처 회복 지연 시간 (초) - 타격 후 회복 시작까지 대기")]
        [SerializeField, Range(0f, 10f)] private float woundRecoveryDelay = 2.0f;
        
        [Tooltip("상처 회복 속도 (0=없음, 1=즉시) - 정상 상태로 돌아오는 속도")]
        [SerializeField, Range(0.01f, 1f)] private float woundRecoverySpeed = 0.1f;
        
        [Tooltip("최소 회복 배율 (0=완전 정지, 1=정상) - 타격 시 최저치")]
        [SerializeField, Range(0f, 1f)] private float woundMinRecoveryScale = 0.1f;
        
        [Tooltip("상처 확산 반경 (섹터 개수) - 주변 섹터에 영향")]
        [SerializeField, Range(0, 20)] private int woundSplashRadius = 5;

        [Header("Projectile & Collision (투사체)")]
        [Tooltip("자동 발사 활성화")]
        [SerializeField] private bool enableAutoFire = true;
        
        [Tooltip("발사 간격 (초)")]
        [SerializeField, Range(0.1f, 5f)] private float fireRate = 0.5f;
        
        [Tooltip("총알 속도 (Unity 단위/초)")]
        [SerializeField, Range(1f, 50f)] private float bulletSpeed = 10f;
        
        [Tooltip("미사일 폭발력 (반지름 증가량)")]
        [SerializeField, Range(0.1f, 10f)] private float missileForce = 3.0f;
        
        [Tooltip("충돌 감지 정밀도 (작을수록 정밀)")]
        [SerializeField, Range(0.01f, 0.5f)] private float collisionEpsilon = 0.1f;
        
        [Tooltip("폭발 반경 (섹터 개수)")]
        [SerializeField, Range(1, 30)] private int explosionRadius = 10;
        
        [Tooltip("폭발 시 상처 강도 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float explosionWoundIntensity = 0.8f;

        [Header("Debug")]
        [Tooltip("디버그 로그 출력 간격 (초, 0 = 비활성화)")]
        [SerializeField] private float debugLogInterval = 5f;

        [Header("Phase 2: Enemy - Resistance System")]
        [Tooltip("각 섹터의 기본 저항력 (HP)")]
        [SerializeField, Range(10f, 1000f)] private float baseResistance = 100f;

        [Tooltip("저항력 자동 회복 속도 (초당 %, 0 = 비활성, 0.05 = 5%/초)")]
        [SerializeField, Range(0f, 1f)] private float resistanceRegenRate = 0f;

        [Tooltip("받는 피해 배율 (1.0 = 기본)")]
        [SerializeField, Range(0.1f, 5f)] private float resistanceDamageMultiplier = 1f;

        [Header("Phase 2: Enemy - Knockback System")]
        [Tooltip("최소 넉백 거리")]
        [SerializeField, Range(0.01f, 1f)] private float minKnockback = 0.1f;

        [Tooltip("최대 넉백 거리")]
        [SerializeField, Range(0.5f, 5f)] private float maxKnockback = 2f;

        [Tooltip("넉백 쿨다운 (초)")]
        [SerializeField, Range(0f, 5f)] private float knockbackCooldown = 0.5f;

        // 공개 프로퍼티
        public int SectorCount => sectorCount;
        public float EarthRadius => earthRadius;
        public float InitialRadius => initialRadius;
        public float BaseGravityAccel => baseGravityAccel;
        public float GravityMultiplierPerStage => gravityMultiplierPerStage;
        public float StageTransitionTime => stageTransitionTime;
        public float DebugLogInterval => debugLogInterval;
        
        // 맥동 프로퍼티
        public bool EnablePulsation => enablePulsation;
        public float PulsationAmplitude => pulsationAmplitude;
        public float PulsationFrequency => pulsationFrequency;
        public float PhaseOffset => phaseOffset;
        public bool UsePerlinNoise => usePerlinNoise;
        public float NoiseScale => noiseScale;
        
        // Push & Carving 프로퍼티
        public float PushPower => pushPower;
        public int SmoothingRadius => smoothingRadius;
        public float SmoothingStrength => smoothingStrength;
        
        // Neighbor Smoothing 프로퍼티
        public bool EnableNeighborSmoothing => enableNeighborSmoothing;
        public float NeighborSmoothingStrength => neighborSmoothingStrength;
        
        // Global Restoration 프로퍼티
        public bool EnableGlobalRestoration => enableGlobalRestoration;
        public float GlobalRestorationStrength => globalRestorationStrength;
        
        // Wound & Recovery Lag 프로퍼티
        public bool EnableWoundSystem => enableWoundSystem;
        public float WoundRecoveryDelay => woundRecoveryDelay;
        public float WoundRecoverySpeed => woundRecoverySpeed;
        public float WoundMinRecoveryScale => woundMinRecoveryScale;
        public int WoundSplashRadius => woundSplashRadius;
        
        // Projectile & Collision 프로퍼티
        public bool EnableAutoFire => enableAutoFire;
        public float FireRate => fireRate;
        public float BulletSpeed => bulletSpeed;
        public float MissileForce => missileForce;
        public float CollisionEpsilon => collisionEpsilon;
        public int ExplosionRadius => explosionRadius;
        public float ExplosionWoundIntensity => explosionWoundIntensity;

        // Phase 2: Resistance System Properties
        public float BaseResistance => baseResistance;
        public float ResistanceRegenRate => resistanceRegenRate;
        public float ResistanceDamageMultiplier => resistanceDamageMultiplier;

        // Phase 2: Knockback System Properties
        public float MinKnockback => minKnockback;
        public float MaxKnockback => maxKnockback;
        public float KnockbackCooldown => knockbackCooldown;

        private void OnValidate()
        {
            sectorCount = Mathf.Max(1, sectorCount);
            earthRadius = Mathf.Max(0.01f, earthRadius);
            initialRadius = Mathf.Max(earthRadius + 0.1f, initialRadius);
            baseGravityAccel = Mathf.Max(0f, baseGravityAccel);
            gravityMultiplierPerStage = Mathf.Max(1f, gravityMultiplierPerStage);
            stageTransitionTime = Mathf.Max(1f, stageTransitionTime);
            debugLogInterval = Mathf.Max(0f, debugLogInterval);
            
            // 맥동 검증
            pulsationAmplitude = Mathf.Clamp(pulsationAmplitude, 0.01f, 1.0f);
            pulsationFrequency = Mathf.Clamp(pulsationFrequency, 0.5f, 5f);
            phaseOffset = Mathf.Clamp01(phaseOffset);
            noiseScale = Mathf.Clamp(noiseScale, 0.1f, 5f);
            
            // Push & Carving 검증
            pushPower = Mathf.Clamp(pushPower, 0.1f, 10f);
            smoothingRadius = Mathf.Clamp(smoothingRadius, 1, 30);
            smoothingStrength = Mathf.Clamp01(smoothingStrength);
            
            // Neighbor Smoothing 검증
            neighborSmoothingStrength = Mathf.Clamp(neighborSmoothingStrength, 0f, 0.2f);
            
            // Global Restoration 검증
            globalRestorationStrength = Mathf.Clamp(globalRestorationStrength, 0f, 0.1f);
            
            // Wound & Recovery Lag 검증
            woundRecoveryDelay = Mathf.Clamp(woundRecoveryDelay, 0f, 10f);
            woundRecoverySpeed = Mathf.Clamp(woundRecoverySpeed, 0.01f, 1f);
            woundMinRecoveryScale = Mathf.Clamp01(woundMinRecoveryScale);
            woundSplashRadius = Mathf.Clamp(woundSplashRadius, 0, 20);
            
            // Projectile & Collision 검증
            fireRate = Mathf.Clamp(fireRate, 0.1f, 5f);
            bulletSpeed = Mathf.Clamp(bulletSpeed, 1f, 50f);
            missileForce = Mathf.Clamp(missileForce, 0.1f, 10f);
            collisionEpsilon = Mathf.Clamp(collisionEpsilon, 0.01f, 0.5f);
            explosionRadius = Mathf.Clamp(explosionRadius, 1, 30);
            explosionWoundIntensity = Mathf.Clamp01(explosionWoundIntensity);

            // Phase 2: Resistance System 검증
            baseResistance = Mathf.Clamp(baseResistance, 10f, 1000f);
            resistanceRegenRate = Mathf.Clamp(resistanceRegenRate, 0f, 1f);
            resistanceDamageMultiplier = Mathf.Clamp(resistanceDamageMultiplier, 0.1f, 5f);

            // Phase 2: Knockback System 검증
            minKnockback = Mathf.Clamp(minKnockback, 0.01f, 1f);
            maxKnockback = Mathf.Clamp(maxKnockback, 0.5f, 5f);
            knockbackCooldown = Mathf.Clamp(knockbackCooldown, 0f, 5f);
        }
    }
}
