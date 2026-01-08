using UnityEngine;
using Script.SystemCore.Resource;

namespace Polar.Weapons
{
    /// <summary>
    /// 플레이어 전용 Polar 무기 관리자 (타입별 자동 로딩 + SGSystem 통합 + Arm 방식)
    /// - IPolarField를 주입받아 무기에 전달
    /// - 무기 데이터 타입에 따라 적절한 무기 클래스 자동 생성
    /// - 무기 교체 및 발사 래퍼 제공
    /// - SGSystem: PoolService 전용
    /// - Arm: 조준 업데이트 지원 (선택 사항)
    /// </summary>
    public sealed class PlayerWeaponManager : MonoBehaviour
    {
        [Header("Field")]
        [SerializeField] private MonoBehaviour polarFieldBehaviour;

        [Header("Weapon")]
        [SerializeField] private Transform weaponSlot;
        [SerializeField] private PolarWeaponData defaultWeaponData;
        [SerializeField] private PolarWeaponDataTable dataTable;
        [SerializeField] private string defaultWeaponId;

        [Header("Aim Settings")]
        [Tooltip("조준 모드 (PolarAngle: 각도 직접, MouseFollow: 마우스 추적)")]
        [SerializeField] private bool enableMouseAim = false;

        private IPolarField _field;
        private PolarWeaponBase _currentWeapon;
        private PolarWeaponData _currentWeaponData;
        private string _currentWeaponId;


