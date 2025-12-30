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

        private Dictionary<string, IObjectPool<GameObject>> _pools;
        private Dictionary<string, GameObject> _prefabs;
        private Dictionary<string, PoolConfig> _configs;
        private Transform _poolRoot;
        private bool _initialized;

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

            _pools = new Dictionary<string, IObjectPool<GameObject>>();
            _prefabs = new Dictionary<string, GameObject>();
            _configs = new Dictionary<string, PoolConfig>();

            _poolRoot = transform;

            _initialized = true;
        }

        public void SetResourceService(ResourceService service)
        {
            _resourceService = service;
        }

        public GameObject Get(string id)
        {
            return Get(id, Vector3.zero, Quaternion.identity);
        }

        public GameObject Get(string id, Vector3 position, Quaternion rotation)
        {
            Initialize();

            if (!_pools.TryGetValue(id, out var pool))
            {
                pool = CreatePool(id);
                if (pool == null)
                {
                    return null;
                }
            }

            var obj = pool.Get();
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }

            return obj;
        }

        public T Get<T>(string id) where T : Component
        {
            var obj = Get(id);
            if (obj != null)
            {
                return obj.GetComponent<T>();
            }

            return null;
        }

        public T Get<T>(string id, Vector3 position, Quaternion rotation) where T : Component
        {
            var obj = Get(id, position, rotation);
            if (obj != null)
            {
                return obj.GetComponent<T>();
            }

            return null;
        }

        public async UniTask GetAsync(string id, Action<GameObject> callback)
        {
            await UniTask.Yield();

            var obj = Get(id);

            if (callback != null)
            {
                callback(obj);
            }
        }

        public void Return(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            string id = GetPoolIdFromObject(obj);
            if (!string.IsNullOrEmpty(id) && _pools.TryGetValue(id, out var pool))
            {
                pool.Release(obj);
            }
            else
            {
                Debug.LogWarning($"[PoolService] Object not found in any pool: {obj.name}");
                UnityEngine.Object.Destroy(obj);
            }
        }

        public void Return(string id, GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (_pools.TryGetValue(id, out var pool))
            {
                pool.Release(obj);
            }
            else
            {
                Debug.LogWarning($"[PoolService] Pool not found: {id}");
                UnityEngine.Object.Destroy(obj);
            }
        }

        public void Preload(string id, int count)
        {
            Initialize();

            if (!_pools.TryGetValue(id, out var pool))
            {
                pool = CreatePool(id);
                if (pool == null)
                {
                    return;
                }
            }

            var tempList = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                tempList.Add(pool.Get());
            }

            foreach (var obj in tempList)
            {
                pool.Release(obj);
            }
        }

        public async UniTask PreloadAsync(string id, int count, Action onComplete)
        {
            Initialize();

            if (!_pools.TryGetValue(id, out var pool))
            {
                pool = CreatePool(id);
                if (pool == null)
                {
                    if (onComplete != null)
                    {
                        onComplete();
                    }
                    return;
                }
            }

            var tempList = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                tempList.Add(pool.Get());

                if (i % 10 == 0)
                {
                    await UniTask.Yield();
                }
            }

            foreach (var obj in tempList)
            {
                pool.Release(obj);
            }

            if (onComplete != null)
            {
                onComplete();
            }
        }

        public void Clear(string id)
        {
            if (_pools.TryGetValue(id, out var pool))
            {
                pool.Clear();
                _pools.Remove(id);
                _prefabs.Remove(id);
                _configs.Remove(id);

                if (_resourceService != null)
                {
                    _resourceService.ReleaseResource(id);
                }

                Debug.Log($"[PoolService] Pool cleared: {id}");
            }
        }

        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }

            _pools.Clear();
            _prefabs.Clear();
            _configs.Clear();

            Debug.Log("[PoolService] All pools cleared");
        }

        public int GetActiveCount(string id)
        {
            if (_pools.TryGetValue(id, out var pool) && pool is ObjectPool<GameObject> objPool)
            {
                return objPool.CountActive;
            }

            return 0;
        }

        public int GetInactiveCount(string id)
        {
            if (_pools.TryGetValue(id, out var pool) && pool is ObjectPool<GameObject> objPool)
            {
                return objPool.CountInactive;
            }

            return 0;
        }

        public bool Contains(string id)
        {
            return _pools.ContainsKey(id);
        }

        private IObjectPool<GameObject> CreatePool(string id)
        {
            if (_resourceService == null)
            {
                Debug.LogError("[PoolService] ResourceService is not set");
                return null;
            }

            var prefab = _resourceService.LoadPrefab(id);
            if (prefab == null)
            {
                Debug.LogError($"[PoolService] Failed to load prefab: {id}");
                return null;
            }

            var poolConfig = _resourceService.GetPoolConfig(id);
            if (poolConfig == null || !poolConfig.enabled)
            {
                poolConfig = fallbackPoolConfig;
            }

            _prefabs[id] = prefab;
            _configs[id] = poolConfig;

            var poolContainer = new GameObject($"Pool_{id}");
            poolContainer.transform.SetParent(_poolRoot);

            var pool = new ObjectPool<GameObject>(
                createFunc: () => OnCreate(id, prefab, poolContainer.transform),
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: poolConfig.preloadCount,
                maxSize: poolConfig.maxPoolSize > 0 ? poolConfig.maxPoolSize : 10000
            );

            _pools[id] = pool;

            if (poolConfig.preloadCount > 0)
            {
                Preload(id, poolConfig.preloadCount);
            }

            Debug.Log($"[PoolService] Pool created: {id} (preload: {poolConfig.preloadCount})");
            return pool;
        }

        private GameObject OnCreate(string id, GameObject prefab, Transform parent)
        {
            var obj = UnityEngine.Object.Instantiate(prefab, parent);
            obj.name = prefab.name;

            var poolMarker = obj.AddComponent<PoolObjectMarker>();
            poolMarker.PoolId = id;

            return obj;
        }

        private void OnGet(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetActive(true);

            var poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnSpawnFromPool();
            }
        }

        private void OnRelease(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            var poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnReturnToPool();
            }

            obj.SetActive(false);
        }

        private void OnDestroyPoolObject(GameObject obj)
        {
            if (obj != null)
            {
                UnityEngine.Object.Destroy(obj);
            }
        }

        private string GetPoolIdFromObject(GameObject obj)
        {
            if (obj == null)
            {
                return null;
            }

            var marker = obj.GetComponent<PoolObjectMarker>();
            return marker != null ? marker.PoolId : null;
        }

        public override void Release()
        {
            Debug.Log("[PoolService] Release");
            ClearAll();
            base.Release();
        }

        public override void Destroy()
        {
            ClearAll();
            Instance = null;
            base.Destroy();
        }

        private class PoolObjectMarker : MonoBehaviour
        {
            public string PoolId;
        }
    }
}
