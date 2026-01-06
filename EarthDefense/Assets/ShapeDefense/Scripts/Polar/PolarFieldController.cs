using System;
using UnityEngine;

namespace ShapeDefense.Scripts.Polar
{
    /// <summary>
    /// Phase 1 - Step 1: 극좌표 필드 시뮬레이션의 핵심 컨트롤러
    /// 180개 섹터의 반지름 데이터를 관리하고 중력 시뮬레이션을 수행합니다.
    /// </summary>
    public class PolarFieldController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PolarDataConfig config;

        [Header("Runtime State")]
        [SerializeField, Tooltip("현재 게임 스테이지 (중력 배율 계산용)")]
        private int currentStage = 1;

        [SerializeField, Tooltip("현재 중력 가속도")]
        private float currentGravity = 1.0f;

        [Header("Debug Visualization")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showGizmos = false;

        // 180개 섹터의 반지름 데이터
        private float[] _sectorRadii;

        // 게임 상태
        private bool _isGameOver;
        private float _stageTimer;
        private float _debugLogTimer;

        // 이벤트
        public event Action OnGameOver;
        public event Action<int> OnStageChanged;
        public event Action<int, float> OnSectorReachedEarth; // (sectorIndex, radius)

        // 공개 프로퍼티
        public int SectorCount => config != null ? config.SectorCount : 180;
        public float EarthRadius => config != null ? config.EarthRadius : 0.5f;
        public float InitialRadius => config != null ? config.InitialRadius : 5.0f; // 추가
        public bool IsGameOver => _isGameOver;
        public int CurrentStage => currentStage;
        public float CurrentGravity => currentGravity;
        public PolarDataConfig Config => config; // ✅ Config 접근 추가

        private void Awake()
        {
            InitializeSectors();
        }

        private void Start()
        {
            if (config == null)
            {
                Debug.LogError("[PolarFieldController] Config is null! Please assign PolarDataConfig.");
                enabled = false;
                return;
            }

            currentGravity = config.BaseGravityAccel;
            _stageTimer = 0f;
            _debugLogTimer = 0f;
            _isGameOver = false;

            if (enableDebugLogs)
            {
                Debug.Log($"[PolarFieldController] Initialized with {SectorCount} sectors, InitialRadius={config.InitialRadius}, EarthRadius={EarthRadius}");
            }
        }

        private void Update()
        {
            if (_isGameOver) return;

            float deltaTime = Time.deltaTime;

            // 1. 중력 시뮬레이션: 모든 섹터의 반지름 감소
            ApplyGravity(deltaTime);

            // 2. 게임 오버 체크
            CheckGameOver();

            // 3. 스테이지 전환 체크
            UpdateStage(deltaTime);

            // 4. 디버그 로그
            UpdateDebugLog(deltaTime);
        }

        /// <summary>
        /// 섹터 배열 초기화
        /// </summary>
        private void InitializeSectors()
        {
            if (config == null)
            {
                Debug.LogWarning("[PolarFieldController] Config not assigned during Awake. Using default values.");
                _sectorRadii = new float[180];
                for (int i = 0; i < 180; i++)
                {
                    _sectorRadii[i] = 5.0f;
                }
                return;
            }

            int count = config.SectorCount;
            _sectorRadii = new float[count];
            float initialRadius = config.InitialRadius;

            for (int i = 0; i < count; i++)
            {
                _sectorRadii[i] = initialRadius;
            }
        }

        /// <summary>
        /// 중력 시뮬레이션: 매 프레임 반지름 감소 + 탄성 복원 + 평활화 + 전역 복원
        /// HTML applySmoothing() 재현 + 맥동 연동 + 원형 복원
        /// </summary>
        private void ApplyGravity(float deltaTime)
        {
            float reductionAmount = currentGravity * deltaTime;
            
            // ✅ 전체 평균 계산 (목표 반지름)
            float averageRadius = 0f;
            for (int i = 0; i < _sectorRadii.Length; i++)
            {
                averageRadius += _sectorRadii[i];
            }
            averageRadius /= _sectorRadii.Length;
            
            // ✅ 맥동 시간 동기화 (PolarBoundaryRenderer와 공유)
            // 맥동이 수축할 때 복원력 강화, 팽창할 때 약화
            float pulsationPhase = 0f;
            float smoothingMultiplier = 1f;
            
            if (config != null && config.EnablePulsation)
            {
                // 사인파: -1 ~ +1
                pulsationPhase = Mathf.Sin(Time.time * config.PulsationFrequency * Mathf.PI * 2f);
                
                // 수축 시(음수) 복원력 강화, 팽창 시(양수) 복원력 약화
                // 범위: 0.7 ~ 1.3
                smoothingMultiplier = 1f - pulsationPhase * 0.3f;
            }
            
            // HTML의 applySmoothing() 로직을 Unity식으로 이식
            // 밀려난 부분이 옆 섹터들을 끌어당기며 부드럽게 원형을 회복하게 함
            float[] nextRadii = new float[_sectorRadii.Length];

            for (int i = 0; i < _sectorRadii.Length; i++)
            {
                // 1. 기본 중력 수축 (HTML: wallRadius[i] -= currentGravity * dt)
                float newRadius = _sectorRadii[i] - reductionAmount;

                // 2. ✅ 평활화 보정 (HTML: applySmoothing 재현 + 맥동 연동)
                // 인접 섹터와의 거리를 비교해 너무 튀어나온 부분은 깎고, 들어간 부분은 메꿈
                int prev = (i - 1 + _sectorRadii.Length) % _sectorRadii.Length;
                int next = (i + 1) % _sectorRadii.Length;
                
                // 주변 값의 평균으로 수렴하려는 성질
                // 가중치: 현재 90%, 이웃 각 5% (HTML과 동일)
                // ✅ 맥동에 따라 복원력 조절
                float baseWeight = 0.9f;
                float neighborWeight = 0.05f * smoothingMultiplier; // 맥동 연동
                
                // 가중치 정규화 (합이 1.0 유지)
                float totalWeight = baseWeight + neighborWeight * 2f;
                baseWeight /= totalWeight;
                neighborWeight /= totalWeight;
                
                newRadius = newRadius * baseWeight + (_sectorRadii[prev] + _sectorRadii[next]) * neighborWeight;
                
                // 3. ✅ 전역 복원력 (원형으로 수렴)
                // 전체 평균으로 당기는 힘
                if (config != null && config.GlobalRestorationStrength > 0f)
                {
                    float restorationForce = (averageRadius - newRadius) * config.GlobalRestorationStrength;
                    newRadius += restorationForce;
                }

                nextRadii[i] = Mathf.Max(newRadius, EarthRadius);
            }

            // 한 번에 적용 (동시 업데이트)
            _sectorRadii = nextRadii;
        }

        /// <summary>
        /// 게임 오버 체크: 임의의 섹터가 EARTH_RADIUS에 도달했는지 확인
        /// </summary>
        private void CheckGameOver()
        {
            for (int i = 0; i < _sectorRadii.Length; i++)
            {
                if (_sectorRadii[i] <= EarthRadius + 0.001f) // epsilon 허용
                {
                    TriggerGameOver(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 게임 오버 트리거
        /// </summary>
        private void TriggerGameOver(int sectorIndex)
        {
            if (_isGameOver) return;

            _isGameOver = true;

            if (enableDebugLogs)
            {
                Debug.LogWarning($"[PolarFieldController] GAME OVER! Sector {sectorIndex} reached Earth (radius={_sectorRadii[sectorIndex]:F3})");
            }

            OnSectorReachedEarth?.Invoke(sectorIndex, _sectorRadii[sectorIndex]);
            OnGameOver?.Invoke();
        }

        /// <summary>
        /// 스테이지 전환: 시간 경과에 따라 중력 가속도 증가
        /// </summary>
        private void UpdateStage(float deltaTime)
        {
            if (config == null) return;

            _stageTimer += deltaTime;

            if (_stageTimer >= config.StageTransitionTime)
            {
                _stageTimer = 0f;
                currentStage++;
                currentGravity = config.BaseGravityAccel * Mathf.Pow(config.GravityMultiplierPerStage, currentStage - 1);

                if (enableDebugLogs)
                {
                    Debug.Log($"[PolarFieldController] Stage {currentStage} started! Gravity: {currentGravity:F2}x");
                }

                OnStageChanged?.Invoke(currentStage);
            }
        }

        /// <summary>
        /// 디버그 로그: 주기적으로 섹터 상태 출력
        /// </summary>
        private void UpdateDebugLog(float deltaTime)
        {
            if (!enableDebugLogs || config == null || config.DebugLogInterval <= 0f) return;

            _debugLogTimer += deltaTime;

            if (_debugLogTimer >= config.DebugLogInterval)
            {
                _debugLogTimer = 0f;

                float minRadius = float.MaxValue;
                float maxRadius = float.MinValue;
                float avgRadius = 0f;

                for (int i = 0; i < _sectorRadii.Length; i++)
                {
                    float r = _sectorRadii[i];
                    minRadius = Mathf.Min(minRadius, r);
                    maxRadius = Mathf.Max(maxRadius, r);
                    avgRadius += r;
                }
                avgRadius /= _sectorRadii.Length;

                Debug.Log($"[PolarFieldController] Stage={currentStage}, Gravity={currentGravity:F2}, " +
                          $"Radii: Min={minRadius:F3}, Max={maxRadius:F3}, Avg={avgRadius:F3}");
            }
        }

        #region Public API

        /// <summary>
        /// 특정 섹터의 반지름 조회
        /// </summary>
        public float GetSectorRadius(int index)
        {
            if (index < 0 || index >= _sectorRadii.Length)
            {
                Debug.LogWarning($"[PolarFieldController] Invalid sector index: {index}");
                return EarthRadius;
            }
            return _sectorRadii[index];
        }

        /// <summary>
        /// 특정 섹터의 반지름 증가 (Step 3: Push 기능용)
        /// </summary>
        public void PushSectorRadius(int index, float amount)
        {
            if (index < 0 || index >= _sectorRadii.Length) return;
            if (_isGameOver) return;

            _sectorRadii[index] += amount;
            _sectorRadii[index] = Mathf.Max(_sectorRadii[index], EarthRadius);
        }
        
        /// <summary>
        /// 특정 섹터의 반지름 증가 + 가우스 평활화 (Step 3: Smooth Push)
        /// </summary>
        public void PushSectorRadiusSmooth(int centerIndex, float amount)
        {
            if (centerIndex < 0 || centerIndex >= _sectorRadii.Length) return;
            if (_isGameOver) return;
            if (config == null) return;

            int radius = config.SmoothingRadius;
            float strength = config.SmoothingStrength;

            // 중앙 섹터: 전체 영향
            _sectorRadii[centerIndex] += amount;

            // 주변 섹터: 가우스 분포
            for (int offset = 1; offset <= radius; offset++)
            {
                // 가우스 곡선: exp(-x²/2σ²)
                float sigma = radius / 3f; // 3-sigma rule
                float gaussian = Mathf.Exp(-(offset * offset) / (2f * sigma * sigma));
                float influence = gaussian * strength;

                int leftIndex = (centerIndex - offset + _sectorRadii.Length) % _sectorRadii.Length;
                int rightIndex = (centerIndex + offset) % _sectorRadii.Length;

                _sectorRadii[leftIndex] += amount * influence;
                _sectorRadii[rightIndex] += amount * influence;
            }

            // 모든 섹터 범위 제한
            for (int i = 0; i < _sectorRadii.Length; i++)
            {
                _sectorRadii[i] = Mathf.Max(_sectorRadii[i], EarthRadius);
            }
        }

        /// <summary>
        /// 각도(Degree)를 섹터 인덱스로 변환
        /// </summary>
        public int AngleToSectorIndex(float angleDegree)
        {
            float normalized = Mathf.Repeat(angleDegree, 360f);
            int index = Mathf.FloorToInt(normalized / 360f * _sectorRadii.Length);
            return Mathf.Clamp(index, 0, _sectorRadii.Length - 1);
        }

        /// <summary>
        /// 섹터 인덱스를 각도(Degree)로 변환
        /// </summary>
        public float SectorIndexToAngle(int index)
        {
            return (index * 360f / _sectorRadii.Length) + (360f / _sectorRadii.Length * 0.5f);
        }

        /// <summary>
        /// 게임 리셋
        /// </summary>
        [ContextMenu("Reset Game")]
        public void ResetGame()
        {
            InitializeSectors();
            currentStage = 1;
            currentGravity = config != null ? config.BaseGravityAccel : 1.0f;
            _stageTimer = 0f;
            _debugLogTimer = 0f;
            _isGameOver = false;

            if (enableDebugLogs)
            {
                Debug.Log("[PolarFieldController] Game Reset!");
            }
        }

        #endregion

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!showGizmos || _sectorRadii == null || _sectorRadii.Length == 0) return;

            Vector3 center = transform.position;

            // Earth radius (red)
            Gizmos.color = Color.red;
            DrawCircle(center, EarthRadius, 64);

            // Sector radii (cyan with alpha based on distance from earth)
            for (int i = 0; i < _sectorRadii.Length; i++)
            {
                float angle = SectorIndexToAngle(i) * Mathf.Deg2Rad;
                float radius = _sectorRadii[i];

                float t = Mathf.InverseLerp(EarthRadius, config != null ? config.InitialRadius : 5f, radius);
                Gizmos.color = Color.Lerp(Color.red, Color.cyan, t);

                Vector3 point = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                Gizmos.DrawLine(center, point);
            }
        }

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }

        #endregion
    }
}
