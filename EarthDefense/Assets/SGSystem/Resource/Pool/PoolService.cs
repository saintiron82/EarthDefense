using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
using Script.SystemCore.Resource;

namespace Script.SystemCore.Pool
{
    public class PoolService : SG.MonoServiceBase
    {
        public static PoolService Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private ResourceService _resourceService;

        [Header("Global Fallback Settings")]
        [Tooltip("프리셋이 없을 때 사용할 기본 풀 설정")]
        public PoolConfig fallbackPoolConfig = new PoolConfig
        {
            enabled = true,
            preloadCount = 10,
            maxPoolSize = 50,
            autoExpand = true
        };

        // "bundleId|TypeFullName" → IObjectPool<T> (object로 저장)
        private Dictionary<string, object> _typedPools;
        private Dictionary<string, GameObject> _prefabs;
        private Dictionary<string, PoolConfig> _configs;
        private Transform _poolRoot;
        private bool _initialized;

        private const string RuntimeBundlePrefix = "runtime://";

        public override async UniTask<bool> Init()
        {
            await base.Init();
            Instance = this;
            return true;
        }

        public override void DirectInit()
        {
            base.DirectInit();
            Initialize();
        }

        public override async UniTask<bool> Prepare()
        {
            await base.Prepare();
            return true;
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _typedPools = new Dictionary<string, object>();
            _prefabs = new Dictionary<string, GameObject>();
            _configs = new Dictionary<string, PoolConfig>();

            _poolRoot = transform;

            _initialized = true;
        }

        public void SetResourceService(ResourceService service)
        {
            _resourceService = service;
        }

        #region Get API

