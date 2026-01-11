# Polar 무기 데미지 타입 (AreaType) 완전 가이드

**작성일:** 2026-01-11  
**버전:** 1.0

## 개요

Polar 무기 시스템은 4가지 데미지 타입(AreaType)을 지원하며, 각각 다른 충돌 판정 방식과 데미지 분산 패턴을 가집니다.

---

## 4가지 AreaType

```csharp
public enum PolarAreaType 
{ 
    Fixed,      // 고정 섹터 (레이저)
    Linear,     // 선형 감쇠 (향후 사용)
    Gaussian,   // 가우시안 분포 (머신건)
    Explosion   // 물리적 반경 (미사일)
}
```

---

## 1. Fixed (고정 섹터)

### 특징
- **단일 타격 또는 균일 범위 피해**
- 감쇠 없음 (모든 섹터 동일 데미지)
- 가장 단순한 판정

### 사용 무기
- **레이저 (Laser)** - BeamWidth 기반 범위

### 충돌 판정 방식
```
중심 섹터 특정 → BeamWidth로 범위 계산 → 균일 데미지 적용

┌─────────────────────┐
│   레이저 빔 범위     │
│  ┌───┬───┬───┬───┐  │
│  │100│100│100│100│  │  ← 모두 동일 데미지
│  └───┴───┴───┴───┘  │
└─────────────────────┘
```

### 데미지 분포 예시
```
BeamWidth = 0.5 (3 섹터)
Base Damage = 100

섹터 -1: 100 (균일)
섹터  0: 100 (중심)
섹터 +1: 100 (균일)

총 데미지: 300 (100 × 3)
```

### 계산식
```csharp
// 중심 섹터
int centerSector = AngleToSectorIndex(beamAngle);

// 범위 계산
float beamArcAngle = (beamWidth / avgRadius) * Mathf.Rad2Deg;
int damageRadius = Mathf.CeilToInt(beamArcAngle / sectorAngleSize);

// 모든 섹터에 동일 데미지
for (int offset = -damageRadius; offset <= damageRadius; offset++)
{
    ApplyDamage(centerSector + offset, baseDamage);  // 감쇠 없음
}
```

### 장점
- ✅ 예측 가능한 데미지
- ✅ 간단한 계산
- ✅ 일관된 DPS

### 단점
- ❌ 정밀도 보상 없음
- ❌ 전술적 깊이 부족

---

## 2. Linear (선형 감쇠)

### 특징
- **거리에 비례하여 균일하게 감소**
- 예측 가능한 감쇠 곡선
- 중간 정도의 복잡도

### 사용 무기
- **향후 확장용** (현재 미사용)
- 산탄총, 플레임스로워 등에 적합

### 충돌 판정 방식
```
중심 섹터 특정 → 거리별 선형 감쇠 적용

┌─────────────────────────────┐
│       선형 감쇠 패턴         │
│  ┌───┬───┬───┬───┬───┐     │
│  │25 │50 │100│50 │25 │     │  ← 균일하게 감소
│  └───┴───┴───┴───┴───┘     │
└─────────────────────────────┘
```

### 데미지 분포 예시
```
DamageRadius = 2
Base Damage = 100

섹터 -2: 33  (거리 2: 1 - 2/3 = 0.33)
섹터 -1: 67  (거리 1: 1 - 1/3 = 0.67)
섹터  0: 100 (중심)
섹터 +1: 67  (거리 1)
섹터 +2: 33  (거리 2)

총 데미지: 300 (100 + 67×2 + 33×2)
```

### 계산식
```csharp
// 중심 섹터: 100%
ApplyDamage(centerIndex, baseDamage);

// 주변 섹터: 선형 감쇠
for (int offset = 1; offset <= damageRadius; offset++)
{
    float falloff = 1f - (float)offset / (damageRadius + 1);
    float damage = baseDamage * falloff;
    
    ApplyDamage(centerIndex - offset, damage);
    ApplyDamage(centerIndex + offset, damage);
}
```

