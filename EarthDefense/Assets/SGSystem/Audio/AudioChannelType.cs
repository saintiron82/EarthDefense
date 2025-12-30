namespace SG.Audio
{
    /// <summary>
    /// 오디오 채널 타입 정의
    /// 각 채널은 독립적인 볼륨 제어 가능
    /// </summary>
    public enum AudioChannelType
    {
        /// <summary>배경 음악 (단일 트랙, 크로스페이드 지원)</summary>
        BGM = 0,

        /// <summary>효과음 (동시 재생 다수, 원샷)</summary>
        SFX = 1,

        /// <summary>환경음 (루프, 레이어링 최대 4개)</summary>
        Ambient = 2,

        /// <summary>UI 사운드 (버튼 클릭 등)</summary>
        UI = 3,

        /// <summary>보이스 (캐릭터 대사 등)</summary>
        Voice = 4
    }
}