        /// <summary>
        /// 타입별 풀에서 컴포넌트 직접 반환. GetComponent 호출 없음.
        /// </summary>
        public T Get<T>(string bundleId) where T : Component
        {
            return Get<T>(bundleId, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// 타입별 풀에서 컴포넌트 직접 반환. GetComponent 호출 없음.
        /// </summary>
        public T Get<T>(string bundleId, Vector3 position, Quaternion rotation) where T : Component
        {
            Initialize();

            var poolKey = GetPoolKey<T>(bundleId);

            if (!_typedPools.TryGetValue(poolKey, out var poolObj))
            {
                poolObj = CreateTypedPool<T>(bundleId, poolKey);
                if (poolObj == null)
                {
                    return null;
                }
            }

            var pool = (IObjectPool<T>)poolObj;
            var component = pool.Get();

            if (component != null)
            {
                component.transform.position = position;
                component.transform.rotation = rotation;
            }

            return component;
        }

        /// <summary>
        /// GameObject 반환이 필요한 경우 Get&lt;Transform&gt; 사용 후 .gameObject 접근
        /// </summary>
        public GameObject Get(string bundleId)
        {
            return Get<Transform>(bundleId)?.gameObject;
        }

        public GameObject Get(string bundleId, Vector3 position, Quaternion rotation)
        {
            return Get<Transform>(bundleId, position, rotation)?.gameObject;
        }

        #endregion

        #region Return API

        /// <summary>
        /// IPoolable 컴포넌트를 풀에 반환. PoolBundleId를 자동으로 사용.
        /// 실제 런타임 타입을 자동으로 감지하여 올바른 풀에 반환.
        /// </summary>
        public void Return<T>(T component) where T : Component, IPoolable
        {
            if (component == null) return;
            
            // 실제 런타임 타입으로 반환 시도
            var actualType = component.GetType();
            var poolKey = $"{component.PoolBundleId}|{actualType.FullName}";
            
            if (_typedPools.TryGetValue(poolKey, out var poolObj))
            {
                // Reflection으로 Release 호출
                var releaseMethod = poolObj.GetType().GetMethod("Release");
                if (releaseMethod != null)
                {
                    releaseMethod.Invoke(poolObj, new object[] { component });
                    return;
                }
            }
            
            // Fallback: 선언된 타입으로 시도
            Return(component.PoolBundleId, component);
        }

        /// <summary>
        /// 컴포넌트를 풀에 반환. bundleId를 명시해야 함.
        /// </summary>
        public void Return<T>(string bundleId, T component) where T : Component
        {
            if (component == null) return;

            var poolKey = GetPoolKey<T>(bundleId);
            if (_typedPools.TryGetValue(poolKey, out var poolObj))
            {
                var pool = (IObjectPool<T>)poolObj;
                pool.Release(component);
            }
            else
            {
                Debug.LogWarning($"[PoolService] Pool not found: {poolKey}");
                UnityEngine.Object.Destroy(component.gameObject);
            }
        }

        /// <summary>
        /// GameObject를 통해 반환 (내부적으로 Transform 풀 사용)
        /// </summary>
        public void Return(string bundleId, GameObject obj)
        {
            if (obj == null) return;
            Return(bundleId, obj.transform);
        }

        #endregion

        #region Preload API

        public void Preload<T>(string bundleId, int count) where T : Component
        {
            Initialize();

            var poolKey = GetPoolKey<T>(bundleId);

            if (!_typedPools.TryGetValue(poolKey, out var poolObj))
            {
                poolObj = CreateTypedPool<T>(bundleId, poolKey);
                if (poolObj == null) return;
            }

            var pool = (IObjectPool<T>)poolObj;
            var tempList = new List<T>(count);

            for (int i = 0; i < count; i++)
            {
                tempList.Add(pool.Get());
            }

            foreach (var item in tempList)
            {
                pool.Release(item);
            }
        }

        public async UniTask PreloadAsync<T>(string bundleId, int count, Action onComplete = null) where T : Component
        {
            Initialize();

            var poolKey = GetPoolKey<T>(bundleId);

            if (!_typedPools.TryGetValue(poolKey, out var poolObj))
            {
                poolObj = CreateTypedPool<T>(bundleId, poolKey);
                if (poolObj == null)
                {
                    onComplete?.Invoke();
                    return;
                }
            }

            var pool = (IObjectPool<T>)poolObj;
            var tempList = new List<T>(count);

            for (int i = 0; i < count; i++)
            {
                tempList.Add(pool.Get());

                if (i % 10 == 0)
                {
                    await UniTask.Yield();
                }
            }

            foreach (var item in tempList)
            {
                pool.Release(item);
            }

            onComplete?.Invoke();
        }

        #endregion

        #region Clear API

        public void Clear<T>(string bundleId) where T : Component
        {
            var poolKey = GetPoolKey<T>(bundleId);

            if (_typedPools.TryGetValue(poolKey, out var poolObj))
            {
                var pool = (IObjectPool<T>)poolObj;
                pool.Clear();
                _typedPools.Remove(poolKey);

                // 해당 번들의 다른 타입 풀이 없으면 프리팹/설정 해제
                CleanupBundleIfEmpty(bundleId);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[PoolService] Pool cleared: {poolKey}");
#endif
            }
        }

        public void ClearAll()
        {
            if (_typedPools == null) return;

            foreach (var kvp in _typedPools)
            {
                // Reflection으로 Clear 호출
                var clearMethod = kvp.Value.GetType().GetMethod("Clear");
                clearMethod?.Invoke(kvp.Value, null);
            }

            _typedPools.Clear();
            _prefabs.Clear();
            _configs.Clear();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[PoolService] All pools cleared");
#endif
        }

        #endregion

        #region Query API

        /// <summary>
        /// 타입별 풀 직접 접근. 배치 처리, 커스텀 로직에 유용.
        /// </summary>
        public IObjectPool<T> GetPool<T>(string bundleId) where T : Component
        {
            Initialize();

            var poolKey = GetPoolKey<T>(bundleId);

            if (!_typedPools.TryGetValue(poolKey, out var poolObj))
            {
                poolObj = CreateTypedPool<T>(bundleId, poolKey);
            }

            return poolObj as IObjectPool<T>;
        }

        public bool Contains<T>(string bundleId) where T : Component
        {
            return _typedPools.ContainsKey(GetPoolKey<T>(bundleId));
        }

        public int GetActiveCount<T>(string bundleId) where T : Component
        {
            var poolKey = GetPoolKey<T>(bundleId);
            if (_typedPools.TryGetValue(poolKey, out var poolObj) && poolObj is ObjectPool<T> pool)
            {
                return pool.CountActive;
            }
            return 0;
        }

        public int GetInactiveCount<T>(string bundleId) where T : Component
        {
            var poolKey = GetPoolKey<T>(bundleId);
            if (_typedPools.TryGetValue(poolKey, out var poolObj) && poolObj is ObjectPool<T> pool)
            {
                return pool.CountInactive;
            }
            return 0;
        }

        #endregion

        #region Internal

        private string GetPoolKey<T>(string bundleId)
        {
            return $"{bundleId}|{typeof(T).FullName}";
        }

        private object CreateTypedPool<T>(string bundleId, string poolKey) where T : Component
        {
            // 동적 등록 prefab이 있으면 ResourceService 없이도 허용
            if (!_prefabs.TryGetValue(bundleId, out var prefab))
            {
                if (_resourceService == null)
                {
                    Debug.LogError("[PoolService] ResourceService is not set");
                    return null;
                }

                prefab = _resourceService.LoadPrefab(bundleId);
                if (prefab == null)
                {
                    Debug.LogError($"[PoolService] Failed to load prefab: {bundleId}");
                    return null;
                }
                _prefabs[bundleId] = prefab;
            }

            // 프리팹에서 T 컴포넌트 확인
            var prefabComponent = prefab.GetComponent<T>();
            if (prefabComponent == null)
            {
                Debug.LogError($"[PoolService] Prefab {bundleId} does not have component {typeof(T).Name}");
                return null;
            }

            // 풀 설정
            if (!_configs.TryGetValue(bundleId, out var poolConfig))
            {
                poolConfig = _resourceService != null ? _resourceService.GetPoolConfig(bundleId) : null;
                if (poolConfig == null || !poolConfig.enabled)
                {
                    poolConfig = fallbackPoolConfig;
                }
                _configs[bundleId] = poolConfig;
            }

            // 풀 컨테이너 생성
            var poolContainer = new GameObject($"Pool_{bundleId}_{typeof(T).Name}");
            poolContainer.transform.SetParent(_poolRoot);

            // IObjectPool<T> 생성
            var pool = new ObjectPool<T>(
                createFunc: () => OnCreate<T>(bundleId, prefab, poolContainer.transform),
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: poolConfig.preloadCount,
                maxSize: poolConfig.maxPoolSize > 0 ? poolConfig.maxPoolSize : 10000
            );

            _typedPools[poolKey] = pool;

            if (poolConfig.preloadCount > 0)
            {
                Preload<T>(bundleId, poolConfig.preloadCount);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PoolService] TypedPool<{typeof(T).Name}> created for {bundleId} (preload: {poolConfig.preloadCount})");
#endif
            return pool;
        }

        private T OnCreate<T>(string bundleId, GameObject prefab, Transform parent) where T : Component
        {
            var obj = UnityEngine.Object.Instantiate(prefab, parent);
            obj.name = prefab.name;

            // IPoolable에 bundleId 주입
            var poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.PoolBundleId = bundleId;
            }

            return obj.GetComponent<T>();
        }

        private void OnGet<T>(T component) where T : Component
        {
            if (component == null) return;

            component.gameObject.SetActive(true);

            if (component is IPoolable poolable)
            {
                poolable.OnSpawnFromPool();
            }
        }

        private void OnRelease<T>(T component) where T : Component
        {
            if (component == null) return;

            if (component is IPoolable poolable)
            {
                poolable.OnReturnToPool();
            }

            component.gameObject.SetActive(false);
        }

        private void OnDestroyPoolObject<T>(T component) where T : Component
        {
            if (component != null)
            {
                UnityEngine.Object.Destroy(component.gameObject);
            }
        }

        private void CleanupBundleIfEmpty(string bundleId)
        {
            foreach (var key in _typedPools.Keys)
            {
                if (key.StartsWith(bundleId + "|"))
                {
                    return; // 아직 다른 타입 풀이 있음
                }
            }

            // 모두 정리됨
            _prefabs.Remove(bundleId);
            _configs.Remove(bundleId);

            if (_resourceService != null)
            {
                _resourceService.ReleaseResource(bundleId);
            }
        }

        #endregion

        #region Dynamic Register API

        /// <summary>
        /// 런타임에 프리팹을 풀에 동적으로 등록합니다.
        /// - ResourceService를 거치지 않고 bundleId로 Get/Preload가 가능해집니다.
        /// - 동일 bundleId가 이미 등록되어 있으면 덮어쓸 수 있습니다(기본: false).
        /// </summary>
        public bool RegisterPrefab(string bundleId, GameObject prefab, PoolConfig config = null, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(bundleId))
            {
                Debug.LogWarning("[PoolService] RegisterPrefab failed: bundleId is null/empty");
                return false;
            }

            if (prefab == null)
            {
                Debug.LogWarning("[PoolService] RegisterPrefab failed: prefab is null");
                return false;
            }

            Initialize();

            if (!overwrite && _prefabs.ContainsKey(bundleId))
            {
                return false;
            }

            _prefabs[bundleId] = prefab;
            _configs[bundleId] = config != null ? config : fallbackPoolConfig;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PoolService] RegisterPrefab: {bundleId} ({prefab.name})");
#endif
            return true;
        }

