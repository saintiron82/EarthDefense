using UnityEngine;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 무기 암(Arm) - 단순 Transform 노드
    /// 암이 회전하면 자식인 Muzzle도 함께 회전
    /// </summary>
    public class WeaponArm : MonoBehaviour
    {
        [Header("Arm Settings")]
        [Tooltip("암 동작 타입")]
        [SerializeField] private ArmBehaviorType behaviorType = ArmBehaviorType.FollowAim;
        
        [Tooltip("고정 각도 (Fixed 타입)")]
        [SerializeField] private float fixedAngle;
        
        [Tooltip("회전 속도 (Rotate 타입)")]
        [SerializeField] private float rotationSpeed = 90f;
        
        [Header("References")]
        [Tooltip("머즐 (발사 지점) - 이 암의 자식")]
        [SerializeField] private Transform muzzle;
        
        // Runtime
        private float _currentRotation;
        private float _targetAngle;  // FollowAim용 목표 각도
        
        // Properties
        public Transform Muzzle => muzzle;
        public ArmBehaviorType BehaviorType => behaviorType;
        
        /// <summary>
        /// 마우스 방향 설정 (FollowAim 모드용)
        /// </summary>
        public void SetAimDirection(float angleInDegrees)
        {
            _targetAngle = angleInDegrees;
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
                case ArmBehaviorType.Fixed:
                    // 고정 각도 유지
                    transform.localRotation = Quaternion.Euler(0f, 0f, fixedAngle);
                    break;
                    
                case ArmBehaviorType.Rotate:
                    // 자동 회전
                    _currentRotation += rotationSpeed * Time.deltaTime;
                    transform.localRotation = Quaternion.Euler(0f, 0f, _currentRotation);
                    break;
                    
                case ArmBehaviorType.FollowAim:
                    // 부모(Aim Pivot)를 따라감 (아무것도 안 함)
                    break;
                    
                case ArmBehaviorType.DirectMouseFollow:
                    // 마우스 방향으로 직접 회전 ⭐
                    transform.localRotation = Quaternion.Euler(0f, 0f, _targetAngle);
                    break;
                    
                case ArmBehaviorType.Independent:
                    // 독립 조준 (추후 구현)
                    break;
            }
        }
    }
    
    /// <summary>
    /// 암 동작 타입
    /// </summary>
    public enum ArmBehaviorType
    {
        FollowAim,          // 조준 방향 따라감 (부모 AimPivot 따라감)
        DirectMouseFollow,  // 마우스 방향으로 직접 회전 ⭐
        Fixed,              // 고정 각도
        Rotate,             // 자동 회전
        Independent         // 독립 조준
    }
}

