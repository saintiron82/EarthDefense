# AudioService 사용 가이드

## 📋 목차
- [개요](#개요)
- [초기화](#초기화)
- [기본 사용법](#기본-사용법)
- [고급 기능](#고급-기능)
- [설정 관리](#설정-관리)
- [모범 사례](#모범-사례)

---

## 개요

AudioService는 Unity 게임의 모든 오디오를 통합 관리하는 서비스입니다.

### 주요 기능
- ✅ **5채널 독립 제어**: BGM, SFX, Ambient, UI, Voice
- ✅ **핸들 기반 제어**: 특정 사운드 인스턴스 추적 가능
- ✅ **페이드 효과**: 부드러운 볼륨 전환
- ✅ **3D 사운드**: 위치 기반 공간 음향
- ✅ **자동 풀링**: 메모리 효율적인 AudioSource 재사용

---

## 초기화

### ServiceHome 통합
```csharp
// ServiceHome이 자동으로 초기화
var audioService = App.Instance.ServiceHome.GetService<AudioService>();
```

### 직접 생성 (테스트용)
```csharp
var serviceRoot = new GameObject("[Services]");
var audioService = serviceRoot.AddComponent<AudioService>();
await audioService.Init();
```

---

## 기본 사용법

### 1. BGM 재생

```csharp
var audioService = App.Instance.ServiceHome.GetService<AudioService>();

// 기본 재생
audioService.PlayBGM("bgm_main");

// 페이드 인 2초
audioService.PlayBGM("bgm_battle", fadeInDuration: 2f);

// 크로스페이드 (자연스러운 전환)
audioService.CrossfadeBGM("bgm_victory", duration: 3f);

// BGM 정지
audioService.StopBGM(fadeOutDuration: 1.5f);

// BGM 일시정지/재개
audioService.PauseBGM();
audioService.ResumeBGM();
```

### 2. SFX (효과음)

```csharp
// 간단 재생
audioService.PlaySFX("sfx_explosion");

// 볼륨 지정
audioService.PlaySFX("sfx_footstep", volume: 0.5f);

// 핸들로 제어
var handle = audioService.PlaySFX("sfx_engine_loop");
handle.SetVolume(0.3f);
handle.Stop();

// 3D 사운드 (위치 기반)
Vector3 explosionPos = new Vector3(10, 0, 5);
audioService.PlaySFX3D("sfx_explosion", explosionPos, volume: 0.8f);

// 특정 주소의 모든 SFX 정지
audioService.StopSFXByAddress("sfx_engine_loop");
```

### 3. Ambient (환경음)

```csharp
// 환경음 재생 (자동 루프)
var forest = audioService.PlayAmbient("ambient_forest");

// 3D 환경음 (위치 기반)
var config = AudioPlayConfig.AmbientLoop3D(new Vector3(0, 0, 0));
config.Volume = 0.6f;
var wind = audioService.PlayAmbient("ambient_wind", config);

// 페이드 아웃으로 정지
audioService.StopAmbient(forest, fadeOutDuration: 2f);

// 주소로 정지
audioService.StopAmbientByAddress("ambient_forest");

// 모든 환경음 정지
audioService.StopAllAmbient(fadeOutDuration: 1f);
```

### 4. UI 사운드

```csharp
// 버튼 클릭
audioService.PlayUI("ui_button_click");

// 볼륨 조절
audioService.PlayUI("ui_notification", volume: 0.7f);
```

### 5. Voice (보이스)

```csharp
// 보이스 재생 (기존 보이스 자동 정지)
audioService.PlayVoice("voice_tutorial_01");

// 상세 설정
var config = new AudioPlayConfig
{
    Volume = 0.9f,
    FadeInDuration = 0.5f
};
audioService.PlayVoice("voice_dialogue_hero", config);

// 보이스 정지
audioService.StopVoice(fadeOutDuration: 0.5f);
```

---

## 고급 기능

### 핸들 기반 개별 제어

```csharp
// A 사운드 재생
var handleA = audioService.PlaySFX("sound_a");

// B 사운드 재생 (A는 계속 재생)
var handleB = audioService.PlaySFX("sound_b");

// A만 정지
handleA.Stop();
// 또는
audioService.Stop(handleA);

// 핸들 상태 확인
if (handleA.IsPlaying)
{
    Debug.Log($"A 재생 중, 진행률: {handleA.GetProgress() * 100}%");
}

// 핸들로 일시정지/재개
handleB.Pause();
handleB.Resume();
```

### 볼륨 제어 (3단계)

```csharp
// 최종 볼륨 = 마스터 × 채널 × 개별

// 1. 마스터 볼륨 (전체)
audioService.SetMasterVolume(0.8f);

// 2. 채널별 볼륨
audioService.SetChannelVolume(AudioChannelType.BGM, 0.7f);
audioService.SetChannelVolume(AudioChannelType.SFX, 0.9f);
audioService.SetChannelVolume(AudioChannelType.Ambient, 0.5f);

// 3. 개별 사운드 볼륨
var handle = audioService.PlaySFX("explosion", volume: 0.6f);

// 예시: 마스터 0.8 × BGM채널 0.7 × 개별 0.6 = 0.336 (33.6%)
```

### 음소거

```csharp
// 전체 음소거
audioService.SetMute(true);

// 음소거 해제
audioService.SetMute(false);

// 음소거 상태 확인
if (audioService.IsMuted)
{
    Debug.Log("현재 음소거 상태");
}
```

### 채널별 제어

```csharp
// 특정 채널 일시정지
audioService.PauseChannel(AudioChannelType.SFX);

// 특정 채널 재개
audioService.ResumeChannel(AudioChannelType.SFX);

// 특정 채널 정지
audioService.StopChannel(AudioChannelType.Ambient);

// 전체 일시정지/재개/정지
audioService.PauseAll();
audioService.ResumeAll();
audioService.StopAll();
```

### 페이드 효과

```csharp
// 페이드 인
var config = new AudioPlayConfig
{
    Loop = true,
    FadeInDuration = 3f
};
audioService.PlayAmbient("ambient_rain", config);

// 페이드 아웃
audioService.StopAmbient(handle, fadeOutDuration: 2f);

// BGM 크로스페이드 (가장 자연스러운 전환)
audioService.CrossfadeBGM("bgm_new", duration: 4f);
```

---

## 설정 관리

### 설정 저장/로드

```csharp
// 볼륨 설정 저장 (PlayerPrefs)
audioService.SaveSettings();

// 설정 로드 (자동으로 InitInternal에서 호출됨)
audioService.LoadSettings();

// 저장되는 항목:
// - Audio_MasterVolume
// - Audio_BGMVolume
// - Audio_SFXVolume
// - Audio_AmbientVolume
// - Audio_UIVolume
// - Audio_VoiceVolume
// - Audio_Muted
```

### 볼륨 UI 연동 예제

```csharp
// UI 슬라이더 이벤트
public void OnMasterVolumeChanged(float value)
{
    var audioService = App.Instance.ServiceHome.GetService<AudioService>();
    audioService.SetMasterVolume(value);
    audioService.SaveSettings();
}

public void OnBGMVolumeChanged(float value)
{
    var audioService = App.Instance.ServiceHome.GetService<AudioService>();
    audioService.SetChannelVolume(AudioChannelType.BGM, value);
    audioService.SaveSettings();
}

// 음소거 토글
public void OnMuteToggle(bool isMuted)
{
    var audioService = App.Instance.ServiceHome.GetService<AudioService>();
    audioService.SetMute(isMuted);
    audioService.SaveSettings();
}
```

---

## 모범 사례

### ✅ DO (권장)

```csharp
// 1. 핸들로 수명 관리
var handle = audioService.PlaySFX("long_sound");
// ... 나중에
handle.Stop();

// 2. 페이드로 부드러운 전환
audioService.CrossfadeBGM("new_bgm", duration: 2f);

// 3. 채널별 볼륨 분리
audioService.SetChannelVolume(AudioChannelType.SFX, 0.7f);

// 4. 3D 사운드는 위치 업데이트
var config = AudioPlayConfig.OneShot3D(enemyPosition);
audioService.PlaySFX3D("enemy_hit", enemyPosition);
```

### ❌ DON'T (비권장)

```csharp
// 1. 매 프레임 재생 (풀 고갈)
void Update()
{
    audioService.PlaySFX("footstep"); // ❌ 너무 자주 호출
}

// 해결: 타이머로 간격 조절
float footstepTimer = 0f;
void Update()
{
    if (isWalking && footstepTimer <= 0f)
    {
        audioService.PlaySFX("footstep");
        footstepTimer = 0.5f; // 0.5초마다
    }
    footstepTimer -= Time.deltaTime;
}

// 2. 핸들 무시하고 중복 재생
audioService.PlayBGM("bgm");
audioService.PlayBGM("bgm"); // ❌ 중복
// 해결: 현재 BGM 확인
if (audioService.CurrentBGM != "bgm")
{
    audioService.PlayBGM("bgm");
}

// 3. 즉시 정지 (딱딱함)
audioService.StopBGM(0f); // ❌ 갑작스러움
// 해결: 페이드 아웃
audioService.StopBGM(1.5f); // ✅ 자연스러움
```

---

## 디버깅

### 캐시 정보 확인

```csharp
var audioService = App.Instance.ServiceHome.GetService<AudioService>();
string cacheInfo = audioService.GetCacheInfo();
Debug.Log(cacheInfo);
// 출력 예시: "Cache: 15/100 clips, Memory: 12MB / 100MB"
```

### 현재 상태 확인

```csharp
// BGM 상태
Debug.Log($"Current BGM: {audioService.CurrentBGM}");
Debug.Log($"BGM Playing: {audioService.IsBGMPlaying}");

// 볼륨 상태
Debug.Log($"Master: {audioService.GetMasterVolume()}");
Debug.Log($"BGM: {audioService.GetChannelVolume(AudioChannelType.BGM)}");

// 음소거 상태
Debug.Log($"Muted: {audioService.IsMuted}");
```

---

## 성능 최적화 팁

### 1. **풀 크기 조정**
```csharp
// AudioService.cs 상수 수정
private const int MAX_SFX_POOL = 16; // SFX 동시 재생 수
private const int MAX_AMBIENT_POOL = 4; // 환경음 레이어 수
```

### 2. **캐시 크기 조정**
```csharp
// DirectInit()에서
_resourceManager = new AudioResourceManager(
    maxCacheSize: 100,           // 클립 개수
    maxMemoryBytes: 100 * 1024 * 1024 // 100MB
);
```

### 3. **핸들 정리 주기 조정**
```csharp
// 프레임 드랍 시 주기 늘리기
private const int CLEANUP_INTERVAL = 60; // 1초마다 (60fps 기준)
```

---

## FAQ

**Q: AudioService를 여러 씬에서 사용할 수 있나요?**
A: 네, ServiceHome이 DontDestroyOnLoad로 관리하므로 씬 전환 시에도 유지됩니다.

**Q: BGM이 재생 안 됩니다.**
A: Addressables 경로 확인: `Assets/AddressableResources/BGM/{address}.ogg`

**Q: SFX 풀이 꽉 찼습니다.**
A: `MAX_SFX_POOL` 값을 늘리거나, 재생 빈도를 줄이세요.

**Q: 페이드가 작동 안 합니다.**
A: MonoServiceBase 상속 확인 및 Coroutine이 정상 동작하는지 확인하세요.

**Q: 3D 사운드가 들리지 않습니다.**
A: AudioListener가 씬에 있는지, 그리고 카메라와 함께 이동하는지 확인하세요.

---

## 라이센스
MIT License
