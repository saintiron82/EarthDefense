# SaveService 사용 가이드

## 📋 목차
- [개요](#개요)
- [아키텍처](#아키텍처)
- [기본 사용법](#기본-사용법)
- [고급 기능](#고급-기능)
- [커스터마이징](#커스터마이징)
- [모범 사례](#모범-사례)

---

## 개요

SaveService는 Unity 게임의 저장/로드를 통합 관리하는 범용 서비스입니다.

### 주요 기능
- ✅ **ISaveable 인터페이스**: 어떤 데이터든 저장 가능
- ✅ **SaveData 베이스 클래스**: 메타데이터 표준화
- ✅ **슬롯 관리**: 자동 슬롯 ID 생성 및 메타 정보 캐싱
- ✅ **버전 관리**: 호환성 체크 및 마이그레이션 지원
- ✅ **훅 시스템**: 저장/로드 전후 커스터마이징

---

## 아키텍처

### 계층 구조

```
┌──────────────────────────────┐
│  SaveData (추상 베이스)          │
│  - 메타데이터 표준화              │
│  - Save/Load 템플릿 메서드       │
│  - 훅 시스템                     │
└──────────────────────────────┘
           ↑ 상속
┌──────────────────────────────┐
│  PlayData / SettingsData      │
│  (게임 특화 데이터)              │
│  - 게임 상태                     │
│  - 설정 값                       │
└──────────────────────────────┘
           ↓ 사용
┌──────────────────────────────┐
│  SaveService (범용 인프라)      │
│  - ISaveable 직렬화            │
│  - 파일 I/O                     │
│  - 슬롯 관리                     │
└──────────────────────────────┘
```

### 핵심 인터페이스

```csharp
public interface ISaveable
{
    int Version { get; }
    string SaveId { get; set; }
    string Serialize();
    void Deserialize(string data);
}
```

---

## 기본 사용법

### 1. **SaveData 상속**

```csharp
using System;
using UnityEngine;
using SG.Save;

[Serializable]
public class MyGameData : SaveData
{
    // 메타데이터는 SaveData에서 자동 제공
    // - version
    // - SaveId
    // - SaveFileName
    // - LastSaveTime
    // - TotalPlayTime
    // - GameLevel

    // 게임 특화 데이터만 추가
    public int PlayerLevel;
    public long Gold;
    public string PlayerName;
    public List<string> UnlockedItems = new List<string>();

    // 훅 메서드 오버라이드 (옵션)
    protected override void OnBeforeSave()
    {
        base.OnBeforeSave();
        TotalPlayTime = Time.time; // 플레이 시간 자동 갱신
    }

    protected override string GenerateDefaultDisplayName()
    {
        return $"{PlayerName} - Lv.{PlayerLevel}";
    }
}
```

### 2. **데이터 저장**

```csharp
// 게임 매니저에서
var gameData = new MyGameData
{
    PlayerLevel = 10,
    Gold = 5000,
    PlayerName = "Player1"
};

// 방법 1: 간편 저장 (SaveService 자동 조회)
gameData.Save("slot_01", "My Progress");

// 방법 2: 명시적 저장 (성능 최적화)
var saveService = App.Instance.ServiceHome.GetService<SaveService>();
gameData.SaveTo(saveService, "slot_01", "My Progress");
```

### 3. **데이터 로드**

```csharp
// 정적 메서드로 로드 (타입 안전)
var loadedData = MyGameData.LoadFrom<MyGameData>(
    saveService, 
    "slot_01"
);

if (loadedData != null)
{
    // 게임 상태 복원
    ApplyGameData(loadedData);
}
```

### 4. **슬롯 목록 조회**

```csharp
// 모든 세이브 슬롯 조회
var saveService = App.Instance.ServiceHome.GetService<SaveService>();
var slots = saveService.GetAllSlots();

foreach (var slot in slots)
{
    Debug.Log($"{slot.DisplayName}");
    Debug.Log($"  저장 시각: {slot.GetFormattedSaveTime()}");
    Debug.Log($"  플레이 시간: {slot.GetFormattedPlayTime()}");
    Debug.Log($"  레벨: {slot.GameLevel}");
}
```

---

## 고급 기능

### 1. **자동 저장**

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private float _autoSaveInterval = 300f; // 5분
    private float _autoSaveTimer = 0f;
    private SaveService _saveService;
    private MyGameData _gameData;

    private void Awake()
    {
        _saveService = App.Instance.ServiceHome.GetService<SaveService>();
    }

    private void Update()
    {
        _autoSaveTimer += Time.deltaTime;
        
        if (_autoSaveTimer >= _autoSaveInterval)
        {
            _autoSaveTimer = 0f;
            _gameData.SaveTo(_saveService, "autosave", "Auto Save");
        }
    }
}
```

### 2. **버전 마이그레이션**

```csharp
[Serializable]
public class MyGameData : SaveData
{
    // 버전 2로 업그레이드
    protected override int version => 2;

    public override void Deserialize(string data)
    {
        base.Deserialize(data);

        // 버전 1 데이터 마이그레이션
        if (Version < 2)
        {
            MigrateFromV1ToV2();
        }
    }

    private void MigrateFromV1ToV2()
    {
        // 예: 새 필드 기본값 설정
        if (UnlockedItems == null)
        {
            UnlockedItems = new List<string>();
        }
    }
}
```

### 3. **세이브 슬롯 삭제**

```csharp
var saveService = App.Instance.ServiceHome.GetService<SaveService>();

// 특정 슬롯 삭제
saveService.Delete("slot_01");

// 삭제 전 확인
if (saveService.Exists("slot_01"))
{
    var slotInfo = saveService.GetSlotInfo("slot_01");
    Debug.Log($"'{slotInfo.DisplayName}' 슬롯을 삭제하시겠습니까?");
    
    // 유저 확인 후 삭제
    saveService.Delete("slot_01");
}
```

### 4. **커스텀 직렬화**

```csharp
[Serializable]
public class MyGameData : SaveData
{
    // Unity의 JsonUtility는 Dictionary를 지원하지 않음
    [NonSerialized]
    public Dictionary<string, int> ItemCounts = new Dictionary<string, int>();

    // 직렬화 가능한 대체 구조
    public List<string> ItemKeys = new List<string>();
    public List<int> ItemValues = new List<int>();

    public override string Serialize()
    {
        // Dictionary → List 변환
        ItemKeys.Clear();
        ItemValues.Clear();
        
        foreach (var kvp in ItemCounts)
        {
            ItemKeys.Add(kvp.Key);
            ItemValues.Add(kvp.Value);
        }

        return base.Serialize();
    }

    public override void Deserialize(string data)
    {
        base.Deserialize(data);

        // List → Dictionary 변환
        ItemCounts.Clear();
        for (int i = 0; i < ItemKeys.Count; i++)
        {
            ItemCounts[ItemKeys[i]] = ItemValues[i];
        }
    }
}
```

---

## 커스터마이징

### 1. **SaveData 훅 메서드**

```csharp
[Serializable]
public class MyGameData : SaveData
{
    /// <summary>
    /// 저장 직전 호출
    /// </summary>
    protected override void OnBeforeSave()
    {
        base.OnBeforeSave();
        
        // 예: 플레이 시간 자동 갱신
        TotalPlayTime = Time.time;
        
        // 예: 게임 레벨 자동 설정
        GameLevel = PlayerLevel;
    }

    /// <summary>
    /// 저장 직후 호출
    /// </summary>
    protected override void OnAfterSave()
    {
        base.OnAfterSave();
        
        // 예: 저장 완료 알림
        Debug.Log($"저장 완료: {SaveFileName}");
        ShowNotification("게임이 저장되었습니다.");
    }

    /// <summary>
    /// 로드 직후 호출
    /// </summary>
    protected override void OnAfterLoad()
    {
        base.OnAfterLoad();
        
        // 예: NonSerialized 필드 초기화
        if (ItemCounts == null)
        {
            ItemCounts = new Dictionary<string, int>();
        }
        
        // 예: 로드 완료 알림
        Debug.Log($"로드 완료: {SaveFileName}");
    }

    /// <summary>
    /// 기본 표시 이름 생성
    /// </summary>
    protected override string GenerateDefaultDisplayName()
    {
        return $"{PlayerName} - Lv.{PlayerLevel} - {DateTime.Now:yyyy-MM-dd HH:mm}";
    }
}
```

### 2. **설정 데이터 예제**

```csharp
using System;
using UnityEngine;
using SG.Save;

[Serializable]
public class SettingsData : SaveData
{
    // 오디오 설정
    public float MasterVolume = 1f;
    public float BGMVolume = 1f;
    public float SFXVolume = 1f;
    public bool Muted = false;

    // 그래픽 설정
    public int QualityLevel = 2;
    public bool FullScreen = true;
    public int ResolutionWidth = 1920;
    public int ResolutionHeight = 1080;

    // 게임플레이 설정
    public string Language = "ko";
    public bool ShowTutorial = true;

    protected override string GenerateDefaultDisplayName()
    {
        return "Settings";
    }

    protected override void OnAfterLoad()
    {
        base.OnAfterLoad();
        
        // 설정 적용
        ApplySettings();
    }

    private void ApplySettings()
    {
        // 오디오 적용
        var audioService = App.Instance.ServiceHome.GetService<AudioService>();
        if (audioService != null)
        {
            audioService.SetMasterVolume(MasterVolume);
            audioService.SetChannelVolume(AudioChannelType.BGM, BGMVolume);
            audioService.SetChannelVolume(AudioChannelType.SFX, SFXVolume);
            audioService.SetMute(Muted);
        }

        // 그래픽 적용
        QualitySettings.SetQualityLevel(QualityLevel);
        Screen.SetResolution(ResolutionWidth, ResolutionHeight, FullScreen);
    }
}

// 사용
var settings = new SettingsData();
settings.Save("settings");

// 로드 (설정 자동 적용)
var loadedSettings = SettingsData.LoadFrom<SettingsData>(saveService, "settings");
```

---

## 모범 사례

### ✅ DO (권장)

```csharp
// 1. SaveData 상속으로 표준화
[Serializable]
public class MyData : SaveData { }

// 2. 명시적 SaveService 전달 (자동 저장 시)
private SaveService _saveService;
myData.SaveTo(_saveService, "autosave");

// 3. 훅 메서드로 로직 분리
protected override void OnBeforeSave()
{
    TotalPlayTime = Time.time;
}

// 4. 정적 로드 메서드로 타입 안전
var data = MyData.LoadFrom<MyData>(saveService, "slot_01");

// 5. 버전 관리로 호환성 유지
protected override int version => 2;
```

### ❌ DON'T (비권장)

```csharp
// 1. ISaveable 직접 구현 (비표준화)
public class MyData : ISaveable { } // ❌ SaveData 상속 권장

// 2. SaveService 매번 조회 (비효율)
for (int i = 0; i < 100; i++)
{
    myData.Save("slot"); // ❌ 매번 GetService 호출
}

// 3. 메타데이터 수동 관리 (실수 위험)
myData.LastSaveTime = DateTime.Now.ToString(); // ❌ 자동 설정됨

// 4. 직렬화 불가능한 타입 직접 저장
public Dictionary<string, object> Data; // ❌ JsonUtility 미지원

// 5. 버전 체크 생략 (호환성 문제)
public override void Deserialize(string data)
{
    JsonUtility.FromJsonOverwrite(data, this); // ❌ 버전 체크 없음
}
```

---

## 파일 저장 위치

```
Windows: C:\Users\<username>\AppData\LocalLow\<CompanyName>\<ProductName>\Saves\
macOS:   ~/Library/Application Support/<CompanyName>/<ProductName>/Saves/
Linux:   ~/.config/unity3d/<CompanyName>/<ProductName>/Saves/

파일 구조:
  Saves/
    ├─ slot_01.data.json  (실제 게임 데이터)
    ├─ slot_01.meta.json  (메타 정보: 저장 시각, 플레이 시간 등)
    ├─ autosave.data.json
    ├─ autosave.meta.json
    └─ settings.data.json
```

---

## 디버깅

### 저장 경로 확인

```csharp
var saveService = App.Instance.ServiceHome.GetService<SaveService>();
Debug.Log($"Save folder: {saveService.SaveFolderPath}");

// Windows 탐색기에서 열기
Application.OpenURL(saveService.SaveFolderPath);
```

### 슬롯 정보 출력

```csharp
var slots = saveService.GetAllSlots();
foreach (var slot in slots)
{
    Debug.Log($"[{slot.SlotId}] {slot.DisplayName}");
    Debug.Log($"  Version: {slot.Version}");
    Debug.Log($"  Saved: {slot.GetFormattedSaveTime()}");
    Debug.Log($"  PlayTime: {slot.GetFormattedPlayTime()}");
}
```

---

## FAQ

**Q: 다른 게임에도 이식 가능한가요?**
A: 네! SaveService, ISaveable, SaveData는 게임 로직과 완전히 독립적입니다.

**Q: JSON 말고 바이너리로 저장할 수 있나요?**
A: `Serialize()/Deserialize()` 메서드를 오버라이드하여 구현 가능합니다.

**Q: 클라우드 세이브를 지원하나요?**
A: SaveService는 로컬 파일 I/O만 제공합니다. 클라우드는 별도 서비스가 필요합니다.

**Q: 세이브 파일을 암호화할 수 있나요?**
A: `Serialize()` 후 암호화, `Deserialize()` 전 복호화를 추가하면 됩니다.

**Q: 여러 세이브 슬롯을 동시에 사용할 수 있나요?**
A: 네, 각 슬롯은 독립적으로 관리됩니다.

---

## 라이센스
MIT License
