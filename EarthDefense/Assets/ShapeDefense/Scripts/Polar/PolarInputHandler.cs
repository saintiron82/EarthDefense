using UnityEngine;
using UnityEngine.InputSystem;

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

        [Header("Debug")]
        [SerializeField] private bool showDebugRays = true;
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private Color debugRayColor = Color.green;

        private PolarDataConfig _config;
        private bool _isPushing;
        
        // New Input System
        private InputAction _pushAction;
        private Mouse _mouse;
        
        // Debug
        private int _pushFrameCount;
        private float _lastLogTime;

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
            _config = controller?.Config;
            
            if (_config == null)
            {
                Debug.LogError("[PolarInputHandler] Config not found!");
                enabled = false;
                return;
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[PolarInputHandler] Start() - Config loaded");
                Debug.Log($"  PushPower: {_config.PushPower}");
                Debug.Log($"  SmoothingRadius: {_config.SmoothingRadius}");
                Debug.Log($"  SmoothingStrength: {_config.SmoothingStrength}");
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
                _pushFrameCount++;
                
                if (enableDebugLogs && _pushFrameCount % 30 == 0) // 30프레임마다 로그
                {
                    Debug.Log($"[PolarInputHandler] Push active for {_pushFrameCount} frames");
                }

                Vector2 worldPos = GetMouseWorldPosition();
                
                if (enableDebugLogs && _pushFrameCount == 1)
                {
                    Debug.Log($"[PolarInputHandler] First push at world position: {worldPos}");
                }
                
                CarveWall(worldPos);
                _isPushing = true;
            }
            else
            {
                if (_isPushing && enableDebugLogs)
                {
                    Debug.Log($"[PolarInputHandler] Push released after {_pushFrameCount} frames");
                }
                _isPushing = false;
                _pushFrameCount = 0;
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
        /// 특정 지점의 벽을 밀어내기 (HTML CarveWall 재현)
        /// </summary>
        private void CarveWall(Vector2 worldPosition)
        {
            // 극좌표 변환: Screen → Polar
            Vector2 localPos = worldPosition - (Vector2)controller.transform.position;
            
            // 각도 계산: Atan2 (0 ~ 2π)
            float angleRad = Mathf.Atan2(localPos.y, localPos.x);
            float angleDeg = angleRad * Mathf.Rad2Deg;
            if (angleDeg < 0) angleDeg += 360f;

            // 섹터 인덱스 변환
            int sectorIndex = controller.AngleToSectorIndex(angleDeg);

            // 밀어내기 양 계산 (초당 pushPower)
            float pushAmount = _config.PushPower * Time.deltaTime;

            if (enableDebugLogs && _pushFrameCount == 1)
            {
                Debug.Log($"[PolarInputHandler] CarveWall() called:");
                Debug.Log($"  World Pos: {worldPosition}");
                Debug.Log($"  Local Pos: {localPos}");
                Debug.Log($"  Angle: {angleDeg:F1}°");
                Debug.Log($"  Sector Index: {sectorIndex}");
                Debug.Log($"  Push Amount: {pushAmount:F4}");
            }

            // 평활화 적용 푸시
            controller.PushSectorRadiusSmooth(sectorIndex, pushAmount);

            // 디버그 시각화
            if (showDebugRays)
            {
                Vector3 direction = new Vector3(localPos.x, localPos.y, 0f).normalized;
                float currentRadius = controller.GetSectorRadius(sectorIndex);
                Vector3 hitPoint = controller.transform.position + direction * currentRadius;
                Debug.DrawLine(controller.transform.position, hitPoint, debugRayColor, 0.1f);
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

            // 중심에서 마우스까지 선
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(controller.transform.position, worldPos);
        }

        #endregion
    }
}
