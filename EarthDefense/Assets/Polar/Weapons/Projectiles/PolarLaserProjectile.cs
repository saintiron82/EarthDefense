using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 레이저 투사체 (빔 타입)
    /// - PolarLaserWeaponData와 매칭
    /// - 독립 로직: 빔 확장/수축, 틱 데미지, 섹터 인덱스 계산
    /// - SGSystem PoolService 완전 통합
    /// </summary>
    public class PolarLaserProjectile : PolarProjectileBase
    {
        [Header("Visual")]
        [SerializeField] private LineRenderer lineRenderer;

        private PolarLaserWeaponData LaserData => _weaponData as PolarLaserWeaponData;
        
        private bool _isRetracting;
        private float _currentLength;
        private float _nextTickTime;
        private Vector2 _direction;
        private Vector2 _origin;

        public override void Launch(IPolarField field, PolarWeaponData weaponData)
        {
            if (weaponData is not PolarLaserWeaponData)
            {
                Debug.LogError("[PolarLaserProjectile] Requires PolarLaserWeaponData!");
                return;
            }

            _field = field;
            _weaponData = weaponData;
            _isActive = true;

            _origin = (_field as Component) != null ? ((Component)_field).transform.position : Vector2.zero;
            _direction = Vector2.right;
            
            InitializeBeam();
        }

        /// <summary>
        /// 특정 방향으로 발사 (오버로드)
        /// </summary>
        public void Launch(IPolarField field, PolarWeaponData weaponData, Vector2 origin, Vector2 direction)
        {
            Launch(field, weaponData);
            _origin = origin;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        }

        private void InitializeBeam()
        {
            _isRetracting = false;
            _currentLength = 0f;
            _nextTickTime = Time.time;
            
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.startColor = LaserData.BeamColor;
                lineRenderer.endColor = LaserData.BeamColor;
                lineRenderer.startWidth = LaserData.BeamWidth;
                lineRenderer.endWidth = LaserData.BeamWidth;
                lineRenderer.positionCount = 2;
            }
        }

        public void BeginRetract()
        {
            _isRetracting = true;
        }

        public override void Deactivate()
        {
            base.Deactivate();
            
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// 풀 반환 시 추가 정리
        /// </summary>
        protected override void OnPoolReturn()
        {
            _isRetracting = false;
            _currentLength = 0f;
            _direction = Vector2.zero;
            _origin = Vector2.zero;
            
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            UpdateBeamLength(deltaTime);
            UpdateBeamVisual();
            ApplyTickDamageIfNeeded();

            // ✅ 리트랙트 완료 시 풀 반환
            if (_isRetracting && _currentLength <= 0f)
            {
                ReturnToPool();
            }
        }

        private void UpdateBeamLength(float deltaTime)
        {
            if (_isRetracting)
            {
                _currentLength = Mathf.Max(0f, _currentLength - LaserData.RetractSpeed * deltaTime);
            }
            else
            {
                _currentLength = Mathf.Min(LaserData.MaxLength, _currentLength + LaserData.ExtendSpeed * deltaTime);
            }
        }

        private void UpdateBeamVisual()
        {
            if (lineRenderer == null) return;

            Vector2 end = _origin + _direction * _currentLength;
            lineRenderer.SetPosition(0, _origin);
            lineRenderer.SetPosition(1, end);
        }

        private void ApplyTickDamageIfNeeded()
        {
            if (_weaponData == null || Time.time < _nextTickTime) return;

            Vector2 hitPoint = _origin + _direction * _currentLength;
            ApplyTickDamage(hitPoint);

            _nextTickTime = Time.time + 1f / Mathf.Max(0.0001f, _weaponData.TickRate);
        }

        private void ApplyTickDamage(Vector2 hitPoint)
        {
            if (_field == null || _weaponData == null) return;

            Vector2 center = (_field as Component) != null 
                ? (Vector2)((Component)_field).transform.position 
                : Vector2.zero;

            Vector2 dir = hitPoint - center;
            if (dir.sqrMagnitude <= Mathf.Epsilon) return;

            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            int sectorIndex = _field.AngleToSectorIndex(angleDeg);

            float damagePerTick = _weaponData.Damage / Mathf.Max(0.0001f, _weaponData.TickRate);

            _field.SetLastWeaponKnockback(_weaponData.KnockbackPower);
            _field.ApplyDamageToSector(sectorIndex, damagePerTick);

            if (_field.EnableWoundSystem)
            {
                _field.ApplyWound(sectorIndex, _weaponData.WoundIntensity);
            }
        }
    }
}
