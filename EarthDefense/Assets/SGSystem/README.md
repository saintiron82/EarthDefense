# 리소스 & 풀링 시스템

Unity 프로젝트를 위한 독립적인 리소스 로딩 및 오브젝트 풀링 시스템입니다.

## 구조

```
Assets/Script/System/
├─ Service/
│   ├─ ServiceBase.cs             - 순수 C# 서비스 베이스
│   ├─ MonoServiceBase.cs         - MonoBehaviour 서비스 베이스 (NEW!)
│   └─ IService.cs                - 서비스 인터페이스
├─ Resource/
│   ├─ ResourceService.cs         - 리소스 로딩 및 캐시 관리 (MonoServiceBase 상속)
│   ├─ ResourceBundle.cs          - ScriptableObject 리소스 번들
│   ├─ ResourceEntry.cs           - 개별 리소스 메타데이터
│   ├─ ResourceType.cs            - 리소스 타입 열거형
│   ├─ PoolConfig.cs              - 풀링 설정
│   ├─ PoolConfigPreset.cs        - 풀링 설정 프리셋
│   └─ Presets/                   - 기본 프리셋들
│       ├─ PoolPreset_Disabled.asset
│       ├─ PoolPreset_Low.asset
│       ├─ PoolPreset_Normal.asset
│       ├─ PoolPreset_High.asset
│       ├─ PoolPreset_VeryHigh.asset
│       └─ PoolPreset_UI.asset
└─ Pool/
    ├─ PoolService.cs             - Unity ObjectPool 기반 풀 관리 (MonoServiceBase 상속)
    └─ IPoolable.cs               - 풀링 생명주기 인터페이스
```

## MonoServiceBase (NEW!)

### ServiceBase vs MonoServiceBase

| 특징 | ServiceBase | MonoServiceBase |
|------|-------------|-----------------|
| **상속** | 순수 C# 클래스 | MonoBehaviour |
| **Inspector 설정** | ❌ 불가 | ✅ 가능 |
| **SerializeField** | ❌ 불가 | ✅ 가능 |
| **씬 배치** | ❌ 불가 | ✅ 가능 |
| **Transform 계층** | ❌ 불가 | ✅ 가능 |
| **사용 사례** | 순수 로직 서비스 | Unity 의존 서비스 |

### MonoServiceBase 구조

```csharp
public abstract class MonoServiceBase : MonoBehaviour, IService
{
    // IService 구현
    public bool IsPrepare { get; }
    public bool IsRelease { get; }
    
    // 비동기 초기화
    public virtual async UniTask<bool> Init()
    
    // 동기 초기화 (Awake/Start에서 호출 가능)
    public virtual void DirectInit()
    
    // 비동기 준비
    public virtual async UniTask<bool> Prepare()
    
    // 동기 준비
    public virtual void DirectPrepare()
    
    // 해제
    public virtual void Release()
    
    // 파괴
    public virtual void Destroy()
}
```

### 사용 예시

```csharp
public class ResourceService : MonoServiceBase
{
    [Header("Resource Bundles")]
    [SerializeField] private List<ResourceBundle> _bundles;  // ← Inspector 할당 가능
    
    public override void DirectInit()
    {
        base.DirectInit();
        // Awake/Start에서 호출 가능한 동기 초기화
        Initialize();
    }
    
    public override async UniTask<bool> Init()
    {
        await base.Init();
        // ServiceHome에서 호출되는 비동기 초기화
        return true;
    }
}
```

## 주요 기능

### ResourceService (MonoServiceBase 상속)
- **MonoServiceBase 통합**: Unity Inspector 설정 지원
- **ServiceBase 생명주기**: Init/Prepare/Release/Destroy 준수
- **UniTask 기반**: 비동기 초기화 및 로딩
- **캐시 우선 로딩**: 한번 로드된 리소스 재사용
- **그룹 단위 관리**: 레벨/카테고리별 리소스 묶음 관리
- **Fallback 체인**: ScriptableObject → Resources 폴더
- **싱글톤 패턴**: ResourceService.Instance로 전역 접근

