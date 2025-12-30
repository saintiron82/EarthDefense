using UnityEngine;
using System.Collections.Generic;

namespace SG.Audio
{
    /// <summary>
    /// 오디오 리소스 관리 및 캐싱
    /// AudioClip 로드/언로드 및 메모리 관리
    /// </summary>
    public class AudioResourceManager
    {
        private readonly Dictionary<string, CachedAudioClip> _cache;
        private readonly int _maxCacheSize;
        private long _currentMemoryUsage;
        private long _maxMemoryUsage;

        public AudioResourceManager(int maxCacheSize = 100, long maxMemoryBytes = 100 * 1024 * 1024)
        {
            _cache = new Dictionary<string, CachedAudioClip>(maxCacheSize);
            _maxCacheSize = maxCacheSize;
            _maxMemoryUsage = maxMemoryBytes;
            _currentMemoryUsage = 0;
        }

        /// <summary>
        /// AudioClip 로드 (캐시 우선)
        /// </summary>
        public AudioClip LoadClip(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning("[AudioResourceManager] Empty address");
                return null;
            }

            // 캐시 확인
            if (_cache.TryGetValue(address, out var cached))
            {
                cached.UpdateAccessTime();
                return cached.Clip;
            }

            // ResourceService를 통해 로드
            var clip = SG.Resource.ResourceService.LoadAudioClip(address);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioResourceManager] Failed to load: {address}");
                return null;
            }

            // 메모리 체크 및 캐시 추가
            long clipSize = EstimateClipSize(clip);
            if (_currentMemoryUsage + clipSize > _maxMemoryUsage)
            {
                // 메모리 부족 시 오래된 것부터 제거
                CleanupOldestCached(clipSize);
            }

            // 캐시 크기 제한 체크
            if (_cache.Count >= _maxCacheSize)
            {
                RemoveOldestCached();
            }

            // 캐시에 추가
            var newCached = new CachedAudioClip(address, clip, clipSize);
            _cache[address] = newCached;
            _currentMemoryUsage += clipSize;

            return clip;
        }

        /// <summary>
        /// 특정 AudioClip 언로드
        /// </summary>
        public void UnloadClip(string address)
        {
            if (_cache.TryGetValue(address, out var cached))
            {
                _currentMemoryUsage -= cached.EstimatedSize;
                _cache.Remove(address);
                
                // Resources로 로드한 것은 Unload 불가능, Addressables는 자동 관리
                // 필요시 Resources.UnloadAsset(cached.Clip) 호출 가능
            }
        }

        /// <summary>
        /// 모든 캐시 클리어
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            _currentMemoryUsage = 0;
        }

        /// <summary>
        /// 사용하지 않는 캐시 정리 (마지막 접근 시간 기준)
        /// </summary>
        public void CleanupUnused(float unusedThresholdSeconds = 300f)
        {
            var now = Time.realtimeSinceStartup;
            var toRemove = new List<string>();

            foreach (var kvp in _cache)
            {
                if (now - kvp.Value.LastAccessTime > unusedThresholdSeconds)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                UnloadClip(key);
            }
        }

        /// <summary>
        /// 가장 오래된 캐시 제거
        /// </summary>
        private void RemoveOldestCached()
        {
            string oldestKey = null;
            float oldestTime = float.MaxValue;

            foreach (var kvp in _cache)
            {
                if (kvp.Value.LastAccessTime < oldestTime)
                {
                    oldestTime = kvp.Value.LastAccessTime;
                    oldestKey = kvp.Key;
                }
            }

            if (oldestKey != null)
            {
                UnloadClip(oldestKey);
            }
        }

        /// <summary>
        /// 메모리 확보를 위해 오래된 것부터 제거
        /// </summary>
        private void CleanupOldestCached(long requiredSize)
        {
            var sorted = new List<CachedAudioClip>(_cache.Values);
            sorted.Sort((a, b) => a.LastAccessTime.CompareTo(b.LastAccessTime));

            long freed = 0;
            var toRemove = new List<string>();

            foreach (var cached in sorted)
            {
                if (freed >= requiredSize)
                {
                    break;
                }

                toRemove.Add(cached.Address);
                freed += cached.EstimatedSize;
            }

            foreach (var key in toRemove)
            {
                UnloadClip(key);
            }
        }

        /// <summary>
        /// AudioClip 메모리 크기 추정
        /// </summary>
        private long EstimateClipSize(AudioClip clip)
        {
            if (clip == null)
            {
                return 0;
            }

            // 대략적인 크기: samples * channels * bytes_per_sample
            // 16bit PCM 기준
            long samples = clip.samples;
            int channels = clip.channels;
            return samples * channels * 2; // 2 bytes per sample (16bit)
        }

        /// <summary>
        /// 현재 캐시 상태 정보
        /// </summary>
        public string GetCacheInfo()
        {
            return $"Cache: {_cache.Count}/{_maxCacheSize} clips, " +
                   $"Memory: {_currentMemoryUsage / (1024 * 1024)}MB / {_maxMemoryUsage / (1024 * 1024)}MB";
        }

        /// <summary>
        /// 특정 클립이 캐시되어 있는지 확인
        /// </summary>
        public bool IsCached(string address)
        {
            return _cache.ContainsKey(address);
        }
    }

    /// <summary>
    /// 캐시된 AudioClip 정보
    /// </summary>
    internal class CachedAudioClip
    {
        public string Address { get; private set; }
        public AudioClip Clip { get; private set; }
        public long EstimatedSize { get; private set; }
        public float LastAccessTime { get; private set; }
        public float LoadTime { get; private set; }

        public CachedAudioClip(string address, AudioClip clip, long estimatedSize)
        {
            Address = address;
            Clip = clip;
            EstimatedSize = estimatedSize;
            LoadTime = Time.realtimeSinceStartup;
            LastAccessTime = LoadTime;
        }

        public void UpdateAccessTime()
        {
            LastAccessTime = Time.realtimeSinceStartup;
        }
    }
}