        public void Init()
        {
            if (polarFieldBehaviour != null)
            {
                _field = polarFieldBehaviour as IPolarField;
                if (_field == null)
                {
                    Debug.LogWarning("[PlayerWeaponManager] polarFieldBehaviour는 IPolarField를 구현해야 합니다.");
                }
            }

            if (_field != null)
            {
                var data = ResolveWeaponData(defaultWeaponData, defaultWeaponId);
                _currentWeaponData = data;
                _currentWeaponId = data != null ? data.Id : defaultWeaponId;
                LoadWeapon(_currentWeaponData);
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
            return defaultWeaponData;
        }

        /// <summary>
        /// 무기 로딩 (타입별 자동 생성)
        /// </summary>
        private void LoadWeapon(PolarWeaponData data)
        {
            if (_field == null) return;

            // 기존 무기 제거
            if (_currentWeapon != null)
            {
                Destroy(_currentWeapon.gameObject);
                _currentWeapon = null;
            }

            if (data == null) return;

            // WeaponBundleId로 프리팹 로드 시도
            GameObject weaponObj = null;
            if (ResourceService.Instance != null && !string.IsNullOrEmpty(data.WeaponBundleId))
            {
                var prefab = ResourceService.Instance.LoadPrefab(data.WeaponBundleId);
                if (prefab != null)
                {
                    var parent = weaponSlot != null ? weaponSlot : transform;
                    weaponObj = Instantiate(prefab, parent);
                    weaponObj.transform.localPosition = Vector3.zero;
                    weaponObj.transform.localRotation = Quaternion.identity;
                }
            }

            // 프리팹 로드 실패 시 타입별 동적 생성
            if (weaponObj == null)
            {
                var parent = weaponSlot != null ? weaponSlot : transform;
                weaponObj = new GameObject($"Weapon_{data.WeaponName}");
                weaponObj.transform.SetParent(parent);
                weaponObj.transform.localPosition = Vector3.zero;
                weaponObj.transform.localRotation = Quaternion.identity;
            }

            // 데이터 타입에 따라 적절한 무기 컴포넌트 추가
            _currentWeapon = CreateWeaponByType(weaponObj, data);

            if (_currentWeapon != null)
            {
                _currentWeapon.Initialize(_field, data);
            }
            else
            {
                Debug.LogError($"[PlayerWeaponManager] Failed to create weapon for type: {data.GetType().Name}");
            }
        }

        /// <summary>
        /// 데이터 타입에 따라 무기 컴포넌트 생성
        /// </summary>
        private PolarWeaponBase CreateWeaponByType(GameObject weaponObj, PolarWeaponData data)
        {
            // 기존 무기 컴포넌트 확인
            var existing = weaponObj.GetComponent<PolarWeaponBase>();
            if (existing != null) return existing;

            // 타입별 생성
            if (data is PolarLaserWeaponData)
            {
                return weaponObj.AddComponent<PolarLaserWeapon>();
            }
            else if (data is PolarMachinegunWeaponData)
            {
                return weaponObj.AddComponent<PolarMachinegunWeapon>();
            }
            else if (data is PolarMissileWeaponData)
            {
                return weaponObj.AddComponent<PolarMissileWeapon>();
            }
            else
            {
                Debug.LogWarning($"[PlayerWeaponManager] Unknown weapon data type: {data.GetType().Name}.");
                return null;
            }
        }

        /// <summary>
        /// 외부에서 필드/무기/데이터를 주입해 초기화
        /// </summary>
        public void Initialize(IPolarField field, PolarWeaponData weaponData = null, string weaponId = null)
        {
            _field = field;
            _currentWeaponData = ResolveWeaponData(weaponData, weaponId ?? defaultWeaponId);
            _currentWeaponId = _currentWeaponData != null ? _currentWeaponData.Id : weaponId ?? defaultWeaponId;
            LoadWeapon(_currentWeaponData);
        }

        /// <summary>
        /// 조준 업데이트 (선택 사항)
        /// - MouseFollow 모드에서 사용
        /// </summary>
        public void UpdateAim(Vector2 worldPosition)
        {
            if (_currentWeapon == null || !enableMouseAim) return;
            _currentWeapon.UpdateAim(worldPosition);
        }

        /// <summary>
        /// 각도로 조준 업데이트 (극좌표 전용)
        /// - PolarAngle 모드에서 사용
        /// </summary>
        public void UpdateAimAngle(float angleInDegrees)
        {
            if (_currentWeapon == null) return;
            _currentWeapon.UpdateAimAngle(angleInDegrees);
        }

        /// <summary>
        /// 무기 데이터 교체
        /// </summary>
        public void SetWeaponData(PolarWeaponData data)
        {
            defaultWeaponData = data;
            _currentWeaponData = data;
            _currentWeaponId = data != null ? data.Id : defaultWeaponId;
            LoadWeapon(_currentWeaponData);
        }

        /// <summary>
        /// 무기 ID 교체 (테이블 조회)
        /// </summary>
        public void SetWeaponId(string id)
        {
            _currentWeaponId = id;
            _currentWeaponData = ResolveWeaponData(null, id);
            LoadWeapon(_currentWeaponData);
        }

        /// <summary>
        /// 다음 무기로 전환 (데이터 테이블 순회)
        /// </summary>
        public void NextWeapon()
        {
            if (dataTable == null || dataTable.Weapons.Count == 0) return;
            var list = dataTable.Weapons;
            int idx = 0;
            if (!string.IsNullOrEmpty(_currentWeaponId))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null && list[i].Id == _currentWeaponId)
                    {
                        idx = i;
                        break;
                    }
                }
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
        /// 무기 발사
        /// </summary>
        public void Fire()
        {
            if (_currentWeapon == null) return;
            _currentWeapon.Fire();
        }

        /// <summary>
        /// 특정 각도로 발사 (머신건/미사일용)
        /// </summary>
        public void Fire(float angleDeg)
        {
            if (_currentWeapon == null) return;

            // 타입별 분기
            if (_currentWeapon is PolarMachinegunWeapon machinegun)
            {
                machinegun.Fire(angleDeg);
            }
            else if (_currentWeapon is PolarMissileWeapon missile)
            {
                missile.Fire(angleDeg);
            }
            else
            {
                _currentWeapon.Fire();
            }
        }

        /// <summary>
        /// 발사 중지 (레이저용)
        /// </summary>
        public void StopFire()
        {
            if (_currentWeapon is PolarLaserWeapon laser)
            {
                laser.StopFire();
            }
        }
    }
}
