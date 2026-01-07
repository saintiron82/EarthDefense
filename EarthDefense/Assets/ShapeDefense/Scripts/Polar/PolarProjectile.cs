using UnityEngine;

namespace ShapeDefense.Scripts.Polar
{
    /// <summary>
    /// Phase 1 - Step 4: 극좌표 투사체 (HTML bullets 배열 재현)
    /// 극좌표 기반 이동 및 충돌 감지
    /// </summary>
    public class PolarProjectile : MonoBehaviour
    {
        [Header("Polar Coordinates")]
        [SerializeField] private float angle;      // 각도 (degree)
        [SerializeField] private float radius;     // 반지름 (Unity 단위)
        
        [Header("Movement")]
        [SerializeField] private float speed = 10f;
        
        [Header("Collision")]
        [SerializeField] private float collisionEpsilon = 0.1f;
        
        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private LineRenderer lineRenderer;  // ✅ LineRenderer 추가 (레이저 빔)
        
        [Header("Visual Settings")]
        [SerializeField] private float projectileScale = 0.3f;
        [SerializeField] private Color projectileColor = Color.yellow;
        [SerializeField] private bool useLineRenderer = true;  // LineRenderer 우선 사용
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;
        
        // 상태
        private bool _isActive;
        private PolarFieldController _fieldController;
        private PolarProjectileManager _manager;  // ✅ Manager 참조 추가
        
        // 프로퍼티
        public float Angle => angle;
        public float Radius => radius;
        public bool IsActive => _isActive;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            if (trailRenderer == null)
            {
                trailRenderer = GetComponent<TrailRenderer>();
            }
            
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }
            
            // 스케일 적용
            transform.localScale = Vector3.one * projectileScale;
        }

        /// <summary>
        /// 투사체 초기화 및 발사
        /// </summary>
        public void Launch(PolarFieldController controller, PolarProjectileManager manager, float launchAngle, float startRadius, float projectileSpeed, float epsilon)
        {
            _fieldController = controller;
            _manager = manager;
            angle = launchAngle;
            radius = startRadius;
            speed = projectileSpeed;
            collisionEpsilon = epsilon;
            
            _isActive = true;
            
            // 시각적 활성화
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = projectileColor;
            }
            
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = true;
                trailRenderer.startColor = projectileColor;
                trailRenderer.endColor = new Color(projectileColor.r, projectileColor.g, projectileColor.b, 0f);
            }
            
            if (lineRenderer != null && useLineRenderer)
            {
                lineRenderer.enabled = true;
                lineRenderer.startColor = projectileColor;
                lineRenderer.endColor = projectileColor;
                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.05f;
            }
            
            // 초기 위치 설정
            UpdatePosition();
            
            if (showDebugLogs)
            {
                Debug.Log($"[PolarProjectile] Launched at angle={launchAngle:F1}°, r={startRadius:F2}, speed={projectileSpeed}");
            }
        }

        /// <summary>
        /// 투사체 비활성화
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
            
            // 시각적 비활성화
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (trailRenderer != null) trailRenderer.emitting = false;
            if (lineRenderer != null) lineRenderer.enabled = false;
        }

        private void Update()
        {
            if (!_isActive || _fieldController == null) return;

            float deltaTime = Time.deltaTime;
            
            // 1. 반지름 증가 (바깥으로 이동)
            radius += speed * deltaTime;
            
            // 2. 위치 업데이트 (먼저)
            UpdatePosition();
            
            // 3. 충돌 체크
            if (CheckCollision())
            {
                OnCollision();
                return;
            }
            
            // 4. 화면 밖 체크
            if (radius > _fieldController.InitialRadius * 2f)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[PolarProjectile] Out of bounds at r={radius:F2}");
                }
                Deactivate();
                return;
            }
        }

        /// <summary>
        /// 극좌표 → 카테시안 좌표 변환 및 위치 업데이트
        /// </summary>
        private void UpdatePosition()
        {
            if (_fieldController == null) return;

            float angleRad = angle * Mathf.Deg2Rad;
            Vector3 polarPos = new Vector3(
                Mathf.Cos(angleRad) * radius,
                Mathf.Sin(angleRad) * radius,
                0f
            );
            
            transform.position = _fieldController.transform.position + polarPos;

            // LineRenderer 업데이트
            if (useLineRenderer && lineRenderer != null)
            {
                lineRenderer.SetPosition(0, _fieldController.transform.position);
                lineRenderer.SetPosition(1, transform.position);
            }
        }

        /// <summary>
        /// 충돌 감지: 투사체 반지름 >= 섹터 반지름
        /// </summary>
        private bool CheckCollision()
        {
            if (_fieldController == null) return false;

            int sectorIndex = _fieldController.AngleToSectorIndex(angle);
            float sectorRadius = _fieldController.GetSectorRadius(sectorIndex);
            
            // HTML 로직: r >= wallRadius[idx]
            bool collision = radius >= (sectorRadius - collisionEpsilon);
            
            if (showDebugLogs && collision)
            {
                Debug.Log($"[PolarProjectile] Collision! r={radius:F2} >= sectorRadius={sectorRadius:F2}, sector={sectorIndex}");
            }
            
            return collision;
        }

        /// <summary>
        /// 충돌 처리
        /// </summary>
        private void OnCollision()
        {
            if (_fieldController == null)
            {
                Deactivate();
                return;
            }

            // 충돌 지점 섹터
            int hitSectorIndex = _fieldController.AngleToSectorIndex(angle);
            
            // ✅ Manager에게 충돌 알림
            if (_manager != null)
            {
                _manager.OnProjectileCollision(this, hitSectorIndex);
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning("[PolarProjectile] Manager is null, cannot notify collision!");
            }
            
            Deactivate();
        }

        /// <summary>
        /// Gizmo 디버그
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_isActive || _fieldController == null) return;

            // 현재 위치
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            
            // 중심에서 투사체까지 선
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_fieldController.transform.position, transform.position);
        }
    }
}
