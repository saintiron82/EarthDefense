using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Polar.Weapons;

namespace Polar.Field
{
    /// <summary>
    /// Phase 1 - Step 1: 극좌표 필드 시뮬레이션의 핵심 컨트롤러
    /// 180개 섹터의 반지름 데이터를 관리하고 중력 시뮬레이션을 수행합니다.
    /// </summary>
    public class PolarFieldController : MonoBehaviour, IPolarField
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
        [SerializeField] private bool showGizmos = true;
        [SerializeField, Tooltip("섹터 HP(저항력) 기즈모 표기")]
        private bool showResistanceGizmos = true;
        [SerializeField, Tooltip("피해 적용 로그 출력(개발 중 확인용)")]
        private bool logDamageEvents = false;
        [SerializeField, Tooltip("최근 피해 지점만 표시")]
        private bool showDamageGizmos = true;
        [SerializeField, Tooltip("최근 피해 지점 유지 시간(초), 0 이하는 영구 표시")]
        private float damageGizmoDuration = 0f;
        [SerializeField] private Color damageGizmoColor = Color.red;
        [SerializeField] private float damageGizmoSize = 0.12f;

        // 180개 섹터의 반지름 데이터
        private float[] _sectorRadii;

        // Phase 2: 저항력 시스템
        private float[] _sectorResistances;  // 각 섹터의 저항력 (HP)

        // Phase 2: 넉백 시스템
        private float[] _knockbackCooldowns;  // 각 섹터의 넉백 쿨다운 (초)
        private float _lastWeaponKnockback = 1f;  // 마지막 무기 넉백 파워 (임시 저장)

        // 상처 시스템 (Wound & Recovery Lag)
        private float[] _recoveryScales;    // 각 섹터의 회복 속도 배율 (0.0 ~ 1.0)
        private float[] _woundCooldowns;    // 상처 쿨다운 (초)
        private float[] _lastDamageTimes;   // 최근 피해 시각 (초)

        // 게임 상태
        private bool _isGameOver;
        private float _stageTimer;
        private float _debugLogTimer;

        // 이벤트
        public event Action OnGameOver;
        public event Action<int> OnStageChanged;
        public event Action<int, float> OnSectorReachedEarth; // (sectorIndex, radius)
        public event Action<int> OnLineBreak;  // Phase 2: 라인 붕괴 이벤트 (sectorIndex)
        public event Action<int, float> OnKnockbackExecuted;  // Phase 2: 넉백 실행 이벤트 (sectorIndex, distance)

        // 공개 프로퍼티
        public int SectorCount => config != null ? config.SectorCount : 180;
        public float EarthRadius => config != null ? config.EarthRadius : 0.5f;
        public float InitialRadius => config != null ? config.InitialRadius : 5.0f; // 추가
        public bool EnableWoundSystem => config != null && config.EnableWoundSystem;
        public Vector3 CenterPosition => transform.position;
        public bool IsGameOver => _isGameOver;
        public int CurrentStage => currentStage;
        public float CurrentGravity => currentGravity;
        public PolarDataConfig Config => config; // ✅ Config 접근 추가

        private void Awake()
        {
            InitializeSectors();
            OnLineBreak += HandleLineBreak;  // Phase 2: 라인 붕괴 핸들러 연결
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

            // 1. 상처 회복 업데이트
            UpdateWoundRecovery(deltaTime);

            // 1.5. Phase 2: 저항력 회복 업데이트
            UpdateResistanceRegeneration(deltaTime);

            // 1.6. Phase 2: 넉백 쿨다운 업데이트
            UpdateKnockbackCooldowns(deltaTime);

            // 2. 중력 시뮬레이션: 모든 섹터의 반지름 감소
            ApplyGravity(deltaTime);

            // 3. 게임 오버 체크
            CheckGameOver();

            // 4. 스테이지 전환 체크
            UpdateStage(deltaTime);

            // 5. 디버그 로그
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
                _sectorResistances = new float[180];
                _knockbackCooldowns = new float[180];
                _recoveryScales = new float[180];
                _woundCooldowns = new float[180];
                _lastDamageTimes = new float[180];
                
                for (int i = 0; i < 180; i++)
                {
                    _sectorRadii[i] = 5.0f;
                    _sectorResistances[i] = 100f;  // Phase 2: 기본 저항력
                    _knockbackCooldowns[i] = 0f;  // 기본 넉백 쿨다운
                    _recoveryScales[i] = 1.0f;  // 정상 상태
                    _woundCooldowns[i] = 0f;
                    _lastDamageTimes[i] = -999f;
                }
                return;
            }