### PoolService (MonoServiceBase 상속 + Unity ObjectPool 기반)
- **MonoServiceBase 통합**: Unity Inspector 설정 지원
- **Unity ObjectPool 통합**: `UnityEngine.Pool.ObjectPool<T>` 기반
- **ServiceBase 생명주기**: Init/Prepare/Release/Destroy 준수
- **UniTask 기반**: 비동기 사전 로드
- **자동 생명주기 관리**: Create/Get/Release/Destroy 콜백 자동 처리
- **프리셋 기반 설정**: 재사용 가능한 풀 설정 프리셋
- **Collection Check**: 중복 Release 방지
- **IPoolable 지원**: OnSpawnFromPool/OnReturnToPool 자동 호출
- **ResourceService 연동**: 원본 프리팹 자동 로드
- **싱글톤 패턴**: PoolService.Instance로 전역 접근

## Unity ObjectPool 장점

### 기본 제공 기능
1. **메모리 최적화**: 효율적인 객체 재사용
2. **자동 관리**: Create/Get/Release/Destroy 라이프사이클 자동 처리
3. **Thread-Safe 옵션**: 멀티스레드 환경 지원
4. **Collection Check**: 중복 Release 감지 및 경고
5. **통계 제공**: CountActive/CountInactive로 상태 모니터링

### 사용 예시
```csharp
// Unity ObjectPool 직접 사용 (참고용)
var pool = new ObjectPool<GameObject>(
    createFunc: () => Instantiate(prefab),
    actionOnGet: (obj) => obj.SetActive(true),
    actionOnRelease: (obj) => obj.SetActive(false),
    actionOnDestroy: (obj) => Destroy(obj),
    collectionCheck: true,
    defaultCapacity: 10,
    maxSize: 100
);

// PoolService는 이를 내부적으로 사용
```

## 서비스 통합

### ServiceHome 등록

```csharp
using Script.SystemCore.Resource;
using Script.SystemCore.Pool;

public class ServiceHome
{
    public async UniTask Init()
    {
        // MonoServiceBase 서비스는 씬에 배치 필요
        // 1. 씬에 GameObject 생성 → ResourceService 컴포넌트 추가
        // 2. 씬에 GameObject 생성 → PoolService 컴포넌트 추가
        
        // ServiceHome에 등록 (씬에서 찾아서 등록)
        var resourceService = GameObject.FindObjectOfType<ResourceService>();
        if (resourceService != null)
        {
            AddNewService(resourceService);
        }
        
        var poolService = GameObject.FindObjectOfType<PoolService>();
        if (poolService != null)
        {
            AddNewService(poolService);
        }
        
        // 다른 서비스들...
        AddNewService<UIService>();
    }
}
```

### 씬 설정

```
Hierarchy:
├─ SystemServices (GameObject)
│   ├─ ResourceService (Component)
│   │   └─ Bundles: [UIBundle, GameplayBundle] ← Inspector 할당
│   └─ PoolService (Component)
│       └─ ResourceService: [ResourceService] ← Inspector 할당
```

### 서비스 생명주기

```csharp
// MonoServiceBase는 두 가지 초기화 방식 지원

// 1. 동기 초기화 (Awake/Start)
private void Awake()
{
    resourceService.DirectInit();
    poolService.DirectInit();
}

// 2. 비동기 초기화 (ServiceHome)
await resourceService.Init();
await poolService.Init();

await resourceService.Prepare();
await poolService.Prepare();

// 사용
var sprite = resourceService.LoadSprite("icon_wood");
var obj = poolService.Get("cargo_red");

// 반환 (Unity ObjectPool 자동 관리)
poolService.Return(obj);

// 해제
resourceService.Release();
poolService.Release();

// 파괴
resourceService.Destroy();  // GameObject도 함께 파괴
poolService.Destroy();
```

## 풀링 프리셋 시스템

### 기본 제공 프리셋

| 프리셋 | 사전 생성 | 최대 크기 | 용도 |
|--------|-----------|-----------|------|
| **Disabled** | 0 | 0 | 풀링 비활성화 |
| **Low** | 5 | 20 | 저빈도 사용 (튜토리얼, 일회성 UI) |
| **Normal** | 20 | 100 | 일반 게임플레이 오브젝트 |
| **High** | 50 | 200 | 고빈도 사용 (이펙트, 총알, UI 아이콘) |
| **VeryHigh** | 100 | 500 | 초고빈도 (파티클, 데미지 텍스트) |
| **UI** | 30 | 150 | UI 전용 (아이콘, 버튼, 리스트 항목) |

