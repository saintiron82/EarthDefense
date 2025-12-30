using UnityEngine;
using System.Collections.Generic;

namespace SG.Audio
{
    /// <summary>
    /// 오디오 채널별 AudioSource 풀 관리
    /// 각 채널은 독립적인 풀 크기와 재사용 전략을 가짐
    /// </summary>
    public class AudioSourcePool
    {
        private readonly Transform _parent;
        private readonly AudioChannelType _channelType;
        private readonly int _maxPoolSize;
        private readonly List<PooledAudioSource> _pool;
        private int _nextId = 0;

        public AudioSourcePool(Transform parent, AudioChannelType channelType, int maxPoolSize)
        {
            _parent = parent;
            _channelType = channelType;
            _maxPoolSize = maxPoolSize;
            _pool = new List<PooledAudioSource>(maxPoolSize);
        }

        /// <summary>
        /// 사용 가능한 AudioSource 가져오기 (없으면 새로 생성)
        /// </summary>
        public PooledAudioSource Get()
        {
            // 1. 재사용 가능한 소스 찾기 (재생 중이지 않고 활성화된 것)
            for (int i = 0; i < _pool.Count; i++)
            {
                var pooled = _pool[i];
                if (pooled != null && pooled.IsAvailable)
                {
                    pooled.MarkAsUsed();
                    return pooled;
                }
            }

            // 2. 풀 크기 제한 확인
            if (_pool.Count >= _maxPoolSize)
            {
                // 가장 오래된 것 강제 중단 후 재사용
                var oldest = FindOldestPlaying();
                if (oldest != null)
                {
                    oldest.Stop();
                    oldest.MarkAsUsed();
                    return oldest;
                }
                
                // 모든 소스가 사용 중이면 null 반환 (SFX 채널에서 발생 가능)
                return null;
            }

            // 3. 새 AudioSource 생성
            var newSource = CreateNewAudioSource();
            _pool.Add(newSource);
            newSource.MarkAsUsed();
            return newSource;
        }

        /// <summary>
        /// 새 AudioSource 생성 및 초기화
        /// </summary>
        private PooledAudioSource CreateNewAudioSource()
        {
            var go = new GameObject($"AudioSource_{_channelType}_{_nextId++}");
            go.transform.SetParent(_parent);
            
            var audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            
            // 채널별 기본 설정
            switch (_channelType)
            {
                case AudioChannelType.BGM:
                    audioSource.loop = true;
                    audioSource.priority = 0; // 최고 우선순위
                    break;
                    
                case AudioChannelType.SFX:
                    audioSource.loop = false;
                    audioSource.priority = 128;
                    break;
                    
                case AudioChannelType.Ambient:
                    audioSource.loop = true;
                    audioSource.priority = 64;
                    audioSource.spatialBlend = 0.5f; // 환경음은 약간 3D
                    break;
                    
                case AudioChannelType.UI:
                    audioSource.loop = false;
                    audioSource.priority = 0; // UI는 높은 우선순위
                    break;
                    
                case AudioChannelType.Voice:
                    audioSource.loop = false;
                    audioSource.priority = 32;
                    break;
            }
            
            return new PooledAudioSource(audioSource);
        }

        /// <summary>
        /// 가장 오래 재생 중인 AudioSource 찾기
        /// </summary>
        private PooledAudioSource FindOldestPlaying()
        {
            PooledAudioSource oldest = null;
            float oldestTime = float.MaxValue;
            
            for (int i = 0; i < _pool.Count; i++)
            {
                var pooled = _pool[i];
                if (pooled != null && pooled.IsPlaying && pooled.PlayStartTime < oldestTime)
                {
                    oldest = pooled;
                    oldestTime = pooled.PlayStartTime;
                }
            }
            
            return oldest;
        }