            int count = config.SectorCount;
            _sectorRadii = new float[count];
            _sectorResistances = new float[count];
            _knockbackCooldowns = new float[count];
            _recoveryScales = new float[count];
            _woundCooldowns = new float[count];
            _lastDamageTimes = new float[count];
            
            float initialRadius = config.InitialRadius;

            for (int i = 0; i < count; i++)
            {
                _sectorRadii[i] = initialRadius;
                _sectorResistances[i] = config.BaseResistance;  // Phase 2: 저항력 초기화
                _knockbackCooldowns[i] = 0f;  // Phase 2: 넉백 쿨다운 초기화
                _recoveryScales[i] = 1.0f;  // 정상 상태
                _woundCooldowns[i] = 0f;
                _lastDamageTimes[i] = -999f;
            }
            
            if (enableDebugLogs && config != null)
            {
                Debug.Log($"[PolarFieldController] Resistances initialized: {count} sectors @ {config.BaseResistance} HP");
            }
        }

        /// <summary>
        /// 중력 시뮬레이션: 매 프레임 반지름 감소 + 탄성 복원 + 평smooth화 + 전역 복원
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

                // 2. ✅ 평smooth화 보정 (HTML: applySmoothing 재현 + 맥동 연동)
                // 인접 섹터와의 거리를 비교해 너무 튀어나온 부분은 깎고, 들어간 부분은 메꿈
                int prev = (i - 1 + _sectorRadii.Length) % _sectorRadii.Length;
                int next = (i + 1) % _sectorRadii.Length;

                // 좌우 평활화 (선택적, 조절 가능)
                if (config != null && config.EnableNeighborSmoothing && config.NeighborSmoothingStrength > 0f)
                {
                    // 주변 값의 평균으로 수렵하려는 성질
                    // 가중치: current + 이웃 (HTML 기본: 90% + 5% + 5%)
                    // ✅ 맥동에 따라 복원력 조절
                    // ✅ 상처 배율 적용
                    float recoveryScale = config.EnableWoundSystem ? _recoveryScales[i] : 1f;
                    float neighborWeight = config.NeighborSmoothingStrength * smoothingMultiplier * recoveryScale; // 맥동 + 상처 연동
                    
                    // 가중치 정규화 (합이 1.0 유지)
                    float totalWeight = 1f + neighborWeight * 2f;
                    float baseWeight = 1f / totalWeight;
                    neighborWeight /= totalWeight;
                    
                    newRadius = newRadius * baseWeight + (_sectorRadii[prev] + _sectorRadii[next]) * neighborWeight;
                }
                
                // 3. ✅ 전역 복원력 (원형으로 수렴) - 선택적
                // 전체 평균으로 당기는 힘 (ON/OFF 가능)
                // ✅ 상처 배율 적용
                if (config != null && config.EnableGlobalRestoration && config.GlobalRestorationStrength > 0f)
                {
                    float recoveryScale = config.EnableWoundSystem ? _recoveryScales[i] : 1f;
                    float restorationForce = (averageRadius - newRadius) * config.GlobalRestorationStrength * recoveryScale;
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
                
                // Phase 2: 저항력 통계
                LogResistanceStats();
            }
        }

        /// <summary>
        /// Phase 2: 저항력 통계 로그
        /// </summary>
        private void LogResistanceStats()
        {
            if (_sectorResistances == null || config == null) return;

            float minResist = float.MaxValue;
            float maxResist = float.MinValue;
            float sumResist = 0f;

            for (int i = 0; i < _sectorResistances.Length; i++)
            {
                float r = _sectorResistances[i];
                if (r < minResist) minResist = r;
                if (r > maxResist) maxResist = r;
                sumResist += r;
            }

            float avgResist = sumResist / _sectorResistances.Length;
            Debug.Log($"[PolarFieldController] Resistance Stats: Min={minResist:F1}, Max={maxResist:F1}, Avg={avgResist:F1}");
        }

        /// <summary>
        /// Phase 2: 저항력 자동 회복 (선택적, 비율 기반)
        /// </summary>
        private void UpdateResistanceRegeneration(float deltaTime)
        {
            if (config == null || config.ResistanceRegenRate <= 0f) return;

            for (int i = 0; i < _sectorResistances.Length; i++)
            {
                if (_sectorResistances[i] < config.BaseResistance)
                {
                    // 비율 기반 회복: 초당 BaseResistance의 N% 회복
                    float regenAmount = config.BaseResistance * config.ResistanceRegenRate * deltaTime;
                    _sectorResistances[i] += regenAmount;
                    _sectorResistances[i] = Mathf.Min(_sectorResistances[i], config.BaseResistance);
                }
            }
        }

        /// <summary>
        /// Phase 2: 넉백 쿨다운 업데이트
        /// </summary>
        private void UpdateKnockbackCooldowns(float deltaTime)
        {
            if (_knockbackCooldowns == null) return;

            for (int i = 0; i < _knockbackCooldowns.Length; i++)
            {
                if (_knockbackCooldowns[i] > 0f)
                {
                    _knockbackCooldowns[i] -= deltaTime;
                }
            }
        }

        /// <summary>
        /// Phase 2: 무기의 넉백 파워 설정 (임시 저장)
        /// </summary>
        public void SetLastWeaponKnockback(float power)
        {
            _lastWeaponKnockback = power;
        }

        /// <summary>
        /// Phase 2: 넉백 실행
        /// </summary>
        public void ExecuteKnockback(int index, float power)
        {
            if (index < 0 || index >= _sectorRadii.Length) return;
            if (config == null) return;

            // 1. 쿨다운 체크
            if (_knockbackCooldowns != null && _knockbackCooldowns[index] > 0f)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"[PolarFieldController] Sector {index} in knockback cooldown");
                }
                return;
            }

            // 2. 넉백 거리 계산
            float distance = Mathf.Clamp(power, config.MinKnockback, config.MaxKnockback);

            // 3. 라인 밀어내기
            _sectorRadii[index] += distance;
            _sectorRadii[index] = Mathf.Min(_sectorRadii[index], config.InitialRadius);

            // 4. 저항력 리셋
            if (_sectorResistances != null)
            {
                _sectorResistances[index] = config.BaseResistance;
            }

            // 5. 쿨다운 설정
            if (_knockbackCooldowns != null)
            {
                _knockbackCooldowns[index] = config.KnockbackCooldown;
            }

            // 6. 이벤트 발동
            OnKnockbackExecuted?.Invoke(index, distance);

            if (enableDebugLogs)
            {
                Debug.Log($"[PolarFieldController] Knockback executed: Sector {index}, Distance {distance:F2}");
            }
        }

        /// <summary>
        /// Phase 2: 넉백 쿨다운 상태 조회
        /// </summary>
        public bool IsInKnockbackCooldown(int index)
        {
            if (index < 0 || index >= _sectorRadii.Length) return false;
            if (_knockbackCooldowns == null) return false;
            return _knockbackCooldowns[index] > 0f;
        }

        /// <summary>
        /// Phase 2: 라인 붕괴 핸들러
        /// </summary>
        private void HandleLineBreak(int index)
        {
            ExecuteKnockback(index, _lastWeaponKnockback);
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
        /// Phase 2: 특정 섹터의 저항력 조회
        /// </summary>
        public float GetSectorResistance(int index)
        {
            if (index < 0 || index >= _sectorResistances.Length)
            {
                return 0f;
            }
            return _sectorResistances[index];
        }

        /// <summary>
        /// Phase 2: 특정 섹터에 피해 적용
        /// </summary>
        public void ApplyDamageToSector(int index, float damage)
        {
            if (index < 0 || index >= _sectorResistances.Length)
            {
                Debug.LogWarning($"[PolarFieldController] Invalid sector index: {index}");
                return;
            }
            
            if (_isGameOver) return;
            
            float before = _sectorResistances[index];

            // 피해 배율 적용
            float actualDamage = damage * (config != null ? config.ResistanceDamageMultiplier : 1f);
            _sectorResistances[index] -= actualDamage;
            
            // 음수 방지
            _sectorResistances[index] = Mathf.Max(0f, _sectorResistances[index]);
            _lastDamageTimes[index] = Time.time;

            if (logDamageEvents)
            {
                Debug.Log($"[PolarFieldController] Damage sector {index} -{actualDamage:F2} (hp {before:F2} -> {_sectorResistances[index]:F2})");
            }
            
            // 라인 붕괴 체크
            if (_sectorResistances[index] <= 0f)
            {
                OnLineBreak?.Invoke(index);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[PolarFieldController] Line Break! Sector {index} resistance depleted");
                }
            }
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
        /// 특정 섹터의 반지름 증가 + 가우스 평smooth화 (Step 3: Smooth Push)
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
        
        #region Wound System

        /// <summary>
        /// 특정 섹터에 상처를 입힘 (Step 3: Wound System)
        /// </summary>
        /// <param name="sectorIndex">타격받은 섹터 인덱스</param>
        /// <param name="impactIntensity">충격 강도 (0~1, 1=최대)</param>
        public void ApplyWound(int sectorIndex, float impactIntensity)
        {
            if (sectorIndex < 0 || sectorIndex >= _sectorRadii.Length) return;
            if (config == null || !config.EnableWoundSystem) return;
            
            impactIntensity = Mathf.Clamp01(impactIntensity);
            
            // 중앙 섹터: 최대 충격
            ApplyWoundToSector(sectorIndex, impactIntensity);
            
            // 주변 섹터: 거리에 따라 감쇄 (Splash Wound)
            int splashRadius = config.WoundSplashRadius;
            for (int offset = 1; offset <= splashRadius; offset++)
            {
                // 거리에 따른 감쇄 (선형)
                float falloff = 1f - (float)offset / (splashRadius + 1);
                float splashIntensity = impactIntensity * falloff;
                
                int leftIndex = (sectorIndex - offset + _sectorRadii.Length) % _sectorRadii.Length;
                int rightIndex = (sectorIndex + offset) % _sectorRadii.Length;
                
                ApplyWoundToSector(leftIndex, splashIntensity);
                ApplyWoundToSector(rightIndex, splashIntensity);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[PolarFieldController] Wound applied at sector {sectorIndex}, intensity={impactIntensity:F2}");
            }
        }
        
        /// <summary>
        /// 단일 섹터에 상처 적용
        /// </summary>
        private void ApplyWoundToSector(int index, float intensity)
        {
            if (config == null) return;

            // 회복 배율 감소 (최소값: config.WoundMinRecoveryScale)
            float targetScale = Mathf.Lerp(1f, config.WoundMinRecoveryScale, intensity);
            _recoveryScales[index] = Mathf.Min(_recoveryScales[index], targetScale);
            
            // 쿨다운 리셋 (기존 쿨다운보다 긴 경우만)
            float newCooldown = config.WoundRecoveryDelay * intensity;
            _woundCooldowns[index] = Mathf.Max(_woundCooldowns[index], newCooldown);
        }
        
        /// <summary>
        /// 특정 섹터의 상처 강도 조회 (0=정상, 1=심한 상처)
        /// </summary>
        public float GetWoundIntensity(int index)
        {
            if (index < 0 || index >= _sectorRadii.Length) return 0f;
            return 1f - _recoveryScales[index];  // 0.1 (정상) → 0.9 (상처)
        }
        
        /// <summary>
        /// 특정 섹터의 회복 배율 조회 (0=완전 정지, 1=정상)
        /// </summary>
        public float GetRecoveryScale(int index)
        {
            if (index < 0 || index >= _sectorRadii.Length) return 1f;
            return _recoveryScales[index];
        }

        /// <summary>
        /// 상처 회복 업데이트 (매 프레임)
        /// </summary>
        private void UpdateWoundRecovery(float deltaTime)
        {
            if (config == null || !config.EnableWoundSystem) return;

            for (int i = 0; i < _sectorRadii.Length; i++)
            {
                // 쿨다운 감소
                if (_woundCooldowns[i] > 0f)
                {
                    _woundCooldowns[i] -= deltaTime;
                }
                else
                {
                    // 쿨다운 종료 후 점진적 회복 (1.0으로 수렴)
                    if (_recoveryScales[i] < 1f)
                    {
                        _recoveryScales[i] = Mathf.MoveTowards(
                            _recoveryScales[i], 
                            1f, 
                            config.WoundRecoverySpeed * deltaTime
                        );
                    }
                }
            }
        }

        #endregion

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            // 에디터 모드에서 아직 초기화 전이면 프리뷰용 기본값 세팅
            if (_sectorRadii == null || _sectorRadii.Length == 0)
            {
                if (config == null) return;
                int count = config.SectorCount;
                _sectorRadii = new float[count];
                _sectorResistances = new float[count];
                for (int i = 0; i < count; i++)
                {
                    _sectorRadii[i] = config.InitialRadius;
                    _sectorResistances[i] = config.BaseResistance;
                }
            }
 
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

            // Phase 2: 넉백 쿨다운 시각화 (노란색 구)
            if (Application.isPlaying && _knockbackCooldowns != null)
            {
                for (int i = 0; i < _knockbackCooldowns.Length; i++)
                {
                    if (_knockbackCooldowns[i] > 0f)
                    {
                        float angle = SectorIndexToAngle(i) * Mathf.Deg2Rad;
                        float radius = _sectorRadii[i];
                        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                        Vector3 pos = center + (Vector3)(dir * radius);

                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(pos, 0.1f);
                    }
                }
            }

#if UNITY_EDITOR
            if (Application.isPlaying && showResistanceGizmos && _sectorResistances != null && config != null)
            {
                DrawResistanceLabels(center);
            }

            if (Application.isPlaying && showDamageGizmos && _lastDamageTimes != null)
            {
                DrawDamageHits(center);
            }
#endif
        }