        /// <summary>
        /// 런타임 자동 ID로 프리팹을 등록합니다. (충돌 방지)
        /// </summary>
        public string RegisterPrefabAutoId(GameObject prefab, string nameHint = null, PoolConfig config = null)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[PoolService] RegisterPrefabAutoId failed: prefab is null");
                return null;
            }

            Initialize();

            var hint = string.IsNullOrWhiteSpace(nameHint) ? prefab.name : nameHint;
            hint = SanitizeIdPart(hint);

            // 예: runtime://MissileProjectile/20260126T123456Z/8f3a...
            var id = $"{RuntimeBundlePrefix}{hint}/{System.DateTime.UtcNow:yyyyMMddTHHmmssZ}/{System.Guid.NewGuid():N}";

            RegisterPrefab(id, prefab, config, overwrite: false);
            return id;
        }

        private static string SanitizeIdPart(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "prefab";

            // 최소 규칙: 공백/슬래시/역슬래시를 '_'로 치환
            return raw
                .Trim()
                .Replace(' ', '_')
                .Replace('/', '_')
                .Replace('\\', '_');
        }

        #endregion

        public override void Release()
        {
            ClearAll();
            base.Release();
        }

        public override void Destroy()
        {
            ClearAll();
            Instance = null;
            base.Destroy();
        }
    }
}
