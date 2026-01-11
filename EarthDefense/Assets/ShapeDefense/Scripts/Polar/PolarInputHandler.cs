using Polar.Field;
using UnityEngine;
using UnityEngine.InputSystem;
using ShapeDefense.Scripts.Utilities;

namespace ShapeDefense.Scripts.Polar
{
    /// <summary>
    /// Phase 1 - Step 3: 마우스 입력을 극좌표로 변환하여 공간 밀어내기
    /// HTML 프로토타입의 마우스 인터랙션 재현
    /// Unity New Input System 사용
    /// </summary>
    [RequireComponent(typeof(PolarFieldController))]
    public class PolarInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PolarFieldController controller;
        [SerializeField] private Camera mainCamera;

        [Header("Input Settings")]
        [Tooltip("New Input System Actions Asset")]
        [SerializeField] private InputActionAsset inputActions;
        
        [Tooltip("Push Action Name (예: 'Player/Push')")]
        [SerializeField] private string pushActionName = "Player/Push";

        [Header("Laser Visualization")]
        [SerializeField] private LineRenderer laserBeam;
        [SerializeField] private bool createLaserIfMissing = true;
        [SerializeField] private Color laserColor = Color.green;
        [SerializeField] private float laserWidth = 0.15f;
        [SerializeField] private Material laserMaterial;

        [Header("Beam Properties")]
        [Tooltip("레이저 빔의 넓이 (시각화 및 충돌 예측용)")]
        [SerializeField] private float beamWidth = 0.1f;

        [Header("Collision Prediction")]
        [SerializeField] private bool enableCollisionPrediction = true;
        [SerializeField] private LayerMask collisionLayers = -1;
        [SerializeField] private float maxLaserRange = 100f;
        [SerializeField] private bool predictMovingTargets = true;
        [SerializeField] private bool showPredictionGizmos = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugRays = true;
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private Color predictionHitColor = Color.red;

        private bool _isPushing;
        
        // New Input System
        private InputAction _pushAction;
        private Mouse _mouse;
        
        // Debug
        private int _pushFrameCount;
        private float _lastLogTime;
        private float _pushStartTime; // 푸시 시작 시간

        // Collision Prediction
        private CollisionPredictor.CollisionResult? _lastPredictedHit;
        private Vector2 _lastPredictedEndPoint;
        private Vector2 _lastTargetWorldPos; // 실제 타격할 월드 포지션

