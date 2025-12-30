using UnityEngine;

#if USE_UNITASK
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace SG.Audio
{
    /// <summary>
    /// 오디오 서비스 인터페이스
    /// BGM, SFX, Ambient, UI, Voice 채널 관리
    /// </summary>
    public interface IAudioService : IService
    {
        // ==================== BGM ====================
        /// <summary>BGM 재생</summary>
        AudioHandle PlayBGM(string address, float fadeInDuration = 1f);
        
        /// <summary>BGM 정지</summary>
        void StopBGM(float fadeOutDuration = 1f);
        
        /// <summary>BGM 일시정지</summary>
        void PauseBGM();
        
        /// <summary>BGM 재개</summary>
        void ResumeBGM();
        
        /// <summary>BGM 크로스페이드 (현재 BGM을 페이드아웃하며 새 BGM 페이드인)</summary>
        AudioHandle CrossfadeBGM(string address, float duration = 2f);

        // ==================== SFX ====================
        /// <summary>효과음 원샷 재생 (핸들 반환)</summary>
        AudioHandle PlaySFX(string address, AudioPlayConfig config = null);
        
        /// <summary>효과음 원샷 재생 (간편 버전 - 볼륨만 지정)</summary>
        AudioHandle PlaySFX(string address, float volume = 1f);
        
        /// <summary>3D 효과음 재생</summary>
        AudioHandle PlaySFX3D(string address, Vector3 position, float volume = 1f);

        /// <summary>특정 SFX 핸들 정지</summary>
        void StopSFX(AudioHandle handle);

        /// <summary>특정 주소의 모든 SFX 정지</summary>
        void StopSFXByAddress(string address);

        // ==================== Ambient ====================
        /// <summary>환경음 재생 (루프, 핸들 반환)</summary>
        AudioHandle PlayAmbient(string address, AudioPlayConfig config = null);
        
        /// <summary>특정 환경음 핸들 정지</summary>
        void StopAmbient(AudioHandle handle, float fadeOutDuration = 1f);

        /// <summary>특정 주소의 환경음 정지</summary>
        void StopAmbientByAddress(string address, float fadeOutDuration = 1f);
        
        /// <summary>모든 환경음 정지</summary>
        void StopAllAmbient(float fadeOutDuration = 1f);

        // ==================== UI ====================
        /// <summary>UI 사운드 재생</summary>
        AudioHandle PlayUI(string address, float volume = 1f);

        // ==================== Voice ====================
        /// <summary>보이스 재생</summary>
        AudioHandle PlayVoice(string address, AudioPlayConfig config = null);
        
        /// <summary>보이스 정지</summary>
        void StopVoice(float fadeOutDuration = 0.5f);

        // ==================== Handle Control ====================
        /// <summary>핸들로 특정 사운드 정지</summary>
        void Stop(AudioHandle handle);

        /// <summary>핸들로 특정 사운드 일시정지</summary>
        void Pause(AudioHandle handle);

        /// <summary>핸들로 특정 사운드 재개</summary>
        void Resume(AudioHandle handle);

        /// <summary>핸들로 특정 사운드 볼륨 설정</summary>
        void SetVolume(AudioHandle handle, float volume);

        // ==================== Volume Control ====================
        /// <summary>마스터 볼륨 설정 (0~1)</summary>
        void SetMasterVolume(float volume);
        
        /// <summary>채널 볼륨 설정 (0~1)</summary>
        void SetChannelVolume(AudioChannelType channel, float volume);
        
        /// <summary>마스터 볼륨 가져오기</summary>
        float GetMasterVolume();
        
        /// <summary>채널 볼륨 가져오기</summary>
        float GetChannelVolume(AudioChannelType channel);

        // ==================== Global Control ====================
        /// <summary>모든 오디오 일시정지</summary>
        void PauseAll();
        
        /// <summary>모든 오디오 재개</summary>
        void ResumeAll();
        
        /// <summary>모든 오디오 정지</summary>
        void StopAll();
        
        /// <summary>채널별 일시정지</summary>
        void PauseChannel(AudioChannelType channel);
        
        /// <summary>채널별 재개</summary>
        void ResumeChannel(AudioChannelType channel);
        
        /// <summary>채널별 정지</summary>
        void StopChannel(AudioChannelType channel);

        // ==================== Settings ====================
        /// <summary>설정 저장 (PlayerPrefs)</summary>
        void SaveSettings();
        
        /// <summary>설정 로드 (PlayerPrefs)</summary>
        void LoadSettings();
        
        /// <summary>음소거 설정</summary>
        void SetMute(bool mute);
        
        /// <summary>음소거 상태 확인</summary>
        bool IsMuted { get; }

        // ==================== Info ====================
        /// <summary>현재 재생 중인 BGM 주소</summary>
        string CurrentBGM { get; }
        
        /// <summary>BGM 재생 중 여부</summary>
        bool IsBGMPlaying { get; }
        
        /// <summary>캐시 정보</summary>
        string GetCacheInfo();
    }
}
