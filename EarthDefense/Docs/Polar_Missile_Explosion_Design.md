# 미사일 폭발 메커니즘 재설계

**작성일:** 2026-01-11  
**버전:** 2.0

## 문제 분석

### 현재 구조의 문제점

```csharp
// 현재: 단순 Gaussian 감쇠
falloff = GaussianLookup[offset];  // 0: 100%, 1: 60%, 2: 13%...
damage = baseDamage * falloff;
```

**문제:**
1. **폭심 개념 부재** - 중심과 1칸 차이가 40%나 감소
2. **유의미한 영역 불명확** - 어디까지가 "폭발 범위"인지 애매
3. **급격한 감쇠** - 2칸만 벗어나도 13%로 급감
4. **밸런싱 어려움** - 실제 영향 범위 예측 불가

---

## 새로운 설계: 2단계 폭발 시스템

### 개념

```
폭발을 2개 영역으로 분리:

┌─────────────────────────────────┐
│    유효 피해 영역 (Effective)    │  ← 실제 데미지가 의미있는 범위
│  ┌─────────────────────────┐   │
│  │  폭심 (Core/Epicenter)   │   │  ← 최대 데미지 영역
│  │       100% 데미지        │   │
│  └─────────────────────────┘   │
│      80-100% 데미지            │
└─────────────────────────────────┘
      외곽: 점진적 감쇠
```

---

## 상세 설계

### 1. 데이터 구조

```csharp
[CreateAssetMenu(...)]
public class PolarMissileWeaponData : PolarWeaponData
{
    [Header("Explosion Mechanics")]
    [Tooltip("폭심 반경 (섹터 수) - 이 범위는 풀 데미지")]
    [SerializeField, Range(0, 5)] private int coreRadius = 1;
    
    [Tooltip("유효 피해 반경 (섹터 수) - 이 범위까지는 의미있는 데미지")]
    [SerializeField, Range(3, 10)] private int effectiveRadius = 5;
    
    [Tooltip("최대 영향 반경 (섹터 수) - 이 범위까지 감쇠 적용")]
    [SerializeField, Range(5, 15)] private int maxRadius = 8;
    
    [Tooltip("폭심 데미지 배율 (1.0 = 기본 데미지)")]
    [SerializeField, Range(0.8f, 1.5f)] private float coreMultiplier = 1.0f;
    
    [Tooltip("유효 범위 최소 데미지 배율")]
    [SerializeField, Range(0.5f, 1.0f)] private float effectiveMinMultiplier = 0.8f;
    
    [Tooltip("최대 범위 최소 데미지 배율")]
    [SerializeField, Range(0.0f, 0.5f)] private float maxMinMultiplier = 0.1f;
    
    [Tooltip("감쇠 곡선 타입")]
    [SerializeField] private ExplosionFalloffType falloffType = ExplosionFalloffType.Smooth;
    
    public int CoreRadius => coreRadius;
    public int EffectiveRadius => effectiveRadius;
    public int MaxRadius => maxRadius;
    public float CoreMultiplier => coreMultiplier;
    public float EffectiveMinMultiplier => effectiveMinMultiplier;
    public float MaxMinMultiplier => maxMinMultiplier;
    public ExplosionFalloffType FalloffType => falloffType;
}

public enum ExplosionFalloffType
{
    Linear,      // 선형 감쇠
    Smooth,      // 부드러운 곡선 (SmoothStep)
    Exponential  // 지수 감쇠 (급격)
}
```

---

### 2. 계산 로직