        private void Awake()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[PolarInputHandler] Awake() called");
            }

            if (controller == null)
            {
                controller = GetComponent<PolarFieldController>();
                if (enableDebugLogs)
                {
                    Debug.Log($"[PolarInputHandler] Controller {(controller != null ? "found" : "NOT FOUND")}");
                }
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (enableDebugLogs)
                {
                    Debug.Log($"[PolarInputHandler] Main Camera {(mainCamera != null ? "found" : "NOT FOUND")}");
                }
            }
            
            // LineRenderer 초기화
            InitializeLaserBeam();
            
            // New Input System 초기화
            _mouse = Mouse.current;
            if (enableDebugLogs)
            {
                Debug.Log($"[PolarInputHandler] Mouse.current {(_mouse != null ? "available" : "NULL")}");
            }
            
            if (inputActions != null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[PolarInputHandler] InputActionAsset assigned: {inputActions.name}");
                }
                
                _pushAction = inputActions.FindAction(pushActionName);
                if (_pushAction == null)
                {
                    Debug.LogWarning($"[PolarInputHandler] Action '{pushActionName}' not found in InputActionAsset!");
                }
                else if (enableDebugLogs)
                {
                    Debug.Log($"[PolarInputHandler] Action '{pushActionName}' found successfully");
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.Log("[PolarInputHandler] No InputActionAsset assigned, creating fallback");
                }
                // InputActionAsset이 없으면 직접 생성 (폴백)
                CreateFallbackInputAction();
            }
        }

        private void OnEnable()
        {
            _pushAction?.Enable();
            if (enableDebugLogs)
            {
                Debug.Log($"[PolarInputHandler] OnEnable() - PushAction {(_pushAction != null ? "enabled" : "NULL")}");
            }
        }

        private void OnDisable()
        {
            _pushAction?.Disable();
            if (enableDebugLogs)
            {
                Debug.Log("[PolarInputHandler] OnDisable() - PushAction disabled");
            }
        }

        private void Start()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PolarInputHandler] Start() - Ready for input handling");
            }
        }

        private void Update()
        {
            if (controller == null || controller.IsGameOver) return;

            HandleInput();
        }

        /// <summary>
        /// New Input System으로 마우스 입력 처리
        /// </summary>
        private void HandleInput()
        {
            if (_pushAction == null)
            {
                if (enableDebugLogs && Time.time - _lastLogTime > 2f)
                {
                    Debug.LogWarning("[PolarInputHandler] _pushAction is NULL in HandleInput()");
                    _lastLogTime = Time.time;
                }
                return;
            }

            // New Input System: Action 상태 확인
            bool isInputActive = _pushAction.IsPressed();

            if (isInputActive)
            {
                if (_pushFrameCount == 0)
                {
                    _pushStartTime = Time.time;
                }

                _pushFrameCount++;

                if (enableDebugLogs && _pushFrameCount % 30 == 0) // 30프레임마다 로그
                {
                    float pushDuration = Time.time - _pushStartTime;
                    Debug.Log($"[PolarInputHandler] Push active for {_pushFrameCount} frames, Duration: {pushDuration:F3}s");
                }

                Vector2 mouseWorldPos = GetMouseWorldPosition();

                if (enableDebugLogs && _pushFrameCount == 1)
                {
                    Debug.Log($"[PolarInputHandler] First push at mouse position: {mouseWorldPos}");
                }

                // 충돌 예측을 먼저 수행하여 정확한 타격 지점 계산
                CalculateTargetPosition(mouseWorldPos);

                // 레이저 빔 시각화만 업데이트 (실제 데미지는 무기 시스템이 처리)
                UpdateLaserBeamFromPrediction();

                _isPushing = true;
            }
            else
            {
                if (_isPushing)
                {
                    // 레이저 빔 비활성화
                    DisableLaserBeam();

                    if (enableDebugLogs)
                    {
                        float totalDuration = Time.time - _pushStartTime;
                        bool wasShortClick = totalDuration < 0.1f && _pushFrameCount <= 5;
                        Debug.Log($"[PolarInputHandler] Push released after {_pushFrameCount} frames, Duration: {totalDuration:F3}s");
                        Debug.Log($"  Classification: {(wasShortClick ? "SHORT CLICK" : "HOLD")}");
                    }
                }
                _isPushing = false;
                _pushFrameCount = 0;
                _pushStartTime = 0f;
            }
        }

        /// <summary>
        /// 마우스 스크린 좌표 → 월드 좌표 변환 (New Input System)
        /// </summary>
        private Vector2 GetMouseWorldPosition()
        {
            if (mainCamera == null || _mouse == null)
            {
                if (enableDebugLogs && Time.time - _lastLogTime > 2f)
                {
                    Debug.LogWarning($"[PolarInputHandler] GetMouseWorldPosition() - Camera:{mainCamera != null}, Mouse:{_mouse != null}");
                    _lastLogTime = Time.time;
                }
                return Vector2.zero;
            }

            // New Input System: Mouse.current.position
            Vector2 mousePos = _mouse.position.ReadValue();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane));
            return new Vector2(worldPos.x, worldPos.y);
        }


        /// <summary>
        /// 레이저 빔 초기화
        /// </summary>
        private void InitializeLaserBeam()
        {
            if (laserBeam == null && createLaserIfMissing)
            {
                // LineRenderer 자동 생성
                GameObject laserObj = new GameObject("PlayerLaserBeam");
                laserObj.transform.SetParent(transform);
                laserObj.transform.localPosition = Vector3.zero;
                
                laserBeam = laserObj.AddComponent<LineRenderer>();
                
                if (enableDebugLogs)
                {
                    Debug.Log("[PolarInputHandler] Created LineRenderer for laser beam");
                }
            }
            
            if (laserBeam != null)
            {
                // LineRenderer 설정
                laserBeam.positionCount = 2;
                laserBeam.startWidth = laserWidth;
                laserBeam.endWidth = laserWidth * 0.5f;
                laserBeam.numCornerVertices = 4;
                laserBeam.numCapVertices = 4;
                
                // Material 설정
                if (laserMaterial != null)
                {
                    laserBeam.material = laserMaterial;
                }
                else
                {
                    // 기본 Material 생성
                    laserBeam.material = new Material(Shader.Find("Sprites/Default"));
                }
                
                laserBeam.startColor = laserColor;
                laserBeam.endColor = laserColor;
                laserBeam.sortingOrder = 20; // 장막 + 투사체 위에
                
                laserBeam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                laserBeam.receiveShadows = false;
                
                // 초기 비활성화
                laserBeam.enabled = false;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[PolarInputHandler] LaserBeam configured: Color={laserColor}, Width={laserWidth}");
                }
            }
        }

        /// <summary>
        /// 마우스 위치를 기반으로 정확한 타격 지점 계산 (단일 충돌 계산)
        /// </summary>
        private void CalculateTargetPosition(Vector2 mouseWorldPos)
        {
            Vector3 startPos = controller.transform.position;
            Vector2 direction = (mouseWorldPos - (Vector2)startPos).normalized;

            if (enableCollisionPrediction)
            {
                // 수학적 충돌 예측 사용 (PolarField 청크 + 외부 적들, 빔 넓이 고려)
                var firstHit = CollisionPredictor.PredictFirstCollision(
                    startPos,
                    direction,
                    beamWidth, // 빔 넓이 추가
                    maxLaserRange,
                    collisionLayers,
                    predictMovingTargets
                );

                if (firstHit.HasValue)
                {
                    _lastTargetWorldPos = firstHit.Value.hitPoint;
                    _lastPredictedHit = firstHit.Value;
                    _lastPredictedEndPoint = firstHit.Value.hitPoint;

                    if (enableDebugLogs && _pushFrameCount == 1)
                    {
                        Debug.Log($"[PolarInputHandler] Predicted collision: Type={firstHit.Value.type}, Distance={firstHit.Value.distance:F2}");
                    }
                }
                else
                {
                    // 충돌이 없으면 마우스 방향으로 최대 범위까지
                    _lastTargetWorldPos = mouseWorldPos;
                    _lastPredictedHit = null;
                    _lastPredictedEndPoint = mouseWorldPos;
                }
            }
            else
            {
                // 예측 없이 마우스 위치 그대로 사용
                _lastTargetWorldPos = mouseWorldPos;
                _lastPredictedHit = null;
                _lastPredictedEndPoint = mouseWorldPos;
            }
        }

        /// <summary>
        /// 이미 계산된 예측 결과로 레이저 빔 업데이트
        /// </summary>
        private void UpdateLaserBeamFromPrediction()
        {
            if (laserBeam == null) return;

            // 레이저 활성화
            if (!laserBeam.enabled)
            {
                laserBeam.enabled = true;
            }

            // 시작점: 컨트롤러 중심 (지구)
            Vector3 startPos = controller.transform.position;

            // LineRenderer 위치 설정 (이미 계산된 endPoint 사용)
            laserBeam.SetPosition(0, startPos);
            laserBeam.SetPosition(1, _lastPredictedEndPoint);
        }

        /// <summary>
        /// 레이저 빔 비활성화
        /// </summary>
        private void DisableLaserBeam()
        {
            if (laserBeam != null && laserBeam.enabled)
            {
                laserBeam.enabled = false;
            }
        }

        /// <summary>
        /// InputActionAsset이 없을 때 폴백 액션 생성
        /// </summary>
        private void CreateFallbackInputAction()
        {
            _pushAction = new InputAction("Push", InputActionType.Button);
            _pushAction.AddBinding("<Mouse>/leftButton");
            _pushAction.Enable();
            
            Debug.Log("[PolarInputHandler] Created fallback InputAction for Mouse left button");
            
            // 바인딩 확인
            if (enableDebugLogs)
            {
                Debug.Log($"[PolarInputHandler] Fallback action bindings: {_pushAction.bindings.Count}");
                for (int i = 0; i < _pushAction.bindings.Count; i++)
                {
                    Debug.Log($"  Binding {i}: {_pushAction.bindings[i].effectivePath}");
                }
            }
        }

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!_isPushing || controller == null) return;

            // 현재 마우스 위치 표시
            Vector2 worldPos = GetMouseWorldPosition();
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(worldPos, 0.2f);

            // 실제 타격 지점 표시 (CalculateTargetPosition 결과)
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_lastTargetWorldPos, 0.15f);

            // 중심에서 마우스까지 선 (기본 방향)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(controller.transform.position, worldPos);

            if (showPredictionGizmos && enableCollisionPrediction)
            {
                // 예측된 충돌 지점 표시
                if (_lastPredictedHit.HasValue)
                {
                    var hit = _lastPredictedHit.Value;

                    // 충돌 지점 마크
                    Gizmos.color = predictionHitColor;
                    Gizmos.DrawWireSphere(hit.hitPoint, 0.3f);

                    // 충돌 타입에 따른 색상 변화
                    switch (hit.type)
                    {
                        case CollisionPredictor.CollisionType.Enemy:
                            Gizmos.color = Color.red;
                            break;
                        case CollisionPredictor.CollisionType.PolarField:
                            Gizmos.color = Color.blue;
                            break;
                        case CollisionPredictor.CollisionType.Obstacle:
                            Gizmos.color = Color.gray;
                            break;
                        default:
                            Gizmos.color = predictionHitColor;
                            break;
                    }

                    // 충돌 지점에서의 법선 벡터
                    Gizmos.DrawRay(hit.hitPoint, hit.normal * 0.5f);
                }

                // 실제 레이저 빔 경로 (중심선)
                Gizmos.color = Color.green;
                Gizmos.DrawLine(controller.transform.position, _lastPredictedEndPoint);

                // 빔 넓이 시각화
                if (beamWidth > 0)
                {
                    Vector3 beamDirection = ((Vector3)_lastPredictedEndPoint - controller.transform.position).normalized;
                    Vector3 perpendicular = new Vector3(-beamDirection.y, beamDirection.x, 0f);
                    float halfWidth = beamWidth * 0.5f;

                    Vector3 leftEdge = (Vector3)_lastPredictedEndPoint + perpendicular * halfWidth;
                    Vector3 rightEdge = (Vector3)_lastPredictedEndPoint - perpendicular * halfWidth;
                    Vector3 leftStart = controller.transform.position + perpendicular * halfWidth;
                    Vector3 rightStart = controller.transform.position - perpendicular * halfWidth;

                    Gizmos.color = Color.yellow;
                    // 빔 가장자리 선들
                    Gizmos.DrawLine(leftStart, leftEdge);
                    Gizmos.DrawLine(rightStart, rightEdge);
                    // 빔 끝부분 연결선
                    Gizmos.DrawLine(leftEdge, rightEdge);
                }
            }
        }

        #endregion
    }
}