### 프리셋 계층 구조

```
PoolConfigPreset (재사용 가능)
    ↓ 참조
ResourceBundle
    - defaultPoolPreset (번들 기본)
    - entries[]
        - poolPresetOverride (개별 오버라이드)
```

## 사용 방법

### 1. 씬 설정

#### 단계 1: GameObject 생성

```
1. Hierarchy에서 우클릭
2. Create Empty → "SystemServices" 생성
3. SystemServices 선택 후 Add Component
   - ResourceService 추가
   - PoolService 추가
```

#### 단계 2: Inspector 설정

**ResourceService:**
```
[Resource Bundles]
- Size: 2
  - Element 0: UIBundle (ScriptableObject 할당)
  - Element 1: GameplayBundle (ScriptableObject 할당)
```

**PoolService:**
```
[Dependencies]
- Resource Service: [ResourceService] (같은 GameObject 또는 다른 GameObject)

[Global Fallback Settings]
- Fallback Pool Config:
  - Enabled: true
  - Preload Count: 10
  - Max Pool Size: 50
  - Auto Expand: true
```

### 2. 풀링 프리셋 생성 (선택)

기본 프리셋을 사용하거나 커스텀 프리셋을 생성할 수 있습니다.

```
1. Project 창에서 우클릭
2. Create > SystemCore > Pool Config Preset
3. Inspector에서 설정:
   - description: "커스텀 프리셋 설명"
   - config 설정
```

### 3. ResourceBundle 생성

```
1. Project 창에서 우클릭
2. Create > SystemCore > Resource Bundle
3. Inspector에서 설정:
   
   [Bundle Info]
   - bundleId: "UIResources"
   
   [Default Pool Settings]
   - defaultPoolPreset: PoolPreset_UI 선택
   
   [Resources]
   - entries 추가:
     - id: "icon_wood"
     - directAsset: 스프라이트 할당
     - cacheGroup: "UI_Common"
     - poolPresetOverride: (비워두면 번들 기본값 사용)
```

### 4. 기본 사용

#### 리소스 로딩

```csharp
// 싱글톤 접근
var resourceService = ResourceService.Instance;

// 동기 로딩
Sprite icon = resourceService.LoadSprite("icon_wood");
GameObject prefab = resourceService.LoadPrefab("cargo_red");

// 비동기 로딩 (UniTask)
await resourceService.LoadSpriteAsync("icon_wood", (sprite) => 
{
    if (sprite != null)
    {
        image.sprite = sprite;
    }
});
```

#### 풀링 사용 (Unity ObjectPool 기반)

```csharp
// 싱글톤 접근
var poolService = PoolService.Instance;

// 오브젝트 가져오기 (Unity ObjectPool.Get 내부 호출)
GameObject cargo = poolService.Get("cargo_red");
Cargo cargoComponent = poolService.Get<Cargo>("cargo_red");

// 위치 지정
GameObject obj = poolService.Get("cargo_red", position, rotation);

// 반환 (Unity ObjectPool.Release 내부 호출)
poolService.Return(cargo);

// 비동기 사전 로드 (UniTask)
await poolService.PreloadAsync("cargo_red", 20, () => 
{
    Debug.Log("Preload complete");
});

// 상태 조회 (Unity ObjectPool.CountActive/CountInactive)
int active = poolService.GetActiveCount("cargo_red");
int inactive = poolService.GetInactiveCount("cargo_red");
```

### 5. IPoolable 인터페이스 (선택 사항)

Unity ObjectPool의 actionOnGet/actionOnRelease 콜백에서 자동 호출됩니다.

```csharp
using Script.SystemCore.Pool;

public class Cargo : MonoBehaviour, IPoolable
{
    public void OnSpawnFromPool()
    {
        // ObjectPool.Get() 시 자동 호출
        Debug.Log("Cargo spawned from pool");
        ResetState();
    }

    public void OnReturnToPool()
    {
        // ObjectPool.Release() 시 자동 호출
        Debug.Log("Cargo returned to pool");
        ClearReferences();
    }
}
```

