# Weapon System 확장 계획

## 📋 현재 상태 (2026-01-02)

### 구현됨:
- ✅ **SectorSpawner**: 원형 섹터 방식 적 스폰 시스템
- ✅ **Bullet**: 기본 투사체 (데미지, 관통, 재히트 쿨다운)
- ✅ **WeaponStats**: 무기 스탯 시스템 (5가지 프리셋)
- ✅ **공간 분할 구조**: Spatial Grid로 충돌 검사 최적화
- ✅ **풀링 시스템**: PoolService 통합

### 최적화 완료:
- ✅ 충돌 검사: 전역 순회 → 공간 쿼리 (10~20배 향상)
- ✅ 중복 연산 제거: 각도 계산, 컴포넌트 조회 캐싱
- ✅ Prune 최적화: 30프레임당 1회

---

## 🚀 향후 Weapon 시스템 확장 계획

### Phase 1: 인터페이스 추상화
```
현재: SectorSpawner (적 스폰 전용)
      ↓
향후: IWeapon 인터페이스
      ├─ SectorWeapon (원형 섹터)
      ├─ LinearWeapon (직선 발사)
      ├─ SpiralWeapon (나선형)
      └─ HomingWeapon (유도 미사일)
```

### Phase 2: 투사체 타입 확장
```
현재: Bullet (단순 발사체)
      ↓
향후: IProjectile 인터페이스
      ├─ Bullet (기본 총알)
      ├─ Missile (유도 미사일)
      ├─ Laser (레이저 빔)
      ├─ Drone (자율 드론)
      └─ Shield (방어막)
```

### Phase 3: 무기 데이터 ScriptableObject화
```csharp
// 현재: WeaponStats (Serializable class)
[CreateAssetMenu(menuName = "Weapon/New Weapon")]
public class WeaponData : ScriptableObject
{
    public WeaponType Type;
    public WeaponConfig Config;
    public ProjectileConfig ProjectileConfig;
    public SpecialEffect[] Effects;
}
```

### Phase 4: 특수 효과 시스템
- **슬로우**: 피격 시 이동 속도 감소
- **빙결**: 일정 시간 동결
- **독**: 지속 데미지
- **폭발**: 범위 데미지
- **전기**: 체인 라이트닝
- **관통**: 다수 적 동시 타격

### Phase 5: 업그레이드 시스템
```csharp
public interface IUpgradeable
{
    int CurrentLevel { get; }
    int MaxLevel { get; }
    void Upgrade();
    UpgradeInfo GetNextUpgradeInfo();
}

// 예: 
// Lv1: 데미지 5, 연사 8발/초
// Lv2: 데미지 7, 연사 10발/초
// Lv3: 데미지 10, 연사 12발/초 + 관통 2회
```

---

## 📐 아키텍처 설계

### 1. 무기 시스템 계층
```
PlayerController
    ↓
WeaponManager (무기 슬롯 관리)
    ↓
IWeapon[] (장착된 무기들)
    ↓
IProjectile[] (발사된 투사체들)
```

### 2. 데이터 흐름
```
WeaponData (SO)
    ↓ Load
WeaponConfig
    ↓ Initialize
IWeapon
    ↓ Fire
IProjectile
    ↓ Hit
DamageSystem
```

### 3. 이벤트 시스템
```csharp
// 무기 이벤트
public event Action<IWeapon> OnWeaponFired;
public event Action<IWeapon, int> OnWeaponUpgraded;
public event Action<IWeapon> OnWeaponChanged;

// 투사체 이벤트
public event Action<IProjectile, IDamageable> OnProjectileHit;
public event Action<IProjectile> OnProjectileExpired;
```

---

## 🔧 마이그레이션 계획

### Step 1: SectorSpawner → SectorWeapon
```csharp
// 현재
public sealed class SectorSpawner : MonoBehaviour { }

// 향후
public sealed class SectorWeapon : MonoBehaviour, IWeapon
{
    private WeaponConfig _config;
    
    public void Initialize(WeaponConfig config)
    {
        _config = config;
        // 기존 Setup() 로직 이동
    }
    
    public void Fire(Vector3 direction)
    {
        // 기존 Spawn() 로직 활용
    }
}
```

### Step 2: ChunkEnemy → IProjectile
```csharp
// ChunkEnemy를 IProjectile로 확장
public sealed class ChunkEnemy : MonoBehaviour, IPoolable, IProjectile
{
    public void Initialize(ProjectileConfig config) { }
    public void Launch(Vector3 position, Vector3 direction, float speed) { }
}
```

### Step 3: Bullet → 다양한 투사체
```csharp
public class Missile : MonoBehaviour, IProjectile
{
    private Transform _target;
    
    public void SetTarget(Transform target)
    {
        _target = target;
    }
    
    private void Update()
    {
        // 유도 로직
        if (_target != null)
        {
            Vector3 direction = (_target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
        }
    }
}
```

---

## 💡 확장 예시

### 예시 1: 레이저 무기
```csharp
public class LaserWeapon : MonoBehaviour, IWeapon
{
    private LineRenderer _beam;
    
    public void Fire(Vector3 direction)
    {
        // 레이캐스트로 즉시 히트 검사
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, range);
        
        if (hit.collider != null)
        {
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            target?.TakeDamage(new DamageEvent(damage, hit.point, direction, gameObject));
        }
        
        // 비주얼 라인 그리기
        DrawLaserBeam(transform.position, hit.point);
    }
}
```

### 예시 2: 드론 소환
```csharp
public class DroneWeapon : MonoBehaviour, IWeapon
{
    private List<Drone> _activeDrones = new();
    
    public void Fire(Vector3 direction)
    {
        if (_activeDrones.Count >= maxDrones) return;
        
        var drone = PoolService.Instance.Get<Drone>(dronePoolId);
        drone.Initialize(new ProjectileConfig { Speed = 5f, Damage = 2f });
        drone.SetOrbitTarget(transform);
        
        _activeDrones.Add(drone);
    }
}
```

---

## 📝 참고 사항

### 호환성 유지
- 기존 `SectorSpawner`는 **적 스폰용**으로 그대로 유지
- 새로운 무기 시스템은 **플레이어용** 별도 구현
- 점진적 마이그레이션 (코드 재사용 우선)

### 성능 고려
- ✅ 공간 분할 구조 활용 (이미 구현됨)
- ✅ 풀링 시스템 활용 (이미 구현됨)
- ⚠️ 많은 투사체 발사 시 Burst Compiler + Job System 고려

### 디자인 패턴
- **Strategy Pattern**: 무기별 발사 패턴
- **Factory Pattern**: 투사체 생성
- **Observer Pattern**: 무기/투사체 이벤트
- **Object Pool**: 투사체 재사용 (이미 구현됨)

---

## ✅ 체크리스트

준비 완료:
- [x] `IWeapon` 인터페이스 정의
- [x] `IProjectile` 인터페이스 정의
- [x] `WeaponConfig` / `ProjectileConfig` 데이터 구조
- [x] 기존 코드에 확장 주석 추가
- [x] 풀링 시스템 통합
- [x] 공간 분할 최적화

다음 단계:
- [ ] `WeaponData` ScriptableObject 구현
- [ ] `WeaponManager` 구현
- [ ] 첫 번째 대체 무기 구현 (LinearWeapon or LaserWeapon)
- [ ] 업그레이드 시스템 구현
- [ ] 특수 효과 시스템 구현

---

**문서 작성일**: 2026-01-02  
**마지막 업데이트**: Phase 3 최적화 완료 (공간 분할 + 풀링)

