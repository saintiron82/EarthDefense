using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// Polar 전용 빔: 자가 업데이트로 틱 데미지를 IPolarField에 직접 적용.
    /// ShapeDefense 무기 계층과 독립.
    /// </summary>
    public sealed class PolarBeamProjectile : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float tickRate = 10f;
        [SerializeField] private float maxLength = 50f;
        [SerializeField] private float extendSpeed = 50f;
        [SerializeField] private float retractSpeed = 70f;

        [Header("Visual")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Color beamColor = Color.cyan;

        [Header("Polar Field")]
        [SerializeField] private MonoBehaviour polarFieldBehaviour; // IPolarField 구현체 할당용
        [SerializeField] private float polarKnockbackPower = 0.1f;
        [SerializeField] private float polarWoundIntensity = 0f;

        private IPolarField _field;
        private PolarWeaponData _weaponData;
        private bool _isRetracting;
        private float _currentLength;
        private float _nextTickTime;
        private Vector2 _direction;
        private Vector2 _origin;

        public void Fire(IPolarField field, PolarWeaponData data, Vector2 origin, Vector2 direction)
        {
            _field = field;
            _weaponData = data;
            _origin = origin;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            _isRetracting = false;
            _currentLength = 0f;
            _nextTickTime = Time.time;
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.startColor = beamColor;
                lineRenderer.endColor = beamColor;
                lineRenderer.positionCount = 2;
            }
        }

        public void SetPolarContext(IPolarField field, float knockbackPower, float woundIntensity)
        {
            _field = field;
            polarKnockbackPower = knockbackPower;
            polarWoundIntensity = woundIntensity;
        }

        private void Awake()
        {
            if (_field == null && polarFieldBehaviour != null)
            {
                _field = polarFieldBehaviour as IPolarField;
                if (_field == null)
                {
                    Debug.LogWarning("[PolarBeamProjectile] polarFieldBehaviour는 IPolarField를 구현해야 합니다.");
                }
            }
        }

        private void Update()
        {
            if (_field == null || _weaponData == null) return;
            float dt = Time.deltaTime;

            if (_isRetracting)
            {
                _currentLength = Mathf.Max(0f, _currentLength - retractSpeed * dt);
                if (_currentLength <= 0f)
                {
                    if (lineRenderer != null) lineRenderer.enabled = false;
                    enabled = false;
                    return;
                }
            }
            else
            {
                _currentLength = Mathf.Min(maxLength, _currentLength + extendSpeed * dt);
            }

            Vector2 end = _origin + _direction * _currentLength;
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, _origin);
                lineRenderer.SetPosition(1, end);
            }

            if (Time.time >= _nextTickTime)
            {
                ApplyTickDamage(end);
                _nextTickTime = Time.time + 1f / Mathf.Max(0.0001f, _weaponData.TickRate);
            }
        }

        public void BeginRetract()
        {
            _isRetracting = true;
        }

        private void ApplyTickDamage(Vector2 hitPoint)
        {
            if (_field == null || _weaponData == null) return;
            Vector2 center = (_field as Component) != null ? (Vector2)((Component)_field).transform.position : Vector2.zero;
            Vector2 dir = hitPoint - center;
            if (dir.sqrMagnitude <= Mathf.Epsilon) return;
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            int sectorIndex = _field.AngleToSectorIndex(angleDeg);
            _field.SetLastWeaponKnockback(polarKnockbackPower);
            _field.ApplyDamageToSector(sectorIndex, _weaponData.Damage / Mathf.Max(0.0001f, _weaponData.TickRate));
            if (_field.EnableWoundSystem)
            {
                _field.ApplyWound(sectorIndex, polarWoundIntensity);
            }
        }
    }
}
