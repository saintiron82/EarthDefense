using UnityEngine;

namespace ShapeDefense.Scripts.Polar
{
    /// <summary>
    /// Phase 1 - Step 1: 극좌표 필드 시뮬레이션을 위한 설정 데이터
    /// 180개 섹터의 반지름 데이터와 중력 가속도를 정의합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PolarDataConfig", menuName = "ShapeDefense/Polar/Config", order = 0)]
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
        
        [Tooltip("전역 복원력 (0=없음, 1=즉시 복원)")]
        [SerializeField, Range(0f, 0.1f)] private float globalRestorationStrength = 0.01f;

        [Header("Debug")]
        [Tooltip("디버그 로그 출력 간격 (초, 0 = 비활성화)")]
        [SerializeField] private float debugLogInterval = 5f;

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
        public float GlobalRestorationStrength => globalRestorationStrength;

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
            globalRestorationStrength = Mathf.Clamp(globalRestorationStrength, 0f, 0.1f);
        }
    }
}