```csharp
private void ApplyExplosionDamage(int centerIndex, PolarCombatProperties props)
{
    var missileData = _weaponData as PolarMissileWeaponData;
    if (missileData == null) return;
    
    float baseDamage = props.Damage;
    int coreRadius = missileData.CoreRadius;
    int effectiveRadius = missileData.EffectiveRadius;
    int maxRadius = missileData.MaxRadius;
    
    // 1. 폭심 (Core) - 풀 데미지 영역
    for (int offset = 0; offset <= coreRadius; offset++)
    {
        if (offset == 0)
        {
            // 중심
            float coreDamage = baseDamage * missileData.CoreMultiplier;
            _field.ApplyDamageToSector(centerIndex, coreDamage);
        }
        else
        {
            // 폭심 범위 내
            float coreDamage = baseDamage * missileData.CoreMultiplier;
            int leftIndex = (centerIndex - offset + _field.SectorCount) % _field.SectorCount;
            int rightIndex = (centerIndex + offset) % _field.SectorCount;
            
            _field.ApplyDamageToSector(leftIndex, coreDamage);
            _field.ApplyDamageToSector(rightIndex, coreDamage);
        }
    }
    
    // 2. 유효 범위 (Effective) - 의미있는 데미지 영역
    for (int offset = coreRadius + 1; offset <= effectiveRadius; offset++)
    {
        float t = (float)(offset - coreRadius) / (effectiveRadius - coreRadius);
        float falloff = CalculateFalloff(
            missileData.CoreMultiplier, 
            missileData.EffectiveMinMultiplier, 
            t, 
            missileData.FalloffType
        );
        float damage = baseDamage * falloff;
        
        int leftIndex = (centerIndex - offset + _field.SectorCount) % _field.SectorCount;
        int rightIndex = (centerIndex + offset) % _field.SectorCount;
        
        _field.ApplyDamageToSector(leftIndex, damage);
        _field.ApplyDamageToSector(rightIndex, damage);
    }
    
    // 3. 외곽 범위 (Outer) - 감쇠 영역
    for (int offset = effectiveRadius + 1; offset <= maxRadius; offset++)
    {
        float t = (float)(offset - effectiveRadius) / (maxRadius - effectiveRadius);
        float falloff = CalculateFalloff(
            missileData.EffectiveMinMultiplier, 
            missileData.MaxMinMultiplier, 
            t, 
            missileData.FalloffType
        );
        float damage = baseDamage * falloff;
        
        int leftIndex = (centerIndex - offset + _field.SectorCount) % _field.SectorCount;
        int rightIndex = (centerIndex + offset) % _field.SectorCount;
        
        _field.ApplyDamageToSector(leftIndex, damage);
        _field.ApplyDamageToSector(rightIndex, damage);
    }
    
    // 상처 시스템 (중심만)
    if (_field.EnableWoundSystem)
    {
        _field.ApplyWound(centerIndex, props.WoundIntensity);
    }
}

private float CalculateFalloff(float start, float end, float t, ExplosionFalloffType type)
{
    switch (type)
    {
        case ExplosionFalloffType.Linear:
            return Mathf.Lerp(start, end, t);
            
        case ExplosionFalloffType.Smooth:
            return Mathf.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            
        case ExplosionFalloffType.Exponential:
            return Mathf.Lerp(start, end, t * t);
            
        default:
            return Mathf.Lerp(start, end, t);
    }
}
```

---

## 프리셋 예시

### 1. 집중 폭발 (Focused Blast)
```
baseDamage: 100
coreRadius: 2          // 중심 ±2 섹터
effectiveRadius: 4     // 유효 범위 ±4 섹터
maxRadius: 6           // 최대 범위 ±6 섹터

coreMultiplier: 1.2        // 폭심 120% 데미지
effectiveMinMultiplier: 0.8  // 유효 범위 최소 80%
maxMinMultiplier: 0.2       // 외곽 최소 20%

결과:
섹터 -2~+2: 120 데미지 (폭심)
섹터 -3~+3: 100-80 데미지 (유효)
섹터 -4~+4: 80-20 데미지 (외곽)
```

**시각화:**
```
섹터: -6  -5  -4  -3  -2  -1   0  +1  +2  +3  +4  +5  +6
데미지: 20  30  50  80 120 120 120 120 120  80  50  30  20
        ▁   ▂   ▃   ▅   █   █   █   █   █   ▅   ▃   ▂   ▁
        
        ← 외곽 →← 유효 →← 폭심 →← 유효 →← 외곽 →
```

---

### 2. 광역 폭발 (Wide Blast)
```
baseDamage: 80
coreRadius: 1          // 좁은 폭심
effectiveRadius: 6     // 넓은 유효 범위
maxRadius: 10          // 매우 넓은 최대 범위

coreMultiplier: 1.0        // 폭심 100%
effectiveMinMultiplier: 0.6  // 유효 범위 최소 60%
maxMinMultiplier: 0.1       // 외곽 최소 10%

결과:
넓은 범위에 고른 데미지
```

---

### 3. 폭심 강화 (Core Enhanced)
```
baseDamage: 120
coreRadius: 3          // 넓은 폭심
effectiveRadius: 5     // 짧은 유효 범위
maxRadius: 7           // 짧은 최대 범위

coreMultiplier: 1.5        // 폭심 150% 데미지
effectiveMinMultiplier: 0.5  // 유효 범위 급감
maxMinMultiplier: 0.0       // 외곽 거의 없음

결과:
폭심은 매우 강하고, 벗어나면 급격히 약해짐
정확도 중시
```

---

## 밸런싱 가이드

### 총 데미지 계산

```
총 데미지 = 중심 + (폭심 범위 데미지) + (유효 범위 데미지) + (외곽 범위 데미지)

예시 (집중 폭발):
중심:    120
폭심:    120 × 4 = 480  (±2 섹터)
유효:    90 × 4 = 360   (±2 섹터, 평균 90)
외곽:    35 × 4 = 140   (±2 섹터, 평균 35)

총합:    120 + 480 + 360 + 140 = 1,100 데미지
```