#if UNITY_EDITOR
        private void DrawResistanceLabels(Vector3 center)
        {
            float maxResist = config != null ? config.BaseResistance : 1f;
            float now = Application.isPlaying ? Time.time : float.PositiveInfinity;
            for (int i = 0; i < _sectorRadii.Length; i++)
            {
                bool hasDamage = _lastDamageTimes != null && _lastDamageTimes[i] >= 0f;
                if (Application.isPlaying && _lastDamageTimes != null && damageGizmoDuration > 0f)
                {
                    hasDamage &= now - _lastDamageTimes[i] <= damageGizmoDuration;
                }
                if (Application.isPlaying && !hasDamage)
                {
                    continue; // 피해가 발생한 섹터만 표시 (영구 or 기간 내)
                }

                float angle = SectorIndexToAngle(i) * Mathf.Deg2Rad;
                float radius = _sectorRadii[i];
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 pos = center + (Vector3)(dir * radius);
 
                float hp = _sectorResistances != null ? _sectorResistances[i] : 0f;
                float t = maxResist > 0f ? Mathf.Clamp01(hp / maxResist) : 0f;
                Color c = Color.Lerp(Color.red, Color.green, t);
                Handles.color = c;
                Handles.Label(pos, hp.ToString("F0"));
            }
        }

        private void DrawDamageHits(Vector3 center)
        {
            float now = Time.time;
            for (int i = 0; i < _lastDamageTimes.Length; i++)
            {
                if (_lastDamageTimes[i] < 0f) continue;
                if (damageGizmoDuration > 0f && now - _lastDamageTimes[i] > damageGizmoDuration) continue;

                float angle = SectorIndexToAngle(i) * Mathf.Deg2Rad;
                float radius = _sectorRadii[i];
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 pos = center + (Vector3)(dir * radius);
                Handles.color = damageGizmoColor;
                Handles.DrawWireDisc(pos, Vector3.forward, damageGizmoSize);
            }
        }
#endif
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
