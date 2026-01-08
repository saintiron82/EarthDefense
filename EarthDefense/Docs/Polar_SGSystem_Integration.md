# Polar 무기 시스템 SGSystem 통합 가이드

## 📊 현재 문제점

### 1. IPoolable 미구현
- `PolarProjectileBase`가 `IPoolable` 인터페이스를 구현하지 않음
- `PoolBundleId` 프로퍼티 없음
- `OnSpawnFromPool/OnReturnToPool` 생명주기 메서드 없음

### 2. 불완전한 풀링
- `PoolService.Get()`은 사용하지만 `Return()` 호출 없음
- 투사체 소멸 시 `Deactivate()`만 호출하고 풀 반환 안 함
- 메모리 누수 가능성

### 3. 수동 Instantiate 혼재
- `usePool` 플래그로 풀링/인스턴스 분기
- 일관성 없는 생명주기 관리

---

## ✅ 해결 방안

### Phase 1: PolarProjectileBase IPoolable 구현

```csharp
using Script.SystemCore.Pool;

public abstract class PolarProjectileBase : MonoBehaviour, IPoolable
{
    protected IPolarField _field;
    protected PolarWeaponData _weaponData;
    protected bool _isActive;

    // ✅ IPoolable 구현
    public string PoolBundleId { get; set; }

    public void OnSpawnFromPool()
    {
        // PoolService.Get() 시 자동 호출
        _isActive = false;  // 초기 상태
        OnPoolSpawn();
    }

    public void OnReturnToPool()
    {
        // PoolService.Return() 시 자동 호출
        _isActive = false;
        _field = null;
        _weaponData = null;
        OnPoolReturn();
    }

    // 하위 클래스에서 추가 초기화/정리 로직
    protected virtual void OnPoolSpawn() { }
    protected virtual void OnPoolReturn() { }

    // 기존 메서드...
    public abstract void Launch(IPolarField field, PolarWeaponData weaponData);
    public virtual void Deactivate() { _isActive = false; }
    protected abstract void OnUpdate(float deltaTime);
}
```

### Phase 2: 투사체 ReturnToPool 구현

```csharp
// PolarLaserProjectile
protected override void OnUpdate(float deltaTime)
{
    UpdateBeamLength(deltaTime);
    UpdateBeamVisual();
    ApplyTickDamageIfNeeded();

    // 리트랙트 완료 시 풀 반환
    if (_isRetracting && _currentLength <= 0f)
    {
        ReturnToPool();  // ✅ 추가
    }
}

public override void ReturnToPool()
{
    base.ReturnToPool();
    
    // ✅ PoolService에 반환
    if (PoolService.Instance != null && !string.IsNullOrEmpty(PoolBundleId))
    {
        PoolService.Instance.Return(this);
    }
    else
    {
        Destroy(gameObject);  // Fallback
    }
}
```

```csharp
// PolarMachinegunProjectile
protected override void OnUpdate(float deltaTime)
{
    radius += speed * deltaTime;
    UpdatePosition();
    
    if (CheckCollision())
    {
        OnCollision();
        ReturnToPool();  // ✅ Deactivate() → ReturnToPool()
        return;
    }
    
    if (radius > _field.InitialRadius * 2f)
    {
        ReturnToPool();  // ✅ 추가
        return;
    }
}
```

```csharp
// PolarMissileProjectile
protected override void OnUpdate(float deltaTime)
{
    radius += speed * deltaTime;
    UpdatePosition();
    
    if (CheckCollision())
    {
        SpawnExplosionVFX();
        OnCollision();
        ReturnToPool();  // ✅ Deactivate() → ReturnToPool()
        return;
    }
    
    if (radius > _field.InitialRadius * 2f)
    {
        ReturnToPool();  // ✅ 추가
        return;
    }
}
```

### Phase 3: 무기 클래스 단순화