### 장점
- ✅ 예측 가능한 감쇠
- ✅ 균등한 분배
- ✅ 계산 단순

### 단점
- ❌ 현실성 부족 (급격한 변화)
- ❌ 게임플레이 깊이 부족

---

## 3. Gaussian (가우시안 분포)

### 특징
- **부드러운 곡선 감쇠**
- 중심 근처는 강하고 멀어질수록 급격히 약해짐
- 현실적인 산탄/파편 효과

### 사용 무기
- **머신건 (Machinegun)** - 현재는 단일 섹터로 변경됨
- 향후: 산탄총, 파편 무기에 적합

### 충돌 판정 방식
```
중심 섹터 특정 → Gaussian 곡선 감쇠 적용

┌─────────────────────────────┐
│     Gaussian 감쇠 패턴       │
│  ┌───┬───┬───┬───┬───┐     │
│  │1  │13 │100│13 │1  │     │  ← 부드러운 곡선
│  └───┴───┴───┴───┴───┘     │
└─────────────────────────────┘
```

### 데미지 분포 예시
```
DamageRadius = 2
Base Damage = 100

섹터 -2: 14  (Gaussian: e^(-4) ≈ 0.14)
섹터 -1: 61  (Gaussian: e^(-1) ≈ 0.61)
섹터  0: 100 (중심)
섹터 +1: 61  (Gaussian)
섹터 +2: 14  (Gaussian)

총 데미지: 250 (100 + 61×2 + 14×2)
```

### 계산식 (룩업 테이블)
```csharp
// 사전 계산된 Gaussian 값
private static readonly float[] GaussianLookup = new float[]
{
    1.0000f,  // offset 0 (중심, 100%)
    0.6065f,  // offset 1 (60.65%)
    0.1353f,  // offset 2 (13.53%)
    0.0111f,  // offset 3 (1.11%)
    0.0003f   // offset 4 (0.03%)
};

// 중심 섹터
ApplyDamage(centerIndex, baseDamage);

// 주변 섹터: Gaussian 감쇠
for (int offset = 1; offset <= damageRadius; offset++)
{
    float falloff = GaussianLookup[offset];
    float damage = baseDamage * falloff;
    
    ApplyDamage(centerIndex - offset, damage);
    ApplyDamage(centerIndex + offset, damage);
}
```

### 수학적 배경
```
Gaussian 함수: f(x) = e^(-x²/2σ²)

여기서:
- x = offset (거리)
- σ = damageRadius / 3 (표준편차)
- 3σ 범위 = 99.7% 커버 (통계학)
```

### 장점
- ✅ 현실적인 산탄/파편 효과
- ✅ 부드러운 감쇠 곡선
- ✅ 중심 타격 보상

### 단점
- ❌ 가장자리 데미지 너무 약함
- ❌ 유효 범위 좁음 (±1-2 섹터)

---

## 4. Explosion (폭발 반경)

### 특징
- **3단계 폭발 시스템**
- 폭심 → 유효 범위 → 외곽으로 구분
- 가장 복잡하지만 현실적

### 사용 무기
- **미사일 (Missile)**

### 충돌 판정 방식
```
중심 섹터 특정 → 3단계 폭발 영역별 감쇠 적용

┌─────────────────────────────────────────────┐
│           3단계 폭발 시스템                  │
│  ┌───┬───┬───┬───┬───┬───┬───┬───┬───┐   │
│  │10 │35 │65 │90 │120│120│90 │65 │35 │10 │
│  └───┴───┴───┴───┴───┴───┴───┴───┴───┘   │
│   외곽   유효 범위   폭심   유효   외곽      │
└─────────────────────────────────────────────┘
```

### 3단계 구조

#### Stage 1: Core (폭심)
```
범위: CoreRadius (±1-3 섹터)
데미지: CoreMultiplier (100-150%)
특징: 최대 데미지, 직격탄 보상
```

#### Stage 2: Effective (유효 범위)
```
범위: EffectiveRadius (±4-7 섹터)
데미지: CoreMultiplier → EffectiveMinMultiplier (80-100%)
특징: 의미있는 데미지, 감쇠 곡선 적용
```

