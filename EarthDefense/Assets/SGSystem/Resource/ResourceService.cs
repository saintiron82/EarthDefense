using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Script.SystemCore.Resource
{
    public class ResourceService : SG.MonoServiceBase
    {
        public static ResourceService Instance { get; private set; }

        [Header("Resource Bundles")]
        [SerializeField] private List<ResourceBundle> _bundles;

        private Dictionary<string, UnityEngine.Object> _cache;
        private Dictionary<string, string> _idToGroup;
        private Dictionary<string, HashSet<string>> _groupToIds;
        private Dictionary<string, ResourceBundle> _idToBundle;
        private bool _initialized;

        private const string RuntimeIdPrefix = "runtime://";
        private Transform _runtimeCreateRoot;

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

            _cache = new Dictionary<string, UnityEngine.Object>();
            _idToGroup = new Dictionary<string, string>();
            _groupToIds = new Dictionary<string, HashSet<string>>();
            _idToBundle = new Dictionary<string, ResourceBundle>();

            if (_bundles != null)
            {
                for (int i = 0; i < _bundles.Count; i++)
                {
                    var bundle = _bundles[i];
                    if (bundle == null)
                    {
                        continue;
                    }

                    bundle.BuildLookup();

                    if (bundle.entries != null)
                    {
                        for (int j = 0; j < bundle.entries.Count; j++)
                        {
                            var entry = bundle.entries[j];
                            if (entry != null && !string.IsNullOrEmpty(entry.id))
                            {
                                _idToBundle[entry.id] = bundle;
                            }
                        }
                    }
                }
            }

            _initialized = true;
        }

        public void RegisterBundle(ResourceBundle bundle)
        {
            if (bundle == null)
            {
                return;
            }

            Initialize();

            if (_bundles == null)
            {
                _bundles = new List<ResourceBundle>();
            }

            if (!_bundles.Contains(bundle))
            {
                _bundles.Add(bundle);
                bundle.BuildLookup();

                if (bundle.entries != null)
                {
                    for (int i = 0; i < bundle.entries.Count; i++)
                    {
                        var entry = bundle.entries[i];
                        if (entry != null && !string.IsNullOrEmpty(entry.id))
                        {
                            _idToBundle[entry.id] = bundle;
                        }
                    }
                }
            }
        }

        public T Load<T>(string id) where T : UnityEngine.Object
        {
            Initialize();

            if (_cache.TryGetValue(id, out var cached))
            {
                return cached as T;
            }

            var entry = GetResourceEntry(id);
            if (entry == null)
            {
                Debug.LogWarning($"[ResourceService] Resource entry not found: {id}");
                return TryLoadFromResources<T>(id);
            }

            T result = null;

            if (entry.directAsset != null)
            {
                result = entry.directAsset as T;
            }

            if (result == null)
            {
                result = TryLoadFromResources<T>(id);
            }

            if (result != null)
            {
                _cache[id] = result;
                RegisterToGroup(id, entry.cacheGroup);
            }

            return result;
        }

        public Sprite LoadSprite(string id)
        {
            return Load<Sprite>(id);
        }

        public GameObject LoadPrefab(string id)
        {
            return Load<GameObject>(id);
        }

        public async UniTask LoadAsync<T>(string id, Action<T> onComplete) where T : UnityEngine.Object
        {
            await UniTask.Yield();

            T result = Load<T>(id);

            if (onComplete != null)
            {
                onComplete(result);
            }
        }

        public async UniTask LoadSpriteAsync(string id, Action<Sprite> onComplete)
        {
            await LoadAsync(id, onComplete);
        }

        public async UniTask LoadPrefabAsync(string id, Action<GameObject> onComplete)
        {
            await LoadAsync(id, onComplete);
        }

        private T TryLoadFromResources<T>(string id) where T : UnityEngine.Object
        {
            try
            {
                return Resources.Load<T>(id);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ResourceService] Resources.Load failed: {id}, {ex.Message}");
                return null;
            }
        }

        private ResourceEntry GetResourceEntry(string id)
        {
            if (_bundles == null)
            {
                return null;
            }

            for (int i = 0; i < _bundles.Count; i++)
            {
                var bundle = _bundles[i];
                if (bundle != null && bundle.TryGetEntry(id, out var entry))
                {
                    return entry;
                }
            }

            return null;
        }

        public PoolConfig GetPoolConfig(string id)
        {
            if (_idToBundle.TryGetValue(id, out var bundle))
            {
                if (bundle.TryGetEntry(id, out var entry))
                {
                    return bundle.GetPoolConfig(entry);
                }
            }

            return null;
        }

        private void RegisterToGroup(string id, string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                groupName = "Default";
            }

            _idToGroup[id] = groupName;

            if (!_groupToIds.ContainsKey(groupName))
            {
                _groupToIds[groupName] = new HashSet<string>();
            }

            _groupToIds[groupName].Add(id);
        }

        public void ReleaseResource(string id)
        {
            ReleaseInternal(id);
        }

        public void ReleaseGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                Debug.LogWarning("[ResourceService] Group name is null or empty");
                return;
            }

            if (!_groupToIds.TryGetValue(groupName, out var ids))
            {
                Debug.LogWarning($"[ResourceService] Cache group not found: {groupName}");
                return;
            }

            foreach (var id in ids.ToList())
            {
                ReleaseInternal(id);
            }

            _groupToIds.Remove(groupName);

            Debug.Log($"[ResourceService] Released cache group: {groupName} ({ids.Count} items)");
        }

        public void ReleaseGroups(params string[] groupNames)
        {
            if (groupNames == null || groupNames.Length == 0)
            {
                return;
            }

            for (int i = 0; i < groupNames.Length; i++)
            {
                ReleaseGroup(groupNames[i]);
            }
        }

        public void ReleaseAll()
        {
            if (_cache != null)
            {
                _cache.Clear();
            }

            if (_idToGroup != null)
            {
                _idToGroup.Clear();
            }

            if (_groupToIds != null)
            {
                _groupToIds.Clear();
            }

            Debug.Log("[ResourceService] All cache released");
        }

        public void ReleaseAllExcept(params string[] keepGroups)
        {
            if (keepGroups == null || keepGroups.Length == 0)
            {
                ReleaseAll();
                return;
            }

            var keepSet = new HashSet<string>(keepGroups);
            var releaseGroups = _groupToIds.Keys
                .Where(g => !keepSet.Contains(g))
                .ToList();

            for (int i = 0; i < releaseGroups.Count; i++)
            {
                ReleaseGroup(releaseGroups[i]);
            }
        }

        public void ReleaseWhere(Func<string, bool> predicate)
        {
            if (predicate == null)
            {
                return;
            }

            var releaseIds = _cache.Keys
                .Where(predicate)
                .ToList();

            for (int i = 0; i < releaseIds.Count; i++)
            {
                ReleaseInternal(releaseIds[i]);
            }
        }

        private void ReleaseInternal(string id)
        {
            if (_cache != null && _cache.ContainsKey(id))
            {
                _cache.Remove(id);
            }

            if (_idToGroup != null && _idToGroup.TryGetValue(id, out var groupName))
            {
                _idToGroup.Remove(id);

                if (_groupToIds != null && _groupToIds.TryGetValue(groupName, out var ids))
                {
                    ids.Remove(id);
                }
            }
        }

        public bool Contains(string id)
        {
            Initialize();

            if (_cache.ContainsKey(id))
            {
                return true;
            }

            if (_bundles != null)
            {
                for (int i = 0; i < _bundles.Count; i++)
                {
                    if (_bundles[i] != null && _bundles[i].Contains(id))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IEnumerable<string> GetCacheGroups()
        {
            return _groupToIds != null ? _groupToIds.Keys : Enumerable.Empty<string>();
        }

        public int GetCacheCount(string groupName)
        {
            if (_groupToIds != null && _groupToIds.TryGetValue(groupName, out var ids))
            {
                return ids.Count;
            }

            return 0;
        }

        public bool HasCacheGroup(string groupName)
        {
            return _groupToIds != null && _groupToIds.ContainsKey(groupName);
        }

        public IEnumerable<string> GetCachedIds(string groupName)
        {
            if (_groupToIds != null && _groupToIds.TryGetValue(groupName, out var ids))
            {
                return ids;
            }

            return Enumerable.Empty<string>();
        }

        public void LogCacheStatus()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("=== ResourceService Cache Status ===");
            Debug.Log($"Total cached: {(_cache != null ? _cache.Count : 0)}");
            Debug.Log($"Groups: {(_groupToIds != null ? _groupToIds.Count : 0)}");

            if (_groupToIds != null)
            {
                foreach (var kvp in _groupToIds)
                {
                    Debug.Log($"  [{kvp.Key}]: {kvp.Value.Count} items");
                }
            }
#endif
        }

        public override void Release()
        {
            Debug.Log("[ResourceService] Release");
            ReleaseAll();
            base.Release();
        }

        public override void Destroy()
        {
            ReleaseAll();
            Instance = null;
            base.Destroy();
        }

        private Transform GetOrCreateRuntimeRoot()
        {
            if (_runtimeCreateRoot != null) return _runtimeCreateRoot;

            // 루트 아래에 고정 오브젝트로 보관
            var go = GameObject.Find("RuntimeCreateObject");
            if (go == null)
            {
                go = new GameObject("RuntimeCreateObject");
                // 서비스 인스턴스 아래에 두면 씬 정리에 유리
                go.transform.SetParent(transform, worldPositionStays: false);
            }

            _runtimeCreateRoot = go.transform;
            return _runtimeCreateRoot;
        }

        /// <summary>
        /// 런타임에 만들어진 프리팹 템플릿을 ResourceService에 등록합니다.
        /// - 내부 캐시(_cache)에 등록되므로 LoadPrefab(id)로 즉시 사용 가능
        /// - 템플릿은 RuntimeCreateObject 밑에 비활성으로 보관됩니다.
        /// </summary>
        public bool RegisterRuntimePrefab(string id, GameObject prefabTemplate, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[ResourceService] RegisterRuntimePrefab failed: id is null/empty");
                return false;
            }

            if (prefabTemplate == null)
            {
                Debug.LogWarning("[ResourceService] RegisterRuntimePrefab failed: prefabTemplate is null");
                return false;
            }

            Initialize();

            if (!overwrite && _cache.ContainsKey(id))
            {
                return false;
            }

            // 템플릿 보관: scene instance라도 원본을 여기로 옮겨서 템플릿처럼 사용
            var root = GetOrCreateRuntimeRoot();
            prefabTemplate.transform.SetParent(root, worldPositionStays: false);
            prefabTemplate.SetActive(false);

            _cache[id] = prefabTemplate;
            RegisterToGroup(id, "Runtime");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ResourceService] Runtime prefab registered: {id} ({prefabTemplate.name})");
#endif
            return true;
        }

        /// <summary>
        /// 자동 ID로 런타임 프리팹 템플릿을 등록합니다.
        /// </summary>
        public string RegisterRuntimePrefabAutoId(GameObject prefabTemplate, string nameHint = null)
        {
            if (prefabTemplate == null)
            {
                Debug.LogWarning("[ResourceService] RegisterRuntimePrefabAutoId failed: prefabTemplate is null");
                return null;
            }

            Initialize();

            var hint = string.IsNullOrWhiteSpace(nameHint) ? prefabTemplate.name : nameHint;
            hint = SanitizeIdPart(hint);

            var id = $"{RuntimeIdPrefix}{hint}/{System.DateTime.UtcNow:yyyyMMddTHHmmssZ}/{System.Guid.NewGuid():N}";
            RegisterRuntimePrefab(id, prefabTemplate, overwrite: false);
            return id;
        }

        private static string SanitizeIdPart(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "prefab";
            return raw
                .Trim()
                .Replace(' ', '_')
                .Replace('/', '_')
                .Replace('\\', '_');
        }
    }
}
