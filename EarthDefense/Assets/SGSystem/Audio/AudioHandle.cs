namespace SG.Audio
{
    /// <summary>
    /// 재생 중인 오디오의 고유 핸들
    /// 특정 사운드 인스턴스를 찾아 제어할 수 있음
    /// </summary>
    public class AudioHandle
    {
        private static int _nextId = 0;

        /// <summary>고유 ID</summary>
        public int Id { get; private set; }

        /// <summary>재생 주소 (식별용)</summary>
        public string Address { get; internal set; }

        /// <summary>채널 타입</summary>
        public AudioChannelType Channel { get; internal set; }

        /// <summary>내부 AudioSource 참조</summary>
        internal PooledAudioSource PooledSource { get; set; }

        /// <summary>유효한 핸들인지 확인</summary>
        public bool IsValid => PooledSource != null && PooledSource.Source != null;

        /// <summary>재생 중인지 확인</summary>
        public bool IsPlaying => IsValid && PooledSource.IsPlaying;

        /// <summary>일시정지 상태인지 확인</summary>
        public bool IsPaused => IsValid && PooledSource.IsPaused;

        public AudioHandle()
        {
            Id = _nextId++;
        }

        /// <summary>재생 정지</summary>
        public void Stop()
        {
            if (IsValid)
            {
                PooledSource.Stop();
            }
        }

        /// <summary>일시정지</summary>
        public void Pause()
        {
            if (IsValid)
            {
                PooledSource.Pause();
            }
        }

        /// <summary>재개</summary>
        public void Resume()
        {
            if (IsValid)
            {
                PooledSource.Resume();
            }
        }

        /// <summary>볼륨 설정 (0~1)</summary>
        public void SetVolume(float volume)
        {
            if (IsValid)
            {
                PooledSource.SetVolume(volume);
            }
        }

        /// <summary>재생 진행률 (0~1)</summary>
        public float GetProgress()
        {
            if (IsValid)
            {
                return PooledSource.GetProgress();
            }
            return 0f;
        }

        /// <summary>핸들 무효화 (내부용)</summary>
        internal void Invalidate()
        {
            PooledSource = null;
        }
    }
}