#### Stage 3: Outer (외곽)
```
범위: MaxRadius (±8-15 섹터)
데미지: EffectiveMinMultiplier → MaxMinMultiplier (10-50%)
특징: 여파 효과, 급격한 감쇠
```

### 데미지 분포 예시
```
CoreRadius = 1
EffectiveRadius = 5
MaxRadius = 8
CoreMultiplier = 1.0
EffectiveMinMultiplier = 0.8
MaxMinMultiplier = 0.1
Base Damage = 100

폭심 (±1):
  섹터 -1: 100
  섹터  0: 100
  섹터 +1: 100

유효 범위 (±2~5):
  섹터 -5: 80
  섹터 -4: 85
  섹터 -3: 90
  섹터 -2: 95
  섹터 +2: 95
  섹터 +3: 90
  섹터 +4: 85
  섹터 +5: 80

외곽 (±6~8):
  섹터 -8: 10
  섹터 -7: 30
  섹터 -6: 55
  섹터 +6: 55
  섹터 +7: 30
  섹터 +8: 10

총 데미지: ~1,290
```

### 계산식
```csharp
// 1. 폭심 (Core)
for (int offset = 0; offset <= coreRadius; offset++)
{
    float coreDamage = baseDamage * coreMultiplier;
    ApplyDamage(centerIndex + offset, coreDamage);
}

// 2. 유효 범위 (Effective)
for (int offset = coreRadius + 1; offset <= effectiveRadius; offset++)
{
    float t = (offset - coreRadius) / (effectiveRadius - coreRadius);
    float falloff = CalculateFalloff(
        coreMultiplier, 
        effectiveMinMultiplier, 
        t, 
        falloffType  // Linear, Smooth, Exponential
    );
    float damage = baseDamage * falloff;
    ApplyDamage(centerIndex + offset, damage);
}

// 3. 외곽 (Outer)
for (int offset = effectiveRadius + 1; offset <= maxRadius; offset++)
{
    float t = (offset - effectiveRadius) / (maxRadius - effectiveRadius);
    float falloff = CalculateFalloff(
        effectiveMinMultiplier, 
        maxMinMultiplier, 
        t, 
        falloffType
    );
    float damage = baseDamage * falloff;
    ApplyDamage(centerIndex + offset, damage);
}
```

### 감쇠 곡선 타입
```csharp
public enum ExplosionFalloffType
{
    Linear,      // 선형: Lerp(start, end, t)
    Smooth,      // 부드러운: Lerp(start, end, SmoothStep(t))
    Exponential  // 지수: Lerp(start, end, t²)
}
```

### 장점
- ✅ 매우 현실적인 폭발 표현
- ✅ 명확한 영역 구분
- ✅ 정밀도 보상 (폭심 강화)
- ✅ 유효 범위 넓음 (±4-7 섹터)
- ✅ 밸런싱 제어 용이

### 단점
- ❌ 가장 복잡한 계산
- ❌ 파라미터 많음

---

## 비교표

### 타격 범위

| AreaType | 유효 섹터 수 | 총 데미지 (Damage=100 기준) |
|----------|-------------|---------------------------|
| **Fixed** | 3-5 | 300-500 (균일) |
| **Linear** | 5-7 | 300-400 (선형 감쇠) |
| **Gaussian** | 3-5 (±2) | 250-300 (급격한 감쇠) |
| **Explosion** | 11-21 (±5-10) | 800-1,500 (3단계) |

### 계산 복잡도

| AreaType | 복잡도 | 연산 횟수 (예시) | 성능 |
|----------|--------|----------------|------|
| **Fixed** | O(1) | ~10회 | ⭐⭐⭐⭐⭐ 최고 |
| **Linear** | O(1) | ~15회 | ⭐⭐⭐⭐⭐ 최고 |
| **Gaussian** | O(1) | ~15회 | ⭐⭐⭐⭐ 우수 |
| **Explosion** | O(1) | ~40회 | ⭐⭐⭐ 양호 |

