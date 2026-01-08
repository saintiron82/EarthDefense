using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// Polar 무기 암(Arm) - Transform 노드
    /// 암이 회전하면 자식인 Muzzle도 함께 회전
    /// ShapeDefense WeaponArm 기반 + 극좌표 전용 모드 추가
    /// </summary>
    public class PolarWeaponArm : MonoBehaviour
    {
        [Header("Arm Settings")]
        [Tooltip("암 동작 타입")]
        [SerializeField] private PolarArmBehaviorType behaviorType = PolarArmBehaviorType.PolarAngle;
        
        [Tooltip("고정 각도 (Fixed 타입)")]
        [SerializeField] private float fixedAngle;
        
        [Tooltip("회전 속도 (Rotate 타입)")]
        [SerializeField] private float rotationSpeed = 90f;
        
        [Header("References")]
        [Tooltip("머즐 (발사 지점) - 이 암의 자식")]
        [SerializeField] private Transform muzzle;
        
        // Runtime
        private float _currentRotation;
        private float _targetAngle;  // 목표 각도 (모든 모드 공용)
        
        // Properties
        public Transform Muzzle => muzzle;
        public PolarArmBehaviorType BehaviorType => behaviorType;
        
        /// <summary>
        /// 조준 방향 설정 (각도 직접 지정)
        /// </summary>
        public void SetAimAngle(float angleInDegrees)
        {
            _targetAngle = angleInDegrees;
        }
        
        /// <summary>
        /// 마우스 위치로 조준 설정
        /// </summary>
        public void SetAimFromWorldPosition(Vector2 worldPosition)
        {
            Vector2 armPos = transform.position;
            Vector2 dir = (worldPosition - armPos);
            
            if (dir.sqrMagnitude > 0.0001f)
            {
                _targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }
        }
        
        private void Reset()
        {
            // 자식에서 Muzzle 자동 찾기
            if (muzzle == null)
            {
                muzzle = transform.Find("Muzzle");
            }
        }
        
        private void Update()
        {
            UpdateArmBehavior();
        }
        
        /// <summary>
        /// Arm 동작 업데이트 - Transform만 회전
        /// </summary>
        private void UpdateArmBehavior()
        {
            switch (behaviorType)
            {
                case PolarArmBehaviorType.Fixed:
                    // 고정 각도 유지
                    transform.localRotation = Quaternion.Euler(0f, 0f, fixedAngle);
                    break;
                    
                case PolarArmBehaviorType.Rotate:
                    // 자동 회전
                    _currentRotation += rotationSpeed * Time.deltaTime;
                    transform.localRotation = Quaternion.Euler(0f, 0f, _currentRotation);
                    break;
                    
                case PolarArmBehaviorType.PolarAngle:
                    // 극좌표 각도 추적 (Polar 기본) ⭐
                    transform.localRotation = Quaternion.Euler(0f, 0f, _targetAngle);
                    break;
                    
                case PolarArmBehaviorType.MouseFollow:
                    // 마우스 위치 추적 (ShapeDefense 호환) ⭐
                    transform.localRotation = Quaternion.Euler(0f, 0f, _targetAngle);
                    break;
                    
                case PolarArmBehaviorType.FollowParent:
                    // 부모(Aim Pivot) 따라감 (아무것도 안 함)
                    break;
                    
                case PolarArmBehaviorType.Independent:
                    // 독립 조준 (추후 구현)
                    break;
            }
        }
    }
    
    /// <summary>
    /// Polar 암 동작 타입
    /// </summary>
    public enum PolarArmBehaviorType
    {
        PolarAngle,      // 극좌표 각도 직접 지정 (Polar 기본) ⭐
        MouseFollow,     // 마우스 위치 추적 (ShapeDefense 호환) ⭐
        FollowParent,    // 부모 Transform 따라감
        Fixed,           // 고정 각도
        Rotate,          // 자동 회전
        Independent      // 독립 조준
    }
}
