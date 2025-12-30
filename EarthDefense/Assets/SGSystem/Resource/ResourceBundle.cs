using System.Collections.Generic;
using UnityEngine;

namespace Script.SystemCore.Resource
{
    [CreateAssetMenu(fileName = "NewResourceBundle", menuName = "SystemCore/Resource Bundle", order = 100)]
    public class ResourceBundle : ScriptableObject
    {
        [Header("Bundle Info")]
        [Tooltip("번들 고유 ID")]
        public string bundleId;

        [Header("Resources")]
        [Tooltip("리소스 엔트리 목록")]
        public List<ResourceEntry> entries;

        [Header("Addressables Labels")]
        [Tooltip("이 번들과 연관된 Addressables 레이블 목록")]
        public List<string> addressableLabels;

        [Header("Default Pool Settings")]
        [Tooltip("이 번들의 기본 풀 설정 프리셋")]
        public PoolConfigPreset defaultPoolPreset;

        private Dictionary<string, ResourceEntry> _lookup;

        public void BuildLookup()
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, ResourceEntry>();
            }

            _lookup.Clear();

            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.id))
                {
                    continue;
                }

                if (!_lookup.ContainsKey(entry.id))
                {
                    _lookup.Add(entry.id, entry);
                }
                else
                {
                    Debug.LogWarning($"[ResourceBundle:{bundleId}] Duplicate ID: {entry.id}");
                }
            }
        }

        public bool TryGetEntry(string id, out ResourceEntry entry)
        {
            if (_lookup == null)
            {
                BuildLookup();
            }

            return _lookup.TryGetValue(id, out entry);
        }

        public bool Contains(string id)
        {
            if (_lookup == null)
            {
                BuildLookup();
            }

            return _lookup.ContainsKey(id);
        }

        public PoolConfig GetPoolConfig(ResourceEntry entry)
        {
            if (entry.poolPresetOverride != null)
            {
                return entry.poolPresetOverride.config;
            }

            if (defaultPoolPreset != null)
            {
                return defaultPoolPreset.config;
            }

            return new PoolConfig { enabled = false };
        }

        private void OnEnable()
        {
            BuildLookup();
        }

        private void OnValidate()
        {
            BuildLookup();
        }
    }
}