**IPoolable 없이도 풀링 가능:**
- 기본 동작만으로 충분한 경우 IPoolable 구현 불필요
- SetActive(true/false)만으로 충분한 간단한 오브젝트

## MonoServiceBase 활용 팁

### Inspector 설정 활용

```csharp
public class PoolService : MonoServiceBase
{
    [Header("Dependencies")]
    [SerializeField] private ResourceService _resourceService;  // Inspector 할당
    
    [Header("Settings")]
    [SerializeField] private int _defaultCapacity = 10;
    [SerializeField] private bool _enableDebugLogs = true;
    
    // Transform 계층 활용
    private void CreatePoolContainer(string id)
    {
        var container = new GameObject($"Pool_{id}");
        container.transform.SetParent(transform);  // ← MonoBehaviour이므로 transform 사용 가능
    }
}
```

### 씬 간 유지 (DontDestroyOnLoad)

```csharp
public class PoolService : MonoServiceBase
{
    public override void DirectInit()
    {
        base.DirectInit();
        DontDestroyOnLoad(gameObject);  // ← 씬 전환 시에도 유지
    }
}
```

### MonoBehaviour 생명주기 활용

```csharp
public class PoolService : MonoServiceBase
{
    private void Awake()
    {
        // Unity 생명주기 활용 가능
        DirectInit();
    }
    
    private void OnDestroy()
    {
        // 자동 정리
        if (!IsRelease)
        {
            Release();
        }
    }
}
```

## 시나리오 예시

### 게임 시작 시

```csharp
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private ResourceService _resourceService;
    [SerializeField] private PoolService _poolService;
    
    private async void Start()
    {
        // MonoServiceBase는 Inspector에서 할당받음
        await _resourceService.Init();
        await _poolService.Init();
        
        await _resourceService.Prepare();
        await _poolService.Prepare();
        
        StartGame();
    }
}
```

### 씬 전환 시

```csharp
public class SceneManager
{
    private async UniTask LoadScene(string sceneName)
    {
        var resourceService = ResourceService.Instance;
        var poolService = PoolService.Instance;
        
        // 현재 레벨 리소스 해제
        resourceService.ReleaseGroup("CurrentLevel");
        poolService.Clear("level1_cargo");
        
        // 새 레벨 리소스 로드
        await resourceService.LoadPrefabAsync("level2_building", (prefab) => 
        {
            Instantiate(prefab);
        });
        
        // 새 레벨 풀 사전 로드
        await poolService.PreloadAsync("level2_cargo", 30, () => 
        {
            Debug.Log("Level 2 pools ready");
        });
    }
}
```

## 주의사항

1. **MonoServiceBase는 씬에 배치 필수**: GameObject에 컴포넌트로 추가
2. **Inspector 설정 활용**: SerializeField로 에디터 설정 가능
3. **ServiceHome 통합**: FindObjectOfType으로 찾아서 등록
4. **싱글톤 접근**: ResourceService.Instance, PoolService.Instance 사용
5. **UniTask 기반**: 모든 비동기 작업은 UniTask 사용
6. **Unity ObjectPool 기반**: 중복 Release 자동 감지 (collectionCheck: true)
7. **풀링된 오브젝트는 Destroy 금지**: 반드시 `Return()` 사용
8. **PoolObjectMarker 자동 추가**: Pool ID 추적용 컴포넌트
9. **maxSize 초과 시 자동 파괴**: Unity ObjectPool이 초과분 자동 관리
10. **Resources 폴더는 Fallback**: 최종 빌드에서는 ResourceBundle 권장

## ServiceBase vs MonoServiceBase 선택 가이드

### ServiceBase 사용 (순수 로직)
- ✅ Unity 의존성 없는 순수 로직
- ✅ 네트워크, 데이터, 계산 등
- ✅ 예: TableService, LocalizeService

### MonoServiceBase 사용 (Unity 의존)
- ✅ Inspector 설정 필요
- ✅ Unity 씬/GameObject와 상호작용
- ✅ Transform 계층 구조 활용
- ✅ 예: ResourceService, PoolService, CameraService

## 라이선스

MIT License