```csharp
// PolarLaserWeapon
public override void Fire()
{
    if (!CanFire || _field == null || LaserData == null) return;

    if (_activeBeam != null)
    {
        _activeBeam.BeginRetract();
        _activeBeam = null;
    }

    SpawnBeam();
    SetCooldown(1f / LaserData.TickRate);
}

private void SpawnBeam()
{
    Vector2 origin = transform.position;
    Vector2 direction = transform.right;

    // ✅ 풀링 전용 (usePool 플래그 제거)
    PolarLaserProjectile beam = null;
    
    if (PoolService.Instance != null && !string.IsNullOrEmpty(LaserData.BeamBundleId))
    {
        beam = PoolService.Instance.Get<PolarLaserProjectile>(
            LaserData.BeamBundleId, 
            origin, 
            Quaternion.identity
        );
    }
    else
    {
        Debug.LogError("[PolarLaserWeapon] PoolService not available!");
        return;
    }

    if (beam != null)
    {
        beam.Launch(_field, LaserData, origin, direction);
        _activeBeam = beam;
    }
}
```

```csharp
// PolarMachinegunWeapon
private void SpawnProjectile(float? targetAngle = null)
{
    Vector3 origin = GetFieldCenter();
    float angle = targetAngle ?? Random.Range(0f, 360f);
    float spread = Random.Range(-MachinegunData.SpreadAngle, MachinegunData.SpreadAngle);
    angle += spread;

    // ✅ 풀링 전용
    if (PoolService.Instance == null || string.IsNullOrEmpty(MachinegunData.ProjectileBundleId))
    {
        Debug.LogError("[PolarMachinegunWeapon] PoolService not available!");
        return;
    }

    var projectile = PoolService.Instance.Get<PolarMachinegunProjectile>(
        MachinegunData.ProjectileBundleId,
        origin,
        Quaternion.identity
    );

    if (projectile != null)
    {
        float startRadius = 0.8f;
        projectile.Launch(_field, MachinegunData, angle, startRadius);
    }
}
```

```csharp
// PolarMissileWeapon
private void SpawnMissile(float? targetAngle = null)
{
    Vector3 origin = GetFieldCenter();
    float angle = targetAngle ?? Random.Range(0f, 360f);

    // ✅ 풀링 전용
    if (PoolService.Instance == null || string.IsNullOrEmpty(MissileData.ProjectileBundleId))
    {
        Debug.LogError("[PolarMissileWeapon] PoolService not available!");
        return;
    }

    var missile = PoolService.Instance.Get<PolarMissileProjectile>(
        MissileData.ProjectileBundleId,
        origin,
        Quaternion.identity
    );

    if (missile != null)
    {
        float startRadius = 0.8f;
        missile.Launch(_field, MissileData, angle, startRadius);
    }
}
```

### Phase 4: PlayerWeaponManager 단순화

```csharp
// usePool 플래그 제거
public sealed class PlayerWeaponManager : MonoBehaviour
{
    [Header("Field")]
    [SerializeField] private MonoBehaviour polarFieldBehaviour;

    [Header("Weapon")]
    [SerializeField] private Transform weaponSlot;
    [SerializeField] private PolarWeaponData defaultWeaponData;
    [SerializeField] private PolarWeaponDataTable dataTable;
    [SerializeField] private string defaultWeaponId;
    // ❌ usePool 제거

    // Initialize 시그니처 단순화
    public void Initialize(IPolarField field, PolarWeaponData weaponData = null, string weaponId = null)
    {
        _field = field;
        // ❌ usePool 파라미터 제거
        _currentWeaponData = ResolveWeaponData(weaponData, weaponId ?? defaultWeaponId);
        _currentWeaponId = _currentWeaponData != null ? _currentWeaponData.Id : weaponId ?? defaultWeaponId;
        LoadWeapon(_currentWeaponData);
    }

    private void LoadWeapon(PolarWeaponData data)
    {
        // ...무기 생성 로직...
        
        if (_currentWeapon != null)
        {
            // ❌ usePool 제거
            _currentWeapon.Initialize(_field, data);
        }
    }
}
```

---

## 📈 통합 후 데이터 흐름

### 완전한 SGSystem 통합 흐름

