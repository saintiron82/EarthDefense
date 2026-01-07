using UnityEngine;
using Script.SystemCore.Resource;

namespace Polar.Weapons
{
    /// <summary>
    /// 플레이어 전용 Polar 무기 관리자.
    /// - IPolarField를 주입받아 PolarWeapon에 전달
    /// - 무기 데이터 교체 및 발사(투사체/빔) 래퍼 제공
    /// ShapeDefense WeaponController 스타일의 ID/리소스 로드를 일부 반영
    /// </summary>
    public sealed class PlayerWeaponManager : MonoBehaviour
    {
        [Header("Field")]
        [SerializeField] private MonoBehaviour polarFieldBehaviour; // IPolarField 구현체 할당

        [Header("Weapon")]
        [SerializeField] private PolarWeapon weapon; // 인스펙터 지정 시 그대로 사용, 없으면 번들로 로드
        [SerializeField] private Transform weaponSlot; // 비워두면 자기 Transform
        [SerializeField] private PolarWeaponData defaultWeaponData;
        [SerializeField] private PolarWeaponDataTable dataTable;
        [SerializeField] private string defaultWeaponId;
        [SerializeField] private bool usePool = false;
        [SerializeField, Min(0f)] private float defaultStartRadius = 0.8f;

        [Header("Beam")]
        [SerializeField] private Transform beamOrigin;

        private IPolarField _field;
        private PolarWeaponData _currentWeaponData;
        private string _currentWeaponId;

        private void Awake()
        {
            if (polarFieldBehaviour != null)
            {
                _field = polarFieldBehaviour as IPolarField;
                if (_field == null)
                {
                    Debug.LogWarning("[PlayerWeaponManager] polarFieldBehaviour는 IPolarField를 구현해야 합니다.");
                }
            }

            if (weapon != null && _field != null)
            {
                var data = ResolveWeaponData(defaultWeaponData, defaultWeaponId);
                _currentWeaponData = data;
                _currentWeaponId = data != null ? data.Id : defaultWeaponId;
                LoadWeapon(_currentWeaponData);
            }
            else
            {
                // 인스펙터 weapon이 없으면 번들로 로드 시도
                var data = ResolveWeaponData(defaultWeaponData, defaultWeaponId);
                _currentWeaponData = data;
                _currentWeaponId = data != null ? data.Id : defaultWeaponId;
                if (_field != null)
                {
                    LoadWeapon(_currentWeaponData);
                }
            }
        }

        private PolarWeaponData ResolveWeaponData(PolarWeaponData overrideData, string id)
        {
            if (overrideData != null) return overrideData;
            if (dataTable != null && !string.IsNullOrEmpty(id))
            {
                var data = dataTable.GetById(id);
                if (data != null) return data;
            }
            if (dataTable != null && !string.IsNullOrEmpty(defaultWeaponId))
            {
                var data = dataTable.GetById(defaultWeaponId);
                if (data != null) return data;
            }
            return defaultWeaponData; // fallback
        }

        private void LoadWeapon(PolarWeaponData data)
        {
            if (_field == null) return;

            // 기존 무기 제거 (인스펙터 상주형이면 건너뜀)
            if (weapon != null && weapon.gameObject.scene.IsValid())
            {
                Destroy(weapon.gameObject);
                weapon = null;
            }

            // WeaponBundleId로 프리팹 로드
            if (data != null && ResourceService.Instance != null && !string.IsNullOrEmpty(data.WeaponBundleId))
            {
                var prefab = ResourceService.Instance.LoadPrefab(data.WeaponBundleId);
                if (prefab != null)
                {
                    var parent = weaponSlot != null ? weaponSlot : transform;
                    var go = Instantiate(prefab, parent);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    weapon = go.GetComponent<PolarWeapon>() ?? go.AddComponent<PolarWeapon>();
                }
            }

            // 번들 로드 실패 시 인스펙터 weapon 사용
            if (weapon == null)
            {
                weapon = GetComponentInChildren<PolarWeapon>();
                if (weapon == null)
                {
                    Debug.LogError("[PlayerWeaponManager] Weapon not found. Assign weapon or set WeaponBundleId.");
                    return;
                }
            }

            weapon.Initialize(_field, data, usePool);
        }

        /// <summary>
        /// 외부에서 필드/무기/데이터를 주입해 초기화.
        /// </summary>
        public void Initialize(IPolarField field, PolarWeapon weaponInstance = null, PolarWeaponData weaponData = null, string weaponId = null, bool? usePoolOverride = null)
        {
            _field = field;
            if (weaponInstance != null) weapon = weaponInstance;
            if (usePoolOverride.HasValue) usePool = usePoolOverride.Value;
            _currentWeaponData = ResolveWeaponData(weaponData, weaponId ?? defaultWeaponId);
            _currentWeaponId = _currentWeaponData != null ? _currentWeaponData.Id : weaponId ?? defaultWeaponId;
            LoadWeapon(_currentWeaponData);
        }

        /// <summary>
        /// 무기 데이터 교체.
        /// </summary>
        public void SetWeaponData(PolarWeaponData data)
        {
            defaultWeaponData = data;
            _currentWeaponData = data;
            _currentWeaponId = data != null ? data.Id : defaultWeaponId;
            LoadWeapon(_currentWeaponData);
        }

        /// <summary>
        /// 무기 ID 교체(테이블 조회).
        /// </summary>
        public void SetWeaponId(string id)
        {
            _currentWeaponId = id;
            _currentWeaponData = ResolveWeaponData(null, id);
            LoadWeapon(_currentWeaponData);
        }

        /// <summary>
        /// 다음 무기로 전환(데이터 테이블 순회).
        /// </summary>
        public void NextWeapon()
        {
            if (dataTable == null || dataTable.Weapons.Count == 0) return;
            var list = dataTable.Weapons;
            int idx = 0;
            if (!string.IsNullOrEmpty(_currentWeaponId))
            {
                idx = list.FindIndex(w => w != null && w.Id == _currentWeaponId);
                if (idx < 0) idx = 0;
            }
            int next = (idx + 1) % list.Count;
            var data = list[next];
            if (data != null)
            {
                _currentWeaponData = data;
                _currentWeaponId = data.Id;
                LoadWeapon(_currentWeaponData);
            }
        }

        /// <summary>
        /// 투사체 발사(Polar 섹터 각도 기준).
        /// </summary>
        public void FireProjectile(float angleDeg, float? startRadius = null)
        {
            if (weapon == null || _field == null) return;
            float radius = startRadius ?? defaultStartRadius;
            weapon.FireProjectile(angleDeg, radius);
        }

        /// <summary>
        /// 빔 발사(월드 방향 벡터 사용).
        /// </summary>
        public void FireBeam(Vector2 direction)
        {
            if (weapon == null || _field == null) return;
            Vector2 origin = beamOrigin != null ? beamOrigin.position : transform.position;
            weapon.FireBeam(origin, direction);
        }
    }
}
