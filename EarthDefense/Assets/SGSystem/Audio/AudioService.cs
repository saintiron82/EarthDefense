using UnityEngine;
using System.Collections.Generic;
using System.Collections;

#if USE_UNITASK
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace SG.Audio
{
    /// <summary>
    /// 오디오 서비스 메인 구현체
    /// 채널별 풀링, 핸들 기반 제어, 페이드 효과 지원
    /// MonoServiceBase 상속으로 Coroutine 네이티브 지원
    /// </summary>
    public class AudioService : MonoServiceBase, IAudioService, IDoUpdate
    {
        // ==================== Constants ====================
        private const int MAX_SFX_POOL = 16;
        private const int MAX_AMBIENT_POOL = 4;
        private const int MAX_BGM_POOL = 2;
        private const int MAX_UI_POOL = 2;
        private const int MAX_VOICE_POOL = 1;

        // ==================== Components ====================
        private AudioResourceManager _resourceManager;
        private Dictionary<AudioChannelType, AudioSourcePool> _channelPools;
        private Dictionary<int, AudioHandle> _activeHandles;
        private List<AudioHandle> _handlesToCleanup;

        // ==================== Settings ====================
        private float _masterVolume = 1f;
        private Dictionary<AudioChannelType, float> _channelVolumes;
        private bool _isMuted = false;

        // ==================== BGM State ====================
        private string _currentBGM;
        private AudioHandle _currentBGMHandle;

        // ==================== Ambient Tracking ====================
        private Dictionary<string, List<AudioHandle>> _ambientHandlesByAddress;

        // ==================== Fade Tracking (중복 방지) ====================
        private Dictionary<int, Coroutine> _activeFadeCoroutines;

        // ==================== Cleanup Optimization ====================
        private int _cleanupFrameCounter = 0;
        private const int CLEANUP_INTERVAL = 30; // 0.5초마다 (60fps 기준)

        // ==================== Properties ====================
        public bool IsMuted => _isMuted;
        public string CurrentBGM => _currentBGM;
        public bool IsBGMPlaying => _currentBGMHandle != null && _currentBGMHandle.IsPlaying;

        // ==================== Initialization ====================
        public override void DirectInit()
        {
            base.DirectInit();

            // 리소스 매니저 초기화
            _resourceManager = new AudioResourceManager(maxCacheSize: 100, maxMemoryBytes: 100 * 1024 * 1024);

            // 채널 풀 초기화
            _channelPools = new Dictionary<AudioChannelType, AudioSourcePool>
            {
                { AudioChannelType.BGM, new AudioSourcePool(transform, AudioChannelType.BGM, MAX_BGM_POOL) },
                { AudioChannelType.SFX, new AudioSourcePool(transform, AudioChannelType.SFX, MAX_SFX_POOL) },
                { AudioChannelType.Ambient, new AudioSourcePool(transform, AudioChannelType.Ambient, MAX_AMBIENT_POOL) },
                { AudioChannelType.UI, new AudioSourcePool(transform, AudioChannelType.UI, MAX_UI_POOL) },
                { AudioChannelType.Voice, new AudioSourcePool(transform, AudioChannelType.Voice, MAX_VOICE_POOL) }
            };

            // 채널 볼륨 초기화
            _channelVolumes = new Dictionary<AudioChannelType, float>
            {
                { AudioChannelType.BGM, 1f },
                { AudioChannelType.SFX, 1f },
                { AudioChannelType.Ambient, 1f },
                { AudioChannelType.UI, 1f },
                { AudioChannelType.Voice, 1f }
            };

            // 핸들 추적 초기화
            _activeHandles = new Dictionary<int, AudioHandle>();
            _handlesToCleanup = new List<AudioHandle>();
            _ambientHandlesByAddress = new Dictionary<string, List<AudioHandle>>();
            _activeFadeCoroutines = new Dictionary<int, Coroutine>();

            // 설정 로드
            LoadSettings();
        }

        public override void Release()
        {
            base.Release();

            // 모든 사운드 정지
            StopAll();

            // 활성 코루틴 정리
            if (_activeFadeCoroutines != null)
            {
                foreach (var coroutine in _activeFadeCoroutines.Values)
                {
                    if (coroutine != null)
                    {
                        StopCoroutine(coroutine);
                    }
                }
                _activeFadeCoroutines.Clear();
            }

            // 풀 정리
            if (_channelPools != null)
            {
                foreach (var pool in _channelPools.Values)
                {
                    pool?.Dispose();
                }
                _channelPools.Clear();
            }

            // 핸들 정리
            _activeHandles?.Clear();
            _handlesToCleanup?.Clear();
            _ambientHandlesByAddress?.Clear();

            // 캐시 정리
            _resourceManager?.ClearCache();
        }

        // ==================== Update ====================
        public void DoUpdate()
        {
            // 핸들 정리는 30프레임마다 실행 (최적화)
            if (++_cleanupFrameCounter >= CLEANUP_INTERVAL)
            {
                _cleanupFrameCounter = 0;
                CleanupInvalidHandles();
            }
        }

        private void CleanupInvalidHandles()
        {
            _handlesToCleanup.Clear();

            foreach (var kvp in _activeHandles)
            {
                if (!kvp.Value.IsValid || (!kvp.Value.IsPlaying && !kvp.Value.IsPaused))
                {
                    _handlesToCleanup.Add(kvp.Value);
                }
            }

            foreach (var handle in _handlesToCleanup)
            {
                RemoveHandle(handle);
            }
        }

        // ==================== Handle Management ====================
        private AudioHandle CreateHandle(AudioChannelType channel, string address, PooledAudioSource pooledSource)
        {
            var handle = new AudioHandle
            {
                Channel = channel,
                Address = address,
                PooledSource = pooledSource
            };

            _activeHandles[handle.Id] = handle;
            return handle;
        }

        private void RemoveHandle(AudioHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            _activeHandles.Remove(handle.Id);
            handle.Invalidate();

            // Ambient 추적에서도 제거
            if (handle.Channel == AudioChannelType.Ambient && !string.IsNullOrEmpty(handle.Address))
            {
                if (_ambientHandlesByAddress.TryGetValue(handle.Address, out var list))
                {
                    list.Remove(handle);
                    if (list.Count == 0)
                    {
                        _ambientHandlesByAddress.Remove(handle.Address);
                    }
                }
            }
        }

        // ==================== Core Play Method ====================
        private AudioHandle PlayInternal(AudioChannelType channel, string address, AudioPlayConfig config)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning("[AudioService] Empty address");
                return null;
            }

            if (config == null)
            {
                config = AudioPlayConfig.Default;
            }

            // AudioClip 로드
            var clip = _resourceManager.LoadClip(address);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioService] Failed to load clip: {address}");
                return null;
            }

            // AudioSource 풀에서 가져오기
            var pool = _channelPools[channel];
            var pooledSource = pool.Get();
            if (pooledSource == null)
            {
                Debug.LogWarning($"[AudioService] No available AudioSource in {channel} pool");
                return null;
            }

            // 볼륨 적용 (마스터 * 채널 * 개별)
            float finalVolume = _masterVolume * _channelVolumes[channel] * config.Volume;
            if (_isMuted)
            {
                finalVolume = 0f;
            }

            var playConfig = new AudioPlayConfig
            {
                Volume = finalVolume,
                Pitch = config.Pitch,
                Loop = config.Loop,
                FadeInDuration = config.FadeInDuration,
                Delay = config.Delay,
                Is3D = config.Is3D,
                Position = config.Position,
                MinDistance = config.MinDistance,
                MaxDistance = config.MaxDistance,
                SpatialBlend = config.SpatialBlend,
                Priority = config.Priority
            };

            // 재생
            pooledSource.Play(clip, playConfig);

            // 핸들 생성 및 등록
            var handle = CreateHandle(channel, address, pooledSource);

            // Ambient는 주소별 추적
            if (channel == AudioChannelType.Ambient)
            {
                if (!_ambientHandlesByAddress.ContainsKey(address))
                {
                    _ambientHandlesByAddress[address] = new List<AudioHandle>();
                }
                _ambientHandlesByAddress[address].Add(handle);
            }

            // 페이드 인 처리
            if (config.FadeInDuration > 0f)
            {
                StartFadeIn(handle, config.FadeInDuration);
            }

            return handle;
        }

        // ==================== BGM ====================
        public AudioHandle PlayBGM(string address, float fadeInDuration = 1f)
        {
            if (_currentBGMHandle != null && _currentBGMHandle.IsPlaying)
            {
                StopBGM(fadeInDuration);
            }

            var config = new AudioPlayConfig
            {
                Loop = true,
                Volume = 1f,
                FadeInDuration = fadeInDuration
            };

            _currentBGMHandle = PlayInternal(AudioChannelType.BGM, address, config);
            _currentBGM = address;
            return _currentBGMHandle;
        }

        public void StopBGM(float fadeOutDuration = 1f)
        {
            if (_currentBGMHandle != null)
            {
                if (fadeOutDuration > 0f)
                {
                    StartFadeOut(_currentBGMHandle, fadeOutDuration, () =>
                    {
                        _currentBGMHandle?.Stop();
                        _currentBGMHandle = null;
                        _currentBGM = null;
                    });
                }
                else
                {
                    _currentBGMHandle.Stop();
                    _currentBGMHandle = null;
                    _currentBGM = null;
                }
            }
        }

        public void PauseBGM()
        {
            _currentBGMHandle?.Pause();
        }

        public void ResumeBGM()
        {
            _currentBGMHandle?.Resume();
        }

        public AudioHandle CrossfadeBGM(string address, float duration = 2f)
        {
            var oldHandle = _currentBGMHandle;
            
            // 새 BGM 시작
            var newHandle = PlayBGM(address, duration);

            // 이전 BGM 페이드 아웃
            if (oldHandle != null && oldHandle.IsPlaying)
            {
                StartFadeOut(oldHandle, duration, () => oldHandle.Stop());
            }

            return newHandle;
        }

        // ==================== SFX ====================
        public AudioHandle PlaySFX(string address, AudioPlayConfig config = null)
        {
            if (config == null)
            {
                config = AudioPlayConfig.Default;
            }
            return PlayInternal(AudioChannelType.SFX, address, config);
        }

        public AudioHandle PlaySFX(string address, float volume = 1f)
        {
            var config = new AudioPlayConfig { Volume = volume };
            return PlayInternal(AudioChannelType.SFX, address, config);
        }

        public AudioHandle PlaySFX3D(string address, Vector3 position, float volume = 1f)
        {
            var config = AudioPlayConfig.OneShot3D(position);
            config.Volume = volume;
            return PlayInternal(AudioChannelType.SFX, address, config);
        }

        public void StopSFX(AudioHandle handle)
        {
            if (handle != null && handle.Channel == AudioChannelType.SFX)
            {
                handle.Stop();
                RemoveHandle(handle);
            }
        }

        public void StopSFXByAddress(string address)
        {
            var toStop = new List<AudioHandle>();
            
            foreach (var kvp in _activeHandles)
            {
                if (kvp.Value.Channel == AudioChannelType.SFX && kvp.Value.Address == address)
                {
                    toStop.Add(kvp.Value);
                }
            }

            foreach (var handle in toStop)
            {
                StopSFX(handle);
            }
        }

        // ==================== Ambient ====================
        public AudioHandle PlayAmbient(string address, AudioPlayConfig config = null)
        {
            if (config == null)
            {
                config = AudioPlayConfig.Loop2D;
            }
            else
            {
                config.Loop = true; // 환경음은 항상 루프
            }
            
            return PlayInternal(AudioChannelType.Ambient, address, config);
        }

        public void StopAmbient(AudioHandle handle, float fadeOutDuration = 1f)
        {
            if (handle != null && handle.Channel == AudioChannelType.Ambient)
            {
                if (fadeOutDuration > 0f)
                {
                    StartFadeOut(handle, fadeOutDuration, () =>
                    {
                        handle.Stop();
                        RemoveHandle(handle);
                    });
                }
                else
                {
                    handle.Stop();
                    RemoveHandle(handle);
                }
            }
        }

        public void StopAmbientByAddress(string address, float fadeOutDuration = 1f)
        {
            if (_ambientHandlesByAddress.TryGetValue(address, out var handles))
            {
                var handlesCopy = new List<AudioHandle>(handles);
                foreach (var handle in handlesCopy)
                {
                    StopAmbient(handle, fadeOutDuration);
                }
            }
        }

        public void StopAllAmbient(float fadeOutDuration = 1f)
        {
            var allHandles = new List<AudioHandle>();
            foreach (var list in _ambientHandlesByAddress.Values)
            {
                allHandles.AddRange(list);
            }

            foreach (var handle in allHandles)
            {
                StopAmbient(handle, fadeOutDuration);
            }
        }

        // ==================== UI ====================
        public AudioHandle PlayUI(string address, float volume = 1f)
        {
            var config = new AudioPlayConfig { Volume = volume };
            return PlayInternal(AudioChannelType.UI, address, config);
        }

        // ==================== Voice ====================
        public AudioHandle PlayVoice(string address, AudioPlayConfig config = null)
        {
            // 기존 보이스 정지
            StopVoice(0f);
            
            if (config == null)
            {
                config = AudioPlayConfig.Default;
            }
            
            return PlayInternal(AudioChannelType.Voice, address, config);
        }

        public void StopVoice(float fadeOutDuration = 0.5f)
        {
            var toStop = new List<AudioHandle>();
            
            foreach (var kvp in _activeHandles)
            {
                if (kvp.Value.Channel == AudioChannelType.Voice)
                {
                    toStop.Add(kvp.Value);
                }
            }

            foreach (var handle in toStop)
            {
                if (fadeOutDuration > 0f)
                {
                    StartFadeOut(handle, fadeOutDuration, () =>
                    {
                        handle.Stop();
                        RemoveHandle(handle);
                    });
                }
                else
                {
                    handle.Stop();
                    RemoveHandle(handle);
                }
            }
        }

        // ==================== Handle Control ====================
        public void Stop(AudioHandle handle)
        {
            if (handle != null && handle.IsValid)
            {
                handle.Stop();
                RemoveHandle(handle);
            }
        }

        public void Pause(AudioHandle handle)
        {
            handle?.Pause();
        }

        public void Resume(AudioHandle handle)
        {
            handle?.Resume();
        }

        public void SetVolume(AudioHandle handle, float volume)
        {
            if (handle != null && handle.IsValid)
            {
                float finalVolume = _masterVolume * _channelVolumes[handle.Channel] * volume;
                if (_isMuted)
                {
                    finalVolume = 0f;
                }
                handle.SetVolume(finalVolume);
            }
        }

        // ==================== Volume Control ====================
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }

        public void SetChannelVolume(AudioChannelType channel, float volume)
        {
            _channelVolumes[channel] = Mathf.Clamp01(volume);
            _channelPools[channel].SetVolume(_channelVolumes[channel] * _masterVolume);
        }

        public float GetMasterVolume()
        {
            return _masterVolume;
        }

        public float GetChannelVolume(AudioChannelType channel)
        {
            return _channelVolumes[channel];
        }

        private void UpdateAllVolumes()
        {
            foreach (var kvp in _channelPools)
            {
                var channel = kvp.Key;
                var pool = kvp.Value;
                float channelVol = _channelVolumes[channel];
                pool.SetVolume(_masterVolume * channelVol);
            }
        }

        // ==================== Global Control ====================
        public void PauseAll()
        {
            foreach (var pool in _channelPools.Values)
            {
                pool.PauseAll();
            }
        }

        public void ResumeAll()
        {
            foreach (var pool in _channelPools.Values)
            {
                pool.ResumeAll();
            }
        }

        public void StopAll()
        {
            foreach (var pool in _channelPools.Values)
            {
                pool.StopAll();
            }

            _activeHandles.Clear();
            _ambientHandlesByAddress.Clear();
            _currentBGMHandle = null;
            _currentBGM = null;
        }

        public void PauseChannel(AudioChannelType channel)
        {
            _channelPools[channel].PauseAll();
        }

        public void ResumeChannel(AudioChannelType channel)
        {
            _channelPools[channel].ResumeAll();
        }

        public void StopChannel(AudioChannelType channel)
        {
            _channelPools[channel].StopAll();
            
            // 핸들 정리
            var toRemove = new List<AudioHandle>();
            foreach (var kvp in _activeHandles)
            {
                if (kvp.Value.Channel == channel)
                {
                    toRemove.Add(kvp.Value);
                }
            }
            
            foreach (var handle in toRemove)
            {
                RemoveHandle(handle);
            }
        }

        // ==================== Settings ====================
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat("Audio_MasterVolume", _masterVolume);
            PlayerPrefs.SetFloat("Audio_BGMVolume", _channelVolumes[AudioChannelType.BGM]);
            PlayerPrefs.SetFloat("Audio_SFXVolume", _channelVolumes[AudioChannelType.SFX]);
            PlayerPrefs.SetFloat("Audio_AmbientVolume", _channelVolumes[AudioChannelType.Ambient]);
            PlayerPrefs.SetFloat("Audio_UIVolume", _channelVolumes[AudioChannelType.UI]);
            PlayerPrefs.SetFloat("Audio_VoiceVolume", _channelVolumes[AudioChannelType.Voice]);
            PlayerPrefs.SetInt("Audio_Muted", _isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void LoadSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat("Audio_MasterVolume", 1f);
            _channelVolumes[AudioChannelType.BGM] = PlayerPrefs.GetFloat("Audio_BGMVolume", 1f);
            _channelVolumes[AudioChannelType.SFX] = PlayerPrefs.GetFloat("Audio_SFXVolume", 1f);
            _channelVolumes[AudioChannelType.Ambient] = PlayerPrefs.GetFloat("Audio_AmbientVolume", 1f);
            _channelVolumes[AudioChannelType.UI] = PlayerPrefs.GetFloat("Audio_UIVolume", 1f);
            _channelVolumes[AudioChannelType.Voice] = PlayerPrefs.GetFloat("Audio_VoiceVolume", 1f);
            _isMuted = PlayerPrefs.GetInt("Audio_Muted", 0) == 1;
            
            UpdateAllVolumes();
        }

        public void SetMute(bool mute)
        {
            _isMuted = mute;
            UpdateAllVolumes();
        }

        // ==================== Info ====================
        public string GetCacheInfo()
        {
            return _resourceManager.GetCacheInfo();
        }

        // ==================== Fade Effects ====================
        private void StartFadeIn(AudioHandle handle, float duration)
        {
            if (handle == null || !handle.IsValid)
            {
                return;
            }

            // 기존 페이드 중단
            StopFadeIfActive(handle.Id);

            // 새 페이드 시작
            var coroutine = StartCoroutine(FadeInCoroutine(handle, duration));
            _activeFadeCoroutines[handle.Id] = coroutine;
        }

        private void StartFadeOut(AudioHandle handle, float duration, System.Action onComplete = null)
        {
            if (handle == null || !handle.IsValid)
            {
                onComplete?.Invoke();
                return;
            }

            // 기존 페이드 중단
            StopFadeIfActive(handle.Id);

            // 새 페이드 시작
            var coroutine = StartCoroutine(FadeOutCoroutine(handle, duration, onComplete));
            _activeFadeCoroutines[handle.Id] = coroutine;
        }

        private void StopFadeIfActive(int handleId)
        {
            if (_activeFadeCoroutines.TryGetValue(handleId, out var existingCoroutine))
            {
                if (existingCoroutine != null)
                {
                    StopCoroutine(existingCoroutine);
                }
                _activeFadeCoroutines.Remove(handleId);
            }
        }

        private IEnumerator FadeInCoroutine(AudioHandle handle, float duration)
        {
            if (handle == null || !handle.IsValid)
            {
                yield break;
            }

            float startVolume = 0f;
            float targetVolume = _masterVolume * _channelVolumes[handle.Channel];
            float elapsed = 0f;

            handle.SetVolume(startVolume);

            while (elapsed < duration && handle.IsValid)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float volume = Mathf.Lerp(startVolume, targetVolume, t);
                handle.SetVolume(volume);
                yield return null;
            }

            if (handle.IsValid)
            {
                handle.SetVolume(targetVolume);
            }

            // 완료 후 추적에서 제거
            _activeFadeCoroutines.Remove(handle.Id);
        }

        private IEnumerator FadeOutCoroutine(AudioHandle handle, float duration, System.Action onComplete)
        {
            if (handle == null || !handle.IsValid)
            {
                onComplete?.Invoke();
                yield break;
            }

            float startVolume = handle.PooledSource.Source.volume;
            float elapsed = 0f;

            while (elapsed < duration && handle.IsValid)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float volume = Mathf.Lerp(startVolume, 0f, t);
                handle.SetVolume(volume);
                yield return null;
            }

            onComplete?.Invoke();

            // 완료 후 추적에서 제거
            _activeFadeCoroutines.Remove(handle.Id);
        }
    }
}
