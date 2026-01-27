using Script.SystemCore.Pool;
using Script.SystemCore.Resource;
using System;
using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 플레이어 전용 Polar 무기 관리자
    /// - WeaponData.WeaponBundleId로 무기 프리팹 로드
    /// - 프리팹에 PolarWeaponBase 컴포넌트가 사전에 붙어있어야 함
    ///   (PolarLaserWeapon 또는 PolarProjectileWeapon)
    /// </summary>
    public sealed class PlayerWeaponManager : MonoBehaviour
    {
        [Header("Field")]
        [SerializeField] private MonoBehaviour polarFieldBehaviour;

        [Header("Weapon Settings")]
        [SerializeField] private PolarWeaponData defaultWeaponData;
        [SerializeField] private PolarWeaponDataTable dataTable;
        [SerializeField] private string defaultWeaponId;
        [SerializeField] private float defaultStartRadius = 0.8f;
        [SerializeField] private Transform weaponSlot;

        private IPolarField _field;
        private PolarWeaponData _currentWeaponData;
        private string _currentWeaponId;
        private PolarWeaponBase _currentWeapon;

        private void Start()
        {
            Init();
        }

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

        private void LoadWeapon(PolarWeaponData data)
        {
            // 기존 무기 제거
            if (_currentWeapon != null)
            {
                Destroy(_currentWeapon.gameObject);
                _currentWeapon = null;
            }

            if (data == null || string.IsNullOrEmpty(data.WeaponBundleId))
            {
                Debug.LogWarning("[PlayerWeaponManager] WeaponData or WeaponBundleId is null!");
                return;
            }

            // 프리팹 로드 (이미 PolarWeaponBase 컴포넌트가 붙어있음)
            var prefab = ResourceService.Instance?.LoadPrefab(data.WeaponBundleId);
            if (prefab == null)
            {
                Debug.LogError($"[PlayerWeaponManager] Failed to load weapon prefab: {data.WeaponBundleId}");
                return;
            }

            // 인스턴스화
            var parent = weaponSlot != null ? weaponSlot : transform;
            GameObject weaponObj = Instantiate(prefab, parent);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.identity;

            // PolarWeaponBase 컴포넌트 확인
            _currentWeapon = weaponObj.GetComponent<PolarWeaponBase>();
            if (_currentWeapon == null)
            {
                Debug.LogError($"[PlayerWeaponManager] Weapon prefab {data.WeaponBundleId} does not have PolarWeaponBase component!");
                Destroy(weaponObj);
                return;
            }

            // 초기화
            _currentWeapon.Initialize(_field, data);
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
        /// 무기 데이터 교체
        /// </summary>
        public void SetWeaponData(PolarWeaponData data)
        {
            defaultWeaponData = data;
            _currentWeaponData = data;
            _currentWeaponId = data != null ? data.Id : defaultWeaponId;

            if (_currentWeaponData == null)
            {
                Debug.LogWarning("[PlayerWeaponManager] SetWeaponData: data is null");
                return;
            }

            // 런타임 조립 데이터가 WeaponBundleId를 안 갖는 경우도 허용
            if (string.IsNullOrEmpty(_currentWeaponData.WeaponBundleId))
            {
                if (_currentWeapon != null)
                {
                    _currentWeapon.Initialize(_field, _currentWeaponData);
                }
                else
                {
                    Debug.LogWarning("[PlayerWeaponManager] SetWeaponData: WeaponBundleId is empty and no current weapon instance exists.");
                }
                return;
            }

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
        /// 투사체 발사
        /// </summary>
        public void FireProjectile(float angleDeg, float? startRadius = null)
        {
            if (_currentWeapon == null) return;
            
            float radius = startRadius ?? defaultStartRadius;
            _currentWeapon.Fire(angleDeg, radius);
        }

        /// <summary>
        /// 레이저 발사 시작
        /// </summary>
        public void StartFireLaser()
        {
            if (_currentWeapon is PolarLaserWeapon laser)
            {
                laser.StartFire();
            }
        }

        /// <summary>
        /// 레이저 발사 중지
        /// </summary>
        public void StopFireLaser()
        {
            if (_currentWeapon is PolarLaserWeapon laser)
            {
                laser.StopFire();
            }
        }

        /// <summary>
        /// 현재 장착된 무기 데이터
        /// </summary>
        public PolarWeaponData CurrentWeaponData => _currentWeaponData;

        /// <summary>
        /// 현재 장착된 무기 인스턴스
        /// </summary>
        public PolarWeaponBase CurrentWeapon => _currentWeapon;

        /// <summary>
        /// 런타임에 조립된 무기(WeaponData)를 즉시 장착
        /// - WeaponBundleId가 있으면 프리팹을 로드해서 장착
        /// - WeaponBundleId가 없으면: 현재 장착된 무기가 있으면 그 무기에 data만 주입 (발사 테스트용)
        /// </summary>
        public void EquipRuntimeWeapon(PolarWeaponData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[PlayerWeaponManager] EquipRuntimeWeapon: data is null");
                return;
            }

            _currentWeaponData = data;
            _currentWeaponId = data.Id;

            if (string.IsNullOrEmpty(data.WeaponBundleId))
            {
                if (_currentWeapon != null)
                {
                    _currentWeapon.Initialize(_field, data);
                }
                else
                {
                    Debug.LogWarning("[PlayerWeaponManager] EquipRuntimeWeapon: WeaponBundleId is empty and no current weapon instance exists.");
                }
                return;
            }

            LoadWeapon(_currentWeaponData);
        }

        /// <summary>
        /// 런타임에 생성/조립된 무기 인스턴스를 PlayerWeaponManager가 '정식'으로 관리하도록 등록
        /// - 기존 무기 제거
        /// - weaponSlot 아래로 배치
        /// - _field / _currentWeapon / _currentWeaponData / _currentWeaponId 동기화
        /// - weapon.Initialize(field, data) 호출
        /// </summary>
        public void RegisterRuntimeWeaponInstance(PolarWeaponBase weapon, IPolarField field, PolarWeaponData data)
        {
            if (weapon == null)
            {
                Debug.LogWarning("[PlayerWeaponManager] RegisterRuntimeWeaponInstance: weapon is null");
                return;
            }

            if (field == null)
            {
                Debug.LogWarning("[PlayerWeaponManager] RegisterRuntimeWeaponInstance: field is null");
                return;
            }

            if (data == null)
            {
                Debug.LogWarning("[PlayerWeaponManager] RegisterRuntimeWeaponInstance: data is null");
                return;
            }

            // 기존 무기 제거
            if (_currentWeapon != null)
            {
                Destroy(_currentWeapon.gameObject);
                _currentWeapon = null;
            }

            _field = field;
            _currentWeaponData = data;
            _currentWeaponId = data.Id;

            // parent 정리
            var parent = weaponSlot != null ? weaponSlot : transform;
            weapon.transform.SetParent(parent, false);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;

            _currentWeapon = weapon;
            _currentWeapon.Initialize(_field, _currentWeaponData);
        }

        /// <summary>
        /// 런타임 WeaponData만 교체 (무기 인스턴스는 유지)
        /// - 이미 _currentWeapon이 있을 때만 동작
        /// - field가 null이면 현재 field를 재사용
        /// </summary>
        public void SwapRuntimeWeaponData(PolarWeaponData data, IPolarField field = null)
        {
            if (data == null)
            {
                Debug.LogWarning("[PlayerWeaponManager] SwapRuntimeWeaponData: data is null");
                return;
            }

            if (_currentWeapon == null)
            {
                Debug.LogWarning("[PlayerWeaponManager] SwapRuntimeWeaponData: no current weapon instance");
                return;
            }

            if (field != null) _field = field;

            _currentWeaponData = data;
            _currentWeaponId = data.Id;

            _currentWeapon.Initialize(_field, _currentWeaponData);
        }

        /// <summary>
        /// 무기 프리팹을 런타임에 ResourceService에 등록하고, 해당 WeaponData의 WeaponBundleId로 연결합니다.
        /// - overwrite=false면 동일 ID가 있으면 실패
        /// - autoId=true면 충돌 방지용 runtime:// ID를 자동 생성합니다.
        /// - registerToPool=true면 PoolService에도 동일 ID로 등록해, 무기 자체 풀링 스폰이 가능해집니다.
        /// </summary>
        public string RegisterRuntimeWeaponPrefab(
            PolarWeaponData data,
            GameObject weaponPrefabTemplate,
            string bundleId = null,
            bool autoId = true,
            bool overwrite = false,
            bool registerToPool = true,
            PoolConfig poolConfig = null)
        {
            if (data == null)
            {
                Debug.LogWarning("[PlayerWeaponManager] RegisterRuntimeWeaponPrefab: data is null");
                return null;
            }

            if (weaponPrefabTemplate == null)
            {
                Debug.LogWarning("[PlayerWeaponManager] RegisterRuntimeWeaponPrefab: weaponPrefabTemplate is null");
                return null;
            }

            var rs = ResourceService.Instance;
            if (rs == null)
            {
                Debug.LogWarning("[PlayerWeaponManager] RegisterRuntimeWeaponPrefab: ResourceService.Instance is null");
                return null;
            }

            // 템플릿은 런타임 루트로 옮겨지며 비활성화되므로, 외부에서 넘긴 오브젝트는 '템플릿' 용도여야 함
            string id;
            if (autoId)
            {
                id = rs.RegisterRuntimePrefabAutoId(weaponPrefabTemplate, data.WeaponName);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(bundleId))
                {
                    Debug.LogWarning("[PlayerWeaponManager] RegisterRuntimeWeaponPrefab: bundleId required when autoId=false");
                    return null;
                }

                id = bundleId;
                bool ok = rs.RegisterRuntimePrefab(id, weaponPrefabTemplate, overwrite: overwrite);
                if (!ok)
                {
                    Debug.LogWarning($"[PlayerWeaponManager] RegisterRuntimeWeaponPrefab failed: {id}");
                    return null;
                }
            }

            // WeaponData의 private weaponBundleId에 주입
            SetPrivateField(data, "weaponBundleId", id);

            if (registerToPool)
            {
                var pool = PoolService.Instance;
                if (pool == null)
                {
                    Debug.LogWarning("[PlayerWeaponManager] RegisterRuntimeWeaponPrefab: PoolService.Instance is null (skip pool register)");
                }
                else
                {
                    pool.RegisterPrefab(id, weaponPrefabTemplate, poolConfig, overwrite: overwrite);
                }
            }

            return id;
        }

        /// <summary>
        /// 런타임에 생성된 무기 프리팹(템플릿)을 등록하고 즉시 장착까지 수행합니다.
        /// </summary>
        public bool RegisterAndEquipRuntimeWeaponPrefab(
            PolarWeaponData data,
            GameObject weaponPrefabTemplate,
            IPolarField field = null,
            string bundleId = null,
            bool autoId = true,
            bool overwrite = false,
            bool registerToPool = true,
            PoolConfig poolConfig = null)
        {
            var id = RegisterRuntimeWeaponPrefab(data, weaponPrefabTemplate, bundleId, autoId, overwrite, registerToPool, poolConfig);
            if (string.IsNullOrEmpty(id)) return false;

            if (field != null) _field = field;
            EquipRuntimeWeapon(data);
            return true;
        }

        /// <summary>
        /// 무기 프리팹을 풀에서 스폰하여 장착합니다.
        /// - WeaponBundleId가 PoolService에 등록되어 있어야 합니다.
        /// - ResourceService 경로(LoadWeapon) 대신 무기 자체 풀링을 쓰고 싶을 때 사용.
        /// </summary>
        public bool EquipWeaponFromPool(PolarWeaponData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[PlayerWeaponManager] EquipWeaponFromPool: data is null");
                return false;
            }

            if (PoolService.Instance == null)
            {
                Debug.LogWarning("[PlayerWeaponManager] EquipWeaponFromPool: PoolService.Instance is null");
                return false;
            }

            if (string.IsNullOrEmpty(data.WeaponBundleId))
            {
                Debug.LogWarning("[PlayerWeaponManager] EquipWeaponFromPool: WeaponBundleId is empty");
                return false;
            }

            // 기존 무기 제거
            if (_currentWeapon != null)
            {
                Destroy(_currentWeapon.gameObject);
                _currentWeapon = null;
            }

            _currentWeaponData = data;
            _currentWeaponId = data.Id;

            var parent = weaponSlot != null ? weaponSlot : transform;

            // 무기 GameObject는 PoolService가 Component로 반환할 수 있으니, PolarWeaponBase로 요청
            var weapon = PoolService.Instance.Get<PolarWeaponBase>(data.WeaponBundleId, Vector3.zero, Quaternion.identity);
            if (weapon == null)
            {
                Debug.LogWarning($"[PlayerWeaponManager] EquipWeaponFromPool: failed to get weapon from pool: {data.WeaponBundleId}");
                return false;
            }

            weapon.transform.SetParent(parent, false);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;

            _currentWeapon = weapon;
            _currentWeapon.Initialize(_field, data);
            return true;
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            if (obj == null || string.IsNullOrEmpty(fieldName)) return;

            var type = obj.GetType();
            while (type != null)
            {
                var f = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null)
                {
                    f.SetValue(obj, value);
                    return;
                }
                type = type.BaseType;
            }
        }
    }
}