        /// <summary>
        /// 모든 AudioSource 정지
        /// </summary>
        public void StopAll()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null)
                {
                    _pool[i].Stop();
                }
            }
        }

        /// <summary>
        /// 모든 AudioSource 일시정지
        /// </summary>
        public void PauseAll()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && _pool[i].IsPlaying)
                {
                    _pool[i].Pause();
                }
            }
        }

        /// <summary>
        /// 모든 일시정지된 AudioSource 재개
        /// </summary>
        public void ResumeAll()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && _pool[i].IsPaused)
                {
                    _pool[i].Resume();
                }
            }
        }

        /// <summary>
        /// 채널 볼륨 일괄 적용
        /// </summary>
        public void SetVolume(float volume)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null)
                {
                    _pool[i].SetChannelVolume(volume);
                }
            }
        }

        /// <summary>
        /// 재생 중인 AudioSource 개수
        /// </summary>
        public int GetPlayingCount()
        {
            int count = 0;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && _pool[i].IsPlaying)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 풀 정리 (사용하지 않는 AudioSource 제거)
        /// </summary>
        public void Cleanup()
        {
            for (int i = _pool.Count - 1; i >= 0; i--)
            {
                if (_pool[i] != null && !_pool[i].IsPlaying && !_pool[i].IsPaused)
                {
                    if (_pool[i].Source != null)
                    {
                        Object.Destroy(_pool[i].Source.gameObject);
                    }
                    _pool.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 풀 완전 제거
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && _pool[i].Source != null)
                {
                    Object.Destroy(_pool[i].Source.gameObject);
                }
            }
            _pool.Clear();
        }
    }

    /// <summary>
    /// 풀링된 AudioSource 래퍼 클래스
    /// </summary>
    public class PooledAudioSource
    {
        public AudioSource Source { get; private set; }
        public float PlayStartTime { get; private set; }
        public bool IsPlaying => Source != null && Source.isPlaying;
        public bool IsPaused { get; private set; }
        public bool IsAvailable => Source != null && !IsPlaying && !IsPaused;

        private float _originalVolume;
        private float _channelVolume = 1f;
        private bool _inUse;

        public PooledAudioSource(AudioSource source)
        {
            Source = source;
            _originalVolume = 1f;
            IsPaused = false;
            _inUse = false;
        }

        public void MarkAsUsed()
        {
            _inUse = true;
            PlayStartTime = Time.time;
        }

        public void Play(AudioClip clip, AudioPlayConfig config)
        {
            if (Source == null)
            {
                return;
            }

            Source.clip = clip;
            Source.loop = config.Loop;
            Source.pitch = config.Pitch;
            Source.priority = config.Priority;
            
            _originalVolume = config.Volume;
            Source.volume = _originalVolume * _channelVolume;

            if (config.Is3D)
            {
                Source.spatialBlend = config.SpatialBlend;
                Source.minDistance = config.MinDistance;
                Source.maxDistance = config.MaxDistance;
                Source.transform.position = config.Position;
            }
            else
            {
                Source.spatialBlend = 0f;
            }

            if (config.Delay > 0f)
            {
                Source.PlayDelayed(config.Delay);
            }
            else
            {
                Source.Play();
            }

            PlayStartTime = Time.time;
            IsPaused = false;
            _inUse = true;
        }

        public void Stop()
        {
            if (Source != null)
            {
                Source.Stop();
            }
            IsPaused = false;
            _inUse = false;
        }

        public void Pause()
        {
            if (Source != null && IsPlaying)
            {
                Source.Pause();
                IsPaused = true;
            }
        }

        public void Resume()
        {
            if (Source != null && IsPaused)
            {
                Source.UnPause();
                IsPaused = false;
            }
        }

        public void SetChannelVolume(float channelVolume)
        {
            _channelVolume = channelVolume;
            if (Source != null)
            {
                Source.volume = _originalVolume * _channelVolume;
            }
        }

        public void SetVolume(float volume)
        {
            _originalVolume = volume;
            if (Source != null)
            {
                Source.volume = _originalVolume * _channelVolume;
            }
        }

        public float GetProgress()
        {
            if (Source == null || Source.clip == null)
            {
                return 0f;
            }
            return Source.time / Source.clip.length;
        }
    }
}