### DPS 계산
```
DPS = 총 데미지 × FireRate

집중 폭발: 1,100 × 0.5 = 550 DPS (광역)
머신건:    30 × 10 = 300 DPS (단일)
레이저:    100 × 1 = 100 DPS (지속)

→ 미사일은 광역 DPS 최강이지만 느린 발사
```

---

## 게임플레이 효과

### 1. 명확한 역할
```
폭심 안: "직격탄! 최대 데미지"
유효 범위: "충분히 아픔"
외곽: "영향은 있지만 약함"
```

### 2. 정확도 보상
```
정확히 맞추면: 폭심 데미지 (1.2배)
조금 빗나가면: 유효 범위 (0.8-1.0배)
많이 빗나가면: 외곽 (0.2-0.8배)
```

### 3. 전술적 선택
```
집중 폭발:
- 단일 타겟에 강함
- 정확도 중요
- 빗나가면 손해

광역 폭발:
- 다수 타겟에 강함
- 정확도 덜 중요
- 넓은 범위 커버
```

---

## 비교: 구버전 vs 신버전

### 구버전 (Gaussian)
```
offset 0: 100%
offset 1: 60%  ← 급격한 감소
offset 2: 13%  ← 거의 무의미
offset 3: 1%   ← 없는 것과 같음

문제:
- 유효 범위가 ±1 섹터에 불과
- 밸런싱 어려움
- 시각적으로 보이는 폭발 범위와 실제 불일치
```

### 신버전 (2단계 시스템)
```
Core (0-2):       100-120%  ← 폭심
Effective (3-4):  80-100%   ← 유효
Outer (5-6):      20-80%    ← 외곽

장점:
- 명확한 구간 구분
- 예측 가능한 데미지
- 유효 범위 ±4-6 섹터로 확장
- 시각적 폭발과 일치
```

---

## 시각적 표현

### VFX 연동
```csharp
private void SpawnExplosionVFX()
{
    var missileData = _weaponData as PolarMissileWeaponData;
    if (missileData?.ExplosionVFXPrefab == null) return;
    
    // 중심 폭발
    var core = Instantiate(missileData.ExplosionVFXPrefab, 
                          transform.position, 
                          Quaternion.identity);
    
    // 크기를 폭심 반경에 맞춤
    float scale = missileData.CoreRadius * 0.3f;
    core.transform.localScale = Vector3.one * scale;
    
    Destroy(core, 2f);
}
```

### UI 표시
```
발사 전 조준 시:
- 폭심 범위: 빨간 원 (진하게)
- 유효 범위: 주황 원 (중간)
- 최대 범위: 노란 원 (흐리게)
```

---

## 구현 우선순위

### Phase 1: 데이터 구조 (필수)
- [ ] PolarMissileWeaponData 확장
- [ ] ExplosionFalloffType enum 추가
- [ ] 기본 프리셋 생성

### Phase 2: 계산 로직 (필수)
- [ ] ApplyExplosionDamage 재구현
- [ ] CalculateFalloff 구현
- [ ] 3단계 영역 처리

### Phase 3: 밸런싱 (필수)
- [ ] 프리셋별 총 데미지 계산
- [ ] DPS 밸런싱
- [ ] 테스트 플레이

### Phase 4: 비주얼 (선택)
- [ ] VFX 크기 연동
- [ ] 조준 UI 표시
- [ ] 이펙트 단계별 표현

---

## 테스트 시나리오

### 1. 폭심 테스트
```
조건: 정중앙 명중
기대: 폭심 범위 내 모든 섹터 동일 데미지
검증: 범위 밖은 감쇠 적용
```

### 2. 유효 범위 테스트
```
조건: 폭심 밖 유효 범위 내 명중
기대: 80-100% 데미지
검증: 여전히 의미있는 피해
```

### 3. 외곽 테스트
```
조건: 유효 범위 밖 명중
기대: 20-80% 감쇠
검증: 영향은 있지만 약함
```

---

## 요약

### 핵심 개선

**구버전:**
- Gaussian 단일 곡선
- 유효 범위 ±1-2 섹터
- 급격한 감쇠

**신버전:**
- 3단계 영역 시스템
- 유효 범위 ±4-6 섹터
- 명확한 구간별 의미

### 설계 철학

1. **폭심 (Core)** - "직격탄의 쾌감"
2. **유효 범위 (Effective)** - "충분히 아픈 영역"
3. **외곽 (Outer)** - "여파가 미치는 영역"

### 게임플레이

- ✅ 명확한 역할 정의
- ✅ 정확도 보상 명확
- ✅ 밸런싱 예측 가능
- ✅ 전술적 선택 다양

---

**미사일은 이제 진정한 "폭발 무기"가 됩니다!**