### 게임플레이 특성

| AreaType | 정밀도 보상 | 범위 커버 | 전술적 깊이 | 직관성 |
|----------|-----------|----------|-----------|--------|
| **Fixed** | ❌ 없음 | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Linear** | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Gaussian** | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ |
| **Explosion** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |

---

## 무기별 추천 AreaType

### 빔/레이저 무기 → Fixed
```
이유:
- 빔은 본질적으로 균일한 강도
- 범위 = BeamWidth (물리적 크기)
- 감쇠 불필요
```

### 단발 투사체 → Linear
```
이유:
- 예측 가능한 피해 범위
- 간단한 밸런싱
- 중간 정도의 범위 커버
```

### 산탄/파편 무기 → Gaussian
```
이유:
- 현실적인 산탄 분산
- 중심 타격 보상
- 부드러운 감쇠 곡선
```

### 폭발 무기 → Explosion
```
이유:
- 폭발의 물리적 특성 반영
- 폭심/유효/외곽 구분 명확
- 넓은 범위 커버
- 정밀도 강력 보상
```

---

## 실전 사용 예시

### 시나리오 1: 약한 적 다수

**최적:** Fixed (레이저) 또는 Explosion (미사일)
```
레이저: 넓은 빔으로 여러 섹터 동시 커버
미사일: 광역 폭발로 전체 제압
```

**비추:** Linear, Gaussian
```
감쇠로 인해 여러 적 처치에 비효율적
```

### 시나리오 2: 중형 적 단일

**최적:** Linear 또는 Gaussian
```
집중 화력으로 빠른 처치
감쇠가 큰 의미 없음 (단일 타겟)
```

**비추:** Explosion
```
과도한 범위 (낭비)
```

### 시나리오 3: 강한 적 단일 (보스)

**최적:** Explosion (폭심 강화)
```
CoreMultiplier = 1.5 설정
정확한 조준으로 150% 데미지
```

**추천:** Fixed (레이저)
```
지속 데미지로 안정적 DPS
```

---

## 밸런싱 가이드

### DPS 계산

#### Fixed
```
DPS = Damage × AttackRate × SectorCount
예: 100 × 10 × 3 = 3,000 DPS
```

#### Linear
```
총 데미지 = Damage × (1 + Σ falloff)
DPS = 총 데미지 × AttackRate
예: 300 × 10 = 3,000 DPS
```

#### Gaussian
```
총 데미지 = Damage × (1 + Σ gaussian)
DPS = 총 데미지 × AttackRate
예: 250 × 10 = 2,500 DPS
```

#### Explosion
```
총 데미지 = Damage × (CoreDamage + EffectiveDamage + OuterDamage)
DPS = 총 데미지 × FireRate
예: 1,290 × 0.5 = 645 DPS
```

### 밸런스 조정

**너무 강하면:**
- Damage 감소
- 범위(Radius) 감소
- 감쇠율 증가

**너무 약하면:**
- Damage 증가
- 범위(Radius) 증가
- 감쇠율 감소 또는 Multiplier 증가

---

## 요약

### AreaType 선택 기준

1. **Fixed** - 균일 데미지, 레이저/빔
2. **Linear** - 예측 가능, 단순 투사체
3. **Gaussian** - 현실적, 산탄/파편
4. **Explosion** - 복잡하지만 강력, 폭발 무기

### 핵심 차이점

```
Fixed:     ████████  (균일)
Linear:    ██████▓▓  (선형 감소)
Gaussian:  ████▓░░░  (급격한 감소)
Explosion: ██████████████  (3단계, 넓음)
```

### 성능 vs 현실성

```
성능 우선: Fixed > Linear > Gaussian > Explosion
현실성:    Explosion > Gaussian > Linear > Fixed
밸런싱:    Explosion > Linear > Gaussian > Fixed
```

---

**각 무기의 특성에 맞는 AreaType을 선택하면 게임플레이 깊이와 밸런스가 크게 향상됩니다!**

