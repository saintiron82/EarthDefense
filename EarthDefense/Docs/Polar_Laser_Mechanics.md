# Polar 레이저 무기 동작 원리

**작성일:** 2026-01-11  
**버전:** 1.0

## 개요

레이저는 **지속 빔 타입** 무기로, 홀드 중 계속 데미지를 입히는 특성을 가집니다.

---

## 핵심 특징

### 1. 지속 데미지 (Tick Damage)

```
일반 투사체: 1회 충돌 → 1회 데미지
레이저: 홀드 중 → TickRate Hz로 지속 데미지
```

**계산 공식:**
```csharp
DamagePerTick = TotalDPS / TickRate
```

**예시:**
- TotalDPS = 100
- TickRate = 10Hz (초당 10회)
- **DamagePerTick = 10**
- 1초 홀드 = 10틱 × 10 = **100 데미지**

---

## 라이프사이클

### 1단계: Launch (발사)

```csharp
// PolarLaserWeapon에서 호출
projectile.Launch(field, weaponData, muzzle.position, muzzle.right);

// 초기화
_origin = muzzlePosition;
_direction = muzzleDirection;
_currentLength = 0f;
_nextTickTime = Time.time + (1f / TickRate);
```

### 2단계: Extending (확장)

```
Frame 1: Length = 0.0 → 0.3 (ExtendSpeed)
Frame 2: Length = 0.3 → 0.6
Frame 3: Length = 0.6 → 0.9
...
Frame N: Length = 목표 길이 도달
```

**목표 길이 계산:**
```csharp
float targetLength = Mathf.Min(
    LaserData.MaxLength,
    SectorRadius - OriginDistance
);
```

### 3단계: Holding (유지)

- 목표 길이 유지
- 매 틱마다 데미지 적용
- Muzzle 이동/회전 추적

```csharp
// 매 프레임 갱신
UpdateOriginDirection(muzzle.position, muzzle.right);

// 틱 간격마다 데미지
if (Time.time >= _nextTickTime)
{
    ApplyMultiSectorDamage();
    _nextTickTime = Time.time + (1f / TickRate);
}
```

### 4단계: FlyAway (소멸)

- 입력 해제 시 시작
- Origin이 전진하며 빔이 사라짐
- Length가 0에 가까워지면 풀 반환

```csharp
// Origin 전진
_origin += _direction * RetractSpeed * deltaTime;

// Length 감소
_currentLength -= RetractSpeed * deltaTime;

// 완전 소멸 확인
if (_currentLength <= 0.1f)
{
    ReturnToPool();
}
```

---

## 충돌 판정

### 방식: 중심 섹터 + BeamWidth 범위

```csharp
// 1. 빔 방향으로 중심 섹터 특정
float beamAngle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
int centerSectorIndex = _field.AngleToSectorIndex(beamAngle);

// 2. BeamWidth를 섹터 수로 변환
float beamArcAngle = (BeamWidth / 2f / avgRadius) * Mathf.Rad2Deg;
int damageRadius = Mathf.CeilToInt(beamArcAngle / sectorAngleSize);

// 3. 중심 + 주변 섹터 타격 (균일 데미지)
ApplySectorDamage(centerSectorIndex, damagePerTick);
for (int offset = 1; offset <= damageRadius; offset++)
{
    ApplySectorDamage(centerIndex - offset, damagePerTick);
    ApplySectorDamage(centerIndex + offset, damagePerTick);
}
```

### 재타격 쿨다운

동일 섹터에 대해 **0.05초 쿨다운** 적용:
```csharp
private Dictionary<int, float> _lastHitTimeBySector;

private bool CanHitSector(int sectorIndex)
{
    if (_lastHitTimeBySector.TryGetValue(sectorIndex, out float lastTime))
    {
        if (Time.time - lastTime < 0.05f)
            return false;
    }
    _lastHitTimeBySector[sectorIndex] = Time.time;
    return true;
}
```

**이유:**
- 빠른 틱레이트(60Hz)에서 과도한 데미지 방지
- 섹터 경계에서의 중복 타격 방지

---

## 시각화

### LineRenderer 사용

```csharp
// 빔 라인
lineRenderer.SetPosition(0, _origin);
lineRenderer.SetPosition(1, _origin + _direction * _currentLength);

// 색상/두께
lineRenderer.startColor = LaserData.BeamColor;
lineRenderer.startWidth = LaserData.BeamWidth;
```

### Gizmo (에디터 전용)

- **녹색 라인**: 빔 중심
- **빨간 구체**: 중심 섹터
- **노란 구체**: 타격된 주변 섹터
- **흰색 구체**: 필드 중심

---

## 무기 데이터 구조

### PolarLaserWeaponData

```csharp
[CreateAssetMenu]
public class PolarLaserWeaponData : PolarWeaponData
{
    [Header("Laser Beam")]
    public Color BeamColor = Color.red;
    public float BeamWidth = 0.5f;      // 빔 두께 (월드 유닛)
    public float BeamScale = 1f;
    public float MaxLength = 50f;       // 최대 길이
    
    [Header("Animation")]
    public float ExtendSpeed = 70f;     // 확장 속도
    public float RetractSpeed = 70f;    // 수축 속도
}
```

### 상속 관계

```
PolarWeaponData (기본)
├── Damage (DPS)
├── TickRate (Hz)
├── KnockbackPower
├── AreaType (Fixed)
└── DamageRadius (0)

PolarLaserWeaponData (확장)
└── BeamColor, BeamWidth, MaxLength, ExtendSpeed, RetractSpeed
```

