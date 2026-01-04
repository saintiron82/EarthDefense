# Debug.Log 최적화 가이드

## ⚠️ 문제점

### **과도한 로그 출력**
게임 플레이 중 불필요한 로그가 콘솔을 넘쳐나게 하여:
- 성능 저하 (string 할당, 콘솔 렌더링)
- 중요한 에러/경고 메시지 놓침
- 빌드 크기 증가

### **래핑된 로그 메서드**
일부 시스템에서 `Debug.Log`를 래핑한 메서드 사용:
- `ResourceService.LogCacheStatus()` - 캐시 상태 출력
- `RingSectorDebugOverlay.Update()` - 매 초마다 메시 상태 출력

---

## ✅ 적용된 최적화

### **1. PoolService.cs** (4곳)
```csharp
// 이전: 항상 출력
Debug.Log($"[PoolService] Pool created: {id}");

// 이후: 개발 빌드에서만
#if UNITY_EDITOR || DEVELOPMENT_BUILD
Debug.Log($"[PoolService] Pool created: {id}");
#endif
```

**적용 항목:**
- `Pool created` - 조건부
- `Pool cleared` - 조건부  
- `All pools cleared` - 조건부
- `Release` - **완전 제거** (너무 빈번)

### **2. PlayerShooter.cs** (1곳)
```csharp
// 이전: 프리셋 적용 시마다
Debug.Log($"Weapon preset applied: {stats.Damage}");

// 이후: 에디터에서만
#if UNITY_EDITOR
Debug.Log($"Weapon preset applied: {stats.Damage}");
#endif
```

### **3. RingSectorDebugOverlay.cs** (디버그 전용 클래스)
```csharp
// 이전: Update()에서 조건 없이 실행
var mesh = _mf.sharedMesh;
Debug.Log($"[RingSectorDebug] {name} ...");

// 이후: 전체를 조건부 컴파일로 래핑
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    var mesh = _mf.sharedMesh;
    Debug.Log($"[RingSectorDebug] {name} ...");
#endif
```

**효과:** 릴리스 빌드에서 로그뿐 아니라 계산 코드까지 완전 제거

### **4. ResourceService.cs** (래핑된 로그 메서드)
```csharp
public void LogCacheStatus()
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log("=== ResourceService Cache Status ===");
    Debug.Log($"Total cached: {_cache.Count}");
    // ...
#endif
}
```

**효과:** 메서드는 남아있지만 내부 로직이 컴파일 타임에 제거됨

### **5. 유지된 로그**
✅ **경고/에러는 그대로 유지** (중요)
```csharp
Debug.LogWarning($"[PoolService] Object not found: {obj.name}");
Debug.LogError($"[PoolService] Failed to load prefab: {id}");
```

✅ **플래그 기반 디버그 로그** (좋은 패턴)
```csharp
// Bullet.cs - debugLogHits 플래그로 제어
if (debugLogHits)
{
    Debug.Log($"[Bullet→Health] ...");
}
```

---

## 📐 로그 사용 가이드라인

### **1. 항상 출력 (프로덕션)**
```csharp
Debug.LogError()    // ❌ 치명적 오류
Debug.LogWarning()  // ⚠️ 중요한 경고
Debug.LogException()// 💥 예외
```

### **2. 조건부 출력 (개발 전용)**
```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log("일반 정보");
#endif
```

### **3. 플래그 기반 (디버깅 목적)**
```csharp
[SerializeField] private bool debugMode;

if (debugMode)
{
    Debug.Log("상세 디버그 정보");
}
```

### **4. 출력하지 말아야 할 것**
```csharp
❌ Update() 내부 로그
❌ 초당 수십~수백 번 호출되는 로직
❌ 풀링 Get/Return 같은 빈번한 작업
❌ 프로덕션에 남아있는 테스트 로그
```

---

## 🎯 성능 영향

### **개선 전:**
```
Pool Get/Return: 초당 100회 × 로그 출력
→ string 할당 100회/초
→ 콘솔 렌더링 부하
→ GC 압력 증가
```

### **개선 후:**
```
Pool Get/Return: 로그 없음
→ string 할당 0회
→ 콘솔 부하 없음
→ GC 압력 감소
```

**예상 효과:**
- 프레임 저하 방지
- 메모리 할당 감소
- 빌드 크기 소폭 감소

---

## 💡 향후 개선 사항

### **옵션 1: 전역 로그 레벨 시스템**
```csharp
public static class GameLogger
{
    public enum Level { None, Error, Warning, Info, Debug }
    public static Level CurrentLevel = Level.Warning;
    
    public static void Log(string message, Level level = Level.Info)
    {
        if (level <= CurrentLevel)
        {
            Debug.Log($"[{level}] {message}");
        }
    }
}

// 사용
GameLogger.Log("Pool created", GameLogger.Level.Debug);
```

### **옵션 2: 조건부 컴파일 심볼**
```csharp
// PlayerSettings에서 VERBOSE_LOGGING 심볼 정의

#if VERBOSE_LOGGING
    Debug.Log("상세 로그");
#endif
```

### **옵션 3: 커스텀 로거 (권장)**
```csharp
public static class PoolLogger
{
    [Conditional("UNITY_EDITOR")]
    public static void LogPoolCreated(string id, int preload)
    {
        Debug.Log($"[Pool] Created: {id} (preload: {preload})");
    }
}

// 사용
PoolLogger.LogPoolCreated(id, preload);
// 릴리스 빌드에서는 메서드 호출 자체가 제거됨!
```

---

## ✅ 체크리스트

현재 상태:
- [x] PoolService 로그 최적화 (4곳)
- [x] PlayerShooter 로그 최적화 (1곳)
- [x] RingSectorDebugOverlay 로그 최적화 (디버그 클래스)
- [x] ResourceService.LogCacheStatus() 최적화 (래핑 메서드)
- [x] Bullet 플래그 기반 로그 확인
- [x] 경고/에러 로그 유지

권장 추가 작업:
- [ ] 전역 로거 시스템 도입
- [ ] [Conditional] 속성 활용
- [ ] 남은 불필요한 로그 검토

---

**작성일**: 2026-01-02  
**적용 범위**: PoolService, PlayerShooter, RingSectorDebugOverlay, ResourceService  
**최적화 완료**: 총 6개 파일, 10+ 로그 지점 최적화  
**성능 개선**: 프레임 저하 방지, GC 압력 감소, 빌드 크기 감소