```
[발사]
PolarLaserWeapon.Fire()
    ↓
PoolService.Get<PolarLaserProjectile>("Projectiles/LaserBeam")
    ↓ (ObjectPool 내부)
    - 풀에 없으면 OnCreate() → Instantiate(prefab)
    - IPoolable.PoolBundleId = "Projectiles/LaserBeam" (자동 주입)
    - OnGet() → SetActive(true)
    - IPoolable.OnSpawnFromPool() 호출
    ↓
beam.Launch(_field, LaserData)
    ↓
[업데이트]
OnUpdate() 매 프레임 실행
    ↓
[소멸]
beam.ReturnToPool()
    ↓
PoolService.Return(beam)
    ↓ (ObjectPool 내부)
    - IPoolable.OnReturnToPool() 호출
    - OnRelease() → SetActive(false)
    - 풀에 보관 (재사용 대기)
```

### 생명주기 이벤트

```
[PoolService 생성 시]
1. OnCreate(bundleId, prefab)
   - Instantiate(prefab)
   - IPoolable.PoolBundleId = bundleId
   
[PoolService.Get() 시]
2. OnGet(component)
   - SetActive(true)
   - IPoolable.OnSpawnFromPool()
   
[PoolService.Return() 시]
3. OnRelease(component)
   - IPoolable.OnReturnToPool()
   - SetActive(false)
   
[풀 정리 시]
4. OnDestroyPoolObject(component)
   - Destroy(gameObject)
```

---

## 🎯 장점

### 1. 완전한 자동화
- ✅ IPoolable 구현으로 자동 생명주기 관리
- ✅ PoolBundleId 자동 주입
- ✅ OnSpawnFromPool/OnReturnToPool 자동 호출

### 2. 메모리 효율
- ✅ 객체 재사용으로 GC 부하 감소
- ✅ Unity ObjectPool 기반 최적화
- ✅ 자동 풀 크기 관리

### 3. 일관성
- ✅ 모든 투사체가 동일한 생명주기
- ✅ usePool 플래그 제거로 코드 단순화
- ✅ SGSystem 표준 준수

### 4. 디버깅
- ✅ CountActive/CountInactive로 상태 모니터링
- ✅ Collection Check로 중복 Release 감지
- ✅ 풀 컨테이너로 Hierarchy 정리

---

## 📊 성능 비교

| 항목 | 기존 (Instantiate) | SGSystem 풀링 |
|------|-------------------|---------------|
| 생성 시간 | ~0.5ms | ~0.05ms (재사용) |
| 메모리 할당 | 매번 새로 할당 | 초기 1회만 |
| GC 부하 | 높음 (파괴 시마다) | 낮음 (재사용) |
| Hierarchy 정리 | 없음 (분산) | 있음 (풀 컨테이너) |
| 중복 Release 감지 | 없음 | 있음 (ObjectPool) |

---

## 🚀 적용 순서

### 1단계: PolarProjectileBase IPoolable 구현
- `IPoolable` 인터페이스 상속
- `PoolBundleId` 프로퍼티 추가
- `OnSpawnFromPool/OnReturnToPool` 구현

### 2단계: 투사체별 ReturnToPool 구현
- `PolarLaserProjectile.ReturnToPool()`
- `PolarMachinegunProjectile.ReturnToPool()`
- `PolarMissileProjectile.ReturnToPool()`

### 3단계: 무기 클래스 단순화
- `usePool` 플래그 제거
- `PoolService.Get()` 전용 사용
- Fallback 로직 제거

### 4단계: PlayerWeaponManager 단순화
- `usePool` 파라미터 제거
- `Initialize()` 시그니처 정리

### 5단계: 빌드 및 테스트
- PoolService 정상 동작 확인
- CountActive/CountInactive 모니터링
- 메모리 프로파일링

---

## 참고 문서
- SGSystem 풀링: `Assets/SGSystem/README.md`
- PoolService API: `Assets/SGSystem/Resource/Pool/PoolService.cs`
- IPoolable 인터페이스: `Assets/SGSystem/Resource/Pool/IPoolable.cs`
