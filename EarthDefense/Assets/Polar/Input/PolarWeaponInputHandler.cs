using UnityEngine;
using UnityEngine.InputSystem;
using Polar.Weapons;

namespace Polar.Input
{
    /// <summary>
    /// Polar 무기 시스템 입력 핸들러 (완전 독립)
    /// 
    /// - PolarInputActionsRuntime 사용 (Polar 전용)
    /// - Attack/Look 액션
    /// - PlayerWeaponManager 제어
    /// - ShapeDefense 의존성 제거 ✅
    /// </summary>
    public class PolarWeaponInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWeaponManager weaponManager;
        [SerializeField] private Camera aimCamera;
        
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string attackActionName = "Attack";
        
        [Header("Settings")]
        [Tooltip("홀드 모드 (true: 누르는 동안 발사, false: 클릭마다 발사)")]
        [SerializeField] private bool holdToFire = true;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;
        
        // Runtime
        private PolarInputActionsRuntime _runtimeActions;  // ✅ Polar 전용
        private InputAction _attackAction;
        private bool _isInitialized;

        private bool _wasAttackPressed;
        
        private void Awake()
        {
            if (weaponManager == null)
            {
                weaponManager = GetComponentInChildren<PlayerWeaponManager>();
                if (enableDebugLogs)
                {
                    Debug.Log($"[PolarWeaponInput] WeaponManager {(weaponManager != null ? "found" : "NOT FOUND")}");
                }
            }
            
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
            
            BindInput();
        }
        
        private void OnEnable()
        {
            _attackAction?.Enable();
        }

        private void OnDisable()
        {
            _attackAction?.Disable();
        }

        private void OnDestroy()
        {
            _runtimeActions?.Dispose();
            _runtimeActions = null;
            
            if (weaponManager != null)
            {
                weaponManager.StopFireLaser();
            }
        }

        private void BindInput()
        {
            // InputActionAsset이 있으면 사용
            if (inputActions != null)
            {
                var map = inputActions.FindActionMap(actionMapName, throwIfNotFound: false);
                _attackAction = map?.FindAction(attackActionName, throwIfNotFound: false);
            }

            // InputActionAsset이 없으면 런타임 생성 (Fallback) ✅ Polar 전용
            if (_attackAction == null)
            {
                _runtimeActions = new PolarInputActionsRuntime();
                _attackAction = _runtimeActions.Player.Attack;
                
                if (enableDebugLogs)
                {
                    Debug.Log("[PolarWeaponInput] Created Polar runtime input actions (Fallback)");
                }
            }
            
            _isInitialized = true;
        }
        
        private void Update()
        {
            if (!_isInitialized || weaponManager == null || aimCamera == null) return;

            // 마우스 위치 계산
            var mouse = Mouse.current;
            var screenPos = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            var worldPos = (Vector2)aimCamera.ScreenToWorldPoint(screenPos);

            // 마우스 방향을 각도로 변환 (Polar 좌표계)
            Vector2 fieldCenter = weaponManager.transform.position;
            Vector2 direction = (worldPos - fieldCenter).normalized;
            float angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 공격 입력 처리
            bool attackPressed = _attackAction != null && _attackAction.IsPressed();
            bool attackReleased = _attackAction != null && _attackAction.WasReleasedThisFrame();

            bool attackJustPressed;
            if (_attackAction != null)
            {
                attackJustPressed = _attackAction.WasPressedThisFrame();
            }
            else
            {
                attackJustPressed = attackPressed && !_wasAttackPressed;
            }

            // 발사 처리
            if (holdToFire)
            {
                // 홀드 모드: 레이저용
                if (attackJustPressed)
                {
                    weaponManager.StartFireLaser();
                }

                if (attackPressed)
                {
                    // 연속 발사 (레이저는 내부에서 업데이트)
                    weaponManager.FireProjectile(angleDeg);
                }
                else if (attackReleased)
                {
                    weaponManager.StopFireLaser();
                }
            }
            else
            {
                // 클릭 모드: 클릭마다 1발
                if (attackJustPressed)
                {
                    weaponManager.FireProjectile(angleDeg);
                }
            }

            _wasAttackPressed = attackPressed;
        }
        
        private void OnDrawGizmos()
        {
            if (!_isInitialized || aimCamera == null) return;
            
            // 마우스 위치 표시
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var screenPos = mouse.position.ReadValue();
                var worldPos = aimCamera.ScreenToWorldPoint(screenPos);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(worldPos, 0.3f);
            }
        }
    }
}
