using UnityEngine;
using UnityEngine.InputSystem;
using ShapeDefense.Scripts.Data;
using ShapeDefense.Scripts.Weapons;
using System.Collections.Generic;
using Script.SystemCore.Resource;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 무기 컨트롤러 - BaseWeapon 프리팹을 로드하고 관리
    /// 각 무기는 BaseWeapon을 상속한 전용 스크립트를 가짐
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Camera aimCamera;

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string attackActionName = "Attack";

        [Header("Settings")]
        [SerializeField] private int teamKey = 1;
        [SerializeField] private bool holdToFire = true;

        [Header("Weapon Data")]
        [SerializeField] private WeaponDataTable weaponDataTable;
        [SerializeField] private string currentWeaponId = "";
        
        [Header("Available Weapons")]
        [SerializeField] private List<string> availableWeaponIds = new List<string>();

        // Runtime
        private int _cachedShooterTeam;
        private PlayerInputActionsRuntime _runtimeActions;
        private InputAction _attackAction;
        
        private WeaponData _currentWeaponData;
        private BaseWeapon _currentWeapon;
        private Transform _weaponSlot;  // 무기 장착 위치만 유지
        
        private bool isInitialized = false;
        
        public void Init()
        {
            if (isInitialized) return;
            AutoSetup();
            BindInput();
            LoadWeapon();
            isInitialized = true;
        }
        
        /// <summary>
        /// 자동 Setup - WeaponSlot만 생성
        /// </summary>
        private void AutoSetup()
        {
            // Camera
            if (aimCamera == null) 
                aimCamera = Camera.main;
            
            // Team Key
            _cachedShooterTeam = TryGetComponent<Health>(out var health) ? health.TeamKey : teamKey;
            
            if (_weaponSlot == null)
            {
                var slotGo = new GameObject("WeaponSlot");
                _weaponSlot = slotGo.transform;
                _weaponSlot.SetParent(transform);
                _weaponSlot.localPosition = Vector3.zero;
                _weaponSlot.localRotation = Quaternion.identity;
            }
            
            Debug.Log("[WeaponController] Auto Setup 완료: WeaponSlot 생성됨");
        }
        
        private void LoadWeapon()
        {
            if (weaponDataTable == null)
            {
                Debug.LogError("[WeaponController] WeaponDataTable이 할당되지 않았습니다!");
                return;
            }
            
            if (string.IsNullOrEmpty(currentWeaponId))
            {
                Debug.LogWarning("[WeaponController] CurrentWeaponId가 비어있습니다!");
                return;
            }
            
            _currentWeaponData = weaponDataTable.GetById(currentWeaponId);
            
            if (_currentWeaponData == null)
            {
                Debug.LogError($"[WeaponController] 무기 ID를 찾을 수 없습니다: {currentWeaponId}");
                return;
            }
            
            EquipWeapon();
        }
        
        /// <summary>
        /// 무기 장착 - BaseWeapon 프리팹 로드
        /// </summary>
        private void EquipWeapon()
        {
            // 기존 무기 제거
            if (_currentWeapon != null)
            {
                Destroy(_currentWeapon.gameObject);
                _currentWeapon = null;
            }
            
            if (_currentWeaponData == null || _weaponSlot == null) return;
            
            var weaponGo = ResourceService.Instance.LoadPrefab(_currentWeaponData.WeaponBundleId);
            var weaponInstance = Instantiate(weaponGo, _weaponSlot);
            // 무기 생성
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;
            
            // BaseWeapon 컴포넌트 획득 ⭐
            _currentWeapon = weaponInstance.GetComponent<BaseWeapon>();

            if (_currentWeapon == null)
            {
                Debug.LogError($"[WeaponController] BaseWeapon 컴포넌트를 찾을 수 없습니다: {_currentWeaponData.WeaponBundleId}");
                Destroy(weaponInstance);
                return;
            }
            
            // 무기 초기화 - WeaponData 주입 ⭐
            _currentWeapon.Initialize(_currentWeaponData, gameObject, _cachedShooterTeam);
            
            Debug.Log($"[WeaponController] 무기 장착: {_currentWeaponData.WeaponName} ({_currentWeapon.GetType().Name})");
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
            
            if (_currentWeapon != null)
            {
                _currentWeapon.StopFire();  // 자동 발사 중지
                Destroy(_currentWeapon.gameObject);
            }
        }

        private void BindInput()
        {
            if (inputActions != null)
            {
                var map = inputActions.FindActionMap(actionMapName, throwIfNotFound: false);
                _attackAction = map?.FindAction(attackActionName, throwIfNotFound: false);
            }

            if (_attackAction == null)
            {
                _runtimeActions = new PlayerInputActionsRuntime();
                _attackAction = _runtimeActions.Player.Attack;
            }
        }

        void Update()
        {
            if( !isInitialized) return;
            
            if (aimCamera == null || _currentWeapon == null) return;

            // 마우스 위치 계산
            var mouse = Mouse.current;
            var screenPos = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            var world = (Vector2)aimCamera.ScreenToWorldPoint(screenPos);

            // 무기에 조준 정보 전달
            _currentWeapon.UpdateAim(world);

            // 공격 입력 처리
            bool attackPressed = _attackAction != null && _attackAction.IsPressed();
            bool attackJustPressed = _attackAction != null && _attackAction.WasPressedThisFrame();
            bool attackReleased = _attackAction != null && _attackAction.WasReleasedThisFrame();

            // 발사 모드에 따라 처리
            if (_currentWeapon.CurrentFireMode == FireMode.Automatic)
            {
                // 자동 모드: 버튼 누르는 동안 연속 발사
                if (attackJustPressed)
                {
                    _currentWeapon.StartFire();
                }
                else if (attackReleased)
                {
                    _currentWeapon.StopFire();
                }
            }
            else // Manual
            {
                // 수동 모드: 클릭할 때마다 1발
                if (holdToFire ? attackPressed : attackJustPressed)
                {
                    _currentWeapon.Fire(world);
                }
            }
        }

        /// <summary>
        /// 무기 변경 (ID로)
        /// </summary>
        public void ChangeWeapon(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return;
            if (weaponDataTable == null) return;

            var newWeaponData = weaponDataTable.GetById(weaponId);
            if (newWeaponData == null)
            {
                Debug.LogError($"[WeaponController] 무기 ID를 찾을 수 없습니다: {weaponId}");
                return;
            }

            currentWeaponId = weaponId;
            _currentWeaponData = newWeaponData;
            
            EquipWeapon();

            Debug.Log($"[WeaponController] 무기 변경: {newWeaponData.WeaponName}");
        }

        /// <summary>
        /// 인덱스로 무기 변경
        /// </summary>
        public void ChangeWeapon(int index)
        {
            if (index < 0 || index >= availableWeaponIds.Count) return;
            ChangeWeapon(availableWeaponIds[index]);
        }

        /// <summary>
        /// 다음 무기로 전환
        /// </summary>
        public void NextWeapon()
        {
            if (availableWeaponIds.Count == 0) return;
            
            int currentIndex = availableWeaponIds.IndexOf(currentWeaponId);
            int nextIndex = (currentIndex + 1) % availableWeaponIds.Count;
            ChangeWeapon(nextIndex);
        }

        /// <summary>
        /// 이전 무기로 전환
        /// </summary>
        public void PreviousWeapon()
        {
            if (availableWeaponIds.Count == 0) return;
            
            int currentIndex = availableWeaponIds.IndexOf(currentWeaponId);
            int prevIndex = (currentIndex - 1 + availableWeaponIds.Count) % availableWeaponIds.Count;
            ChangeWeapon(prevIndex);
        }

        public void SetTeamKey(int healthTeamKey)
        {
            teamKey = healthTeamKey;
            _cachedShooterTeam = healthTeamKey;
            
            if (_currentWeapon != null && _currentWeaponData != null)
            {
                _currentWeapon.Initialize(_currentWeaponData, gameObject, _cachedShooterTeam);
            }
        }

        public WeaponData CurrentWeapon => _currentWeaponData;
        public BaseWeapon CurrentWeaponInstance => _currentWeapon;  // ⭐
        public IReadOnlyList<string> AvailableWeaponIds => availableWeaponIds;
    }
}

