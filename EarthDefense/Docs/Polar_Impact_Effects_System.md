# 투사체 충돌 효과 시스템

## 데이터 구조 설계

### 현재 구조 (추천 ✅)
```
PolarWeaponData (베이스)
├── 공통: Damage, Knockback, AreaType 등
└── impactEffects[] (충돌 효과들)

PolarMissileWeaponData : PolarWeaponData
├── 미사일 전용: FireRate, Speed, ExplosionRadius 등
└── (부모로부터 상속) impactEffects[]

PolarGravityFieldEffect : ScriptableObject, IPolarProjectileEffect
├── fieldRadius
├── speedMultiplier
└── duration
```

**장점**:
- ✅ 무기 타입별 전용 데이터 분리 (미사일, 레이저, 머신건)
- ✅ 효과는 독립적으로 재사용 가능
- ✅ 상속 구조로 확장성 좋음
- ✅ Inspector에서 직관적

### 대안 1: 컴포지션 구조 (복잡함 ❌)
```
PolarWeaponData
└── weaponConfig (WeaponConfig)
    ├── projectileConfig
    └── effects[]
```
**단점**: 너무 깊은 계층, Inspector 복잡

### 대안 2: 효과를 WeaponData 내장 (유연성 없음 ❌)
```
PolarGravityWeaponData : PolarWeaponData
├── damage
├── fieldRadius (내장)
├── speedMultiplier (내장)
└── duration (내장)
```
**단점**: 
- 미사일에 중력 추가 불가
- 효과 조합 불가
- 재사용 불가

### 추천 구조 상세

#### 1. 베이스 무기 데이터
```csharp
[CreateAssetMenu(...)]
public class PolarWeaponData : ScriptableObject
{
    [Header("Combat")]
    public float damage;
    public float knockbackPower;
    public PolarAreaType areaType;
    
    [Header("Impact Effects (optional)")]
    public ScriptableObject[] impactEffects; // ⭐ 핵심
}
```

#### 2. 무기 타입별 데이터
```csharp
// 미사일 전용
[CreateAssetMenu(menuName = "Polar/Weapon Data/Missile")]
public class PolarMissileWeaponData : PolarWeaponData
{
    [Header("Missile Specific")]
    public float fireRate;
    public float missileSpeed;
    public int explosionRadius;
    
    // impactEffects는 부모에서 상속 ✅
}

// 머신건 전용
[CreateAssetMenu(menuName = "Polar/Weapon Data/Machinegun")]
public class PolarMachinegunWeaponData : PolarWeaponData
{
    [Header("Machinegun Specific")]
    public float fireRate;
    public float spreadAngle;
    public float projectileSpeed;
    
    // impactEffects는 부모에서 상속 ✅
}
```

#### 3. 효과 데이터 (독립적)
```csharp
[CreateAssetMenu(menuName = "Polar/Effects/Gravity Field")]
public class PolarGravityFieldEffect : ScriptableObject, IPolarProjectileEffect
{
    public int fieldRadius;
    public float speedMultiplier;
    public float duration;
    
    public void OnImpact(IPolarField field, int sectorIndex, Vector2 position, PolarWeaponData weaponData)
    {
        // 중력장 생성
    }
}
```

### 사용 예시

#### 예시 1: 일반 미사일
```
PolarMissileWeaponData "NormalMissile"
├── Damage: 50
├── Explosion Radius: 5
└── Impact Effects: [] (비어있음)
```

#### 예시 2: 중력 미사일
```
PolarMissileWeaponData "GravityMissile"
├── Damage: 50
├── Explosion Radius: 5
└── Impact Effects: [1]
    └── GravityFieldEffect
        ├── Field Radius: 10
        ├── Speed Multiplier: 0.2
        └── Duration: 5
```

#### 예시 3: 복합 효과 미사일
```
PolarMissileWeaponData "SuperMissile"
├── Damage: 100
├── Explosion Radius: 8
└── Impact Effects: [3]
    ├── GravityFieldEffect (중력)
    ├── FireEffect (화염)
    └── StunEffect (기절)
```

#### 예시 4: 중력 머신건
```
PolarMachinegunWeaponData "GravityBullet"
├── Damage: 10
├── Fire Rate: 10/s
└── Impact Effects: [1]
    └── GravityFieldEffect (동일한 효과 재사용!)
```

### 효과 재사용 전략

#### 방법 1: 동일 효과 공유 (추천 ✅)
```
GravityFieldEffect (하나의 에셋)
├── 사용처 1: GravityMissile
├── 사용처 2: GravityBullet
└── 사용처 3: GravityLaser
```
**장점**: 한 곳 수정으로 모든 무기 일괄 변경

#### 방법 2: 효과 복사본
```
GravityFieldEffect_Strong
└── Field Radius: 20, Duration: 10

GravityFieldEffect_Weak
└── Field Radius: 5, Duration: 3
```
**장점**: 무기별 독립 밸런스 조정

### WeaponData 계층 구조

```
PolarWeaponData (abstract 또는 일반)
├── impactEffects[] (모든 무기 공통)
│
├── PolarMissileWeaponData
│   ├── fireRate, missileSpeed, explosionRadius
│   └── (상속) impactEffects[]
│
├── PolarMachinegunWeaponData
│   ├── fireRate, spreadAngle, projectileSpeed
│   └── (상속) impactEffects[]
│
├── PolarLaserWeaponData
│   ├── tickRate, beamWidth, reflectCount
│   └── (상속) impactEffects[]
│
└── (미래) PolarRailgunWeaponData
    ├── chargeTime, penetrationCount
    └── (상속) impactEffects[]
```