---

## 발사 모드 (Fire Mode)

### Hold 타입

```csharp
// PolarLaserWeapon.cs
public override void OnInputHold()
{
    if (_currentProjectile != null)
    {
        // 이미 발사 중 - Origin/Direction 갱신
        Vector2 origin = Muzzle.position;
        Vector2 direction = Muzzle.right;
        _currentProjectile.UpdateOriginDirection(origin, direction);
    }
    else if (CanFire)
    {
        // 새로 발사
        SpawnProjectile();
    }
}

public override void OnInputRelease()
{
    if (_currentProjectile != null)
    {
        // 소멸 시작
        _currentProjectile.BeginFlyAway();
        _currentProjectile = null;
    }
}
```

---

## 성능 특성

### O(1) 복잡도

```
섹터 수와 무관하게 일정한 연산:
- 중심 섹터 계산: 1회
- 범위 계산: 1회
- 타격 적용: (2 × damageRadius + 1)회 → 보통 5-10회
```

### 메모리 효율

```
재사용 가능한 필드:
- _hitSectorsThisTick (HashSet, 프레임마다 Clear)
- _lastHitTimeBySector (Dictionary, 섹터 수만큼만 증가)
```

---

## 밸런싱 가이드

### DPS 계산

```
실제 DPS = Damage × (타격 섹터 수 / TickRate)
```

**예시:**
- Damage = 100 DPS
- TickRate = 10Hz
- BeamWidth = 0.5 (3 섹터 타격)
- **실제 DPS = 100 × 1 = 100** (섹터당 균등 분배)

### 권장 수치

| 파라미터 | 기본값 | 용도 |
|---------|--------|------|
| Damage | 50-150 | 초당 총 데미지 |
| TickRate | 10-20Hz | 틱 간격 (높을수록 부드러움) |
| BeamWidth | 0.3-0.8 | 빔 두께 (넓을수록 쉬운 조준) |
| MaxLength | 30-50 | 최대 사거리 |
| ExtendSpeed | 50-100 | 빔 확장 속도 |
| KnockbackPower | 0.1-0.3 | 밀어내기 힘 |

### 난이도 조절

**쉬운 레이저:**
- BeamWidth = 0.8 (넓은 빔)
- TickRate = 20Hz (부드러운 추적)
- ExtendSpeed = 100 (빠른 확장)

**어려운 레이저:**
- BeamWidth = 0.3 (좁은 빔)
- TickRate = 10Hz (정확한 조준 필요)
- ExtendSpeed = 50 (느린 확장)

---

## 디버깅 팁

### 로그 활성화

```csharp
[SerializeField] private bool logTickDamage = true;
```

**출력 예시:**
```
[PolarLaserProjectile] ========== TICK #5 START (HOLDING) ==========
  Origin: (0.0, 0.0), Direction: (1.0, 0.0)
  CurrentLength: 4.50
  [BeamDamage] Center sector: 45, Damage radius: 2 sectors
  [BeamDamage] BeamWidth: 0.500, BeamArcAngle: 5.73°
  Hit sectors: 5
========== TICK #5 END ==========
```

### Gizmo 시각화

```csharp
[SerializeField] private bool showDamageGizmos = true;
```

Scene 뷰에서 실시간으로:
- 빔 경로 확인
- 타격 섹터 확인
- 범위 검증

---

## 제약 사항

### 1. 동시 발사 제한

현재 구조는 **무기당 1개 빔**만 지원:
```csharp
private PolarLaserProjectile _currentProjectile;
```

**다중 빔이 필요하면:**
```csharp
private List<PolarLaserProjectile> _activeProjectiles;
```

### 2. 섹터 단위 판정

- 픽셀 단위 정확도는 없음
- 섹터 경계에서 약간의 시각적 불일치 가능
- 게임플레이 체감 차이는 없음

---

## 향후 확장 가능성

### 1. 충전 레이저

```csharp
float chargeTime = 0f;
float maxCharge = 2f;

void Update()
{
    if (isCharging)
    {
        chargeTime = Mathf.Min(chargeTime + deltaTime, maxCharge);
        float chargeMultiplier = chargeTime / maxCharge;
        // damagePerTick *= chargeMultiplier;
    }
}
```

### 2. 분산 빔

```csharp
// 각도 스프레드 추가
float spreadAngle = 15f;
for (int i = 0; i < 3; i++)
{
    float offset = (i - 1) * spreadAngle;
    SpawnBeam(baseAngle + offset);
}
```

### 3. 반사 레이저

```csharp
// 섹터 충돌 시 반사 방향 계산
if (hitSector.IsReflective)
{
    Vector2 normal = GetSectorNormal(hitSector);
    _direction = Vector2.Reflect(_direction, normal);
}
```

---

## 요약

### ✅ 레이저의 핵심

1. **지속 빔** - TickRate로 계속 데미지
2. **중심 + 범위 판정** - O(1) 복잡도
3. **균일 데미지** - 빔 내 모든 섹터 동일
4. **재타격 쿨다운** - 과도한 데미지 방지
5. **홀드 모드** - 입력 유지 중 추적

### 🎯 디자인 철학

- **단순함**: 복잡한 물리 없음
- **성능**: O(1) 복잡도 유지
- **일관성**: 다른 무기와 동일한 패턴
- **직관성**: 보이는 대로 작동

---

**레이저는 Polar 무기 시스템의 기준점입니다. 다른 무기 설계 시 이 원칙을 참고하세요.**