### 효과 계층 구조

```
IPolarProjectileEffect (interface)
│
├── PolarGravityFieldEffect (중력장)
├── PolarFireEffect (화염)
├── PolarPoisonEffect (독)
├── PolarFreezeEffect (빙결)
├── PolarExplosionChainEffect (연쇄 폭발)
├── PolarTeleportEffect (텔레포트)
└── (미래 확장...)
```

## 개요

투사체에 **충돌 시 발동되는 추가 효과**를 부착할 수 있는 시스템입니다.
중력장, 화염, 독, 빙결 등 다양한 효과를 투사체 타입과 무관하게 추가할 수 있습니다.

## 구조

### 1. 인터페이스
```csharp
public interface IPolarProjectileEffect
{
    void OnImpact(IPolarField field, int sectorIndex, Vector2 position, PolarWeaponData weaponData);
}
```

### 2. 효과 구현체 (ScriptableObject)
```csharp
[CreateAssetMenu(...)]
public class PolarGravityFieldEffect : ScriptableObject, IPolarProjectileEffect
{
    // 설정값
    [SerializeField] private int fieldRadius;
    [SerializeField] private float speedMultiplier;
    [SerializeField] private float duration;
    
    // 충돌 시 발동
    public void OnImpact(IPolarField field, int sectorIndex, Vector2 position, PolarWeaponData weaponData)
    {
        // 중력장 생성 로직
    }
}
```

### 3. WeaponData에 효과 부착
```csharp
public class PolarWeaponData : ScriptableObject
{
    [Header("Impact Effects")]
    [SerializeField] private ScriptableObject[] impactEffects; // IPolarProjectileEffect 구현체들
}
```

### 4. 투사체가 자동 발동
```csharp
// PolarProjectileBase에서 자동 처리
protected void TriggerImpactEffects(int sectorIndex, Vector2 position)
{
    foreach (var effectObj in _weaponData.ImpactEffects)
    {
        if (effectObj is IPolarProjectileEffect effect)
        {
            effect.OnImpact(_field, sectorIndex, position, _weaponData);
        }
    }
}
```

## 사용 방법

### 1. 효과 에셋 생성
```
Project 우클릭
→ Create → EarthDefense → Polar → Effects → Gravity Field
```

### 2. 효과 설정
```
Inspector:
- Field Radius: 10
- Speed Multiplier: 0.2 (80% 둔화)
- Duration: 5초
```

### 3. WeaponData에 효과 추가
```
PolarMissileWeaponData:
- Impact Effects (Size: 1)
  - Element 0: GravityFieldEffect
```

### 4. 완료!
투사체(미사일, 머신건 등)가 벽에 충돌하면 자동으로 중력장 생성됩니다.

## 장점

### 1. 모듈화
- 효과를 독립적으로 구현
- 여러 무기에 재사용 가능
- 효과 조합 가능

### 2. 확장성
```
미사일 + 중력장 효과 = 중력 미사일
머신건 + 화염 효과 = 소이탄
레이저 + 빙결 효과 = 냉동 레이저
```

### 3. 데이터 기반
- 코드 수정 없이 Inspector에서 설정
- 밸런스 조정 용이
- 아티스트/디자이너가 직접 설정 가능

## 새로운 효과 추가 방법

### 1. 효과 클래스 작성
```csharp
[CreateAssetMenu(fileName = "FireEffect", menuName = "EarthDefense/Polar/Effects/Fire")]
public class PolarFireEffect : ScriptableObject, IPolarProjectileEffect
{
    [SerializeField] private float fireDamage = 5f;
    [SerializeField] private float duration = 3f;
    
    public void OnImpact(IPolarField field, int sectorIndex, Vector2 position, PolarWeaponData weaponData)
    {
        // 화염 지대 생성 로직
        Debug.Log($"Fire effect at sector {sectorIndex}!");
    }
}
```

### 2. 효과 에셋 생성 → WeaponData에 추가 → 완료!

## 예시

### 중력 미사일
```
PolarMissileWeaponData:
- Damage: 50
- Explosion Radius: 5
- Impact Effects:
  - GravityFieldEffect (중력장)
```
**결과**: 폭발 데미지 + 중력장으로 적 둔화

### 복합 효과 탄환
```
PolarMachinegunWeaponData:
- Impact Effects:
  - GravityFieldEffect (중력 둔화)
  - PoisonEffect (독 지속 피해)
  - SlowEffect (이동 속도 감소)
```
**결과**: 한 발로 3가지 효과 동시 발동

## 기존 코드 변경 사항

### PolarWeaponData
- ✅ `impactEffects` 필드 추가
- ✅ `ImpactEffects` 프로퍼티 추가

### PolarProjectileBase
- ✅ `TriggerImpactEffects()` 메서드 추가
- ✅ 충돌 감지 시 자동 호출

### 기존 투사체 (미사일, 머신건 등)
- ✅ 변경 없음 - 자동으로 효과 발동됨

## 삭제 가능한 파일

이제 중력장이 효과 시스템으로 통합되었으므로:
- ❌ `PolarGravityProjectile.cs` - 삭제 가능 (더 이상 불필요)
- ❌ `PolarGravityWeaponData.cs` - 삭제 가능
- ✅ 대신 일반 미사일에 `GravityFieldEffect` 추가

