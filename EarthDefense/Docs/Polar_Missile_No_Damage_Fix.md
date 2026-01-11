# 미사일 데미지 문제 해결 가이드

## 🐛 문제 진단

### 증상
미사일이 충돌해도 데미지를 주지 못함

### 원인 분석

제공하신 JSON 데이터:
```json
{
    "baseData": {
        "areaType": "Fixed",  // ← 문제!
        "damage": 5.0
    }
}
```

**문제점:**
1. `areaType`이 `"Fixed"`로 설정됨
2. 미사일은 `"Explosion"` 타입이어야 함
3. `ApplyCombatDamage`에서 `PolarMissileWeaponData`가 있으면 자동으로 3단계 폭발 시스템을 사용하도록 수정됨

---

## ✅ 해결 방법

### 방법 1: Unity Inspector에서 수정 (권장)

1. **미사일 무기 데이터 에셋 선택**
   - Project 창에서 `MissileTurret` 에셋 선택

2. **Inspector에서 AreaType 변경**
   ```
   Combat → Area Type → "Explosion" 선택
   ```

3. **저장**
   - Ctrl+S 또는 File → Save

### 방법 2: JSON 수정 후 Import

올바른 JSON:
```json
{
    "baseData": "{\"id\":\"MissileTurret\",\"weaponName\":\"MissileTurret\",\"weaponBundleId\":\"MissileTurret\",\"projectileBundleId\":\"Missile\",\"damage\":5.0,\"knockbackPower\":0.2,\"areaType\":\"Explosion\",\"damageRadius\":5,\"useGaussianFalloff\":true,\"woundIntensity\":0.2,\"tickRate\":10.0}",
    "fireRate": 0.5,
    "missileSpeed": 12.0,
    "missileLifetime": 5.0,
    "coreRadius": 1,
    "effectiveRadius": 5,
    "maxRadius": 8,
    "coreMultiplier": 1.0,
    "effectiveMinMultiplier": 0.8,
    "maxMinMultiplier": 0.1,
    "falloffType": "Smooth",
    "missileScale": 0.5,
    "missileColor": [1.0, 0.0, 0.0, 1.0]
}
```

**변경 사항:**
- `"areaType":"Fixed"` → `"areaType":"Explosion"`

**Import 방법:**
1. 위 JSON을 파일로 저장 (`MissileTurret_fixed.json`)
2. Unity에서 미사일 데이터 에셋 선택
3. Inspector 하단 → `Import from JSON` 클릭
4. 저장한 JSON 파일 선택

---

## 🔍 추가된 디버그 로그

이제 미사일 발사 시 상세한 로그가 출력됩니다:

### Launch 시
```
[PolarMissile] Launched: Damage=5, AreaType=Fixed, CoreRadius=1, EffectiveRadius=5, MaxRadius=8
```

### Collision 시
```
[PolarMissile] Collision detected at angle=45.0°, radius=10.50
[PolarMissile] No explosion data! Applying single sector damage: 5, AreaType: Fixed
```
또는
```
[PolarMissile] Applying 3-stage explosion damage at sector 12, Base Damage: 5
[PolarMissile] 3-Stage Explosion: Core=1, Effective=5, Max=8, BaseDamage=5
  [Core] Center sector 12: 5 damage
[PolarMissile] Explosion complete: 17 sectors hit
```

### 로그 확인 방법
1. Unity 실행
2. 미사일 발사
3. Console 창 확인 (Window → General → Console)

---

## 🎯 예상 동작 (수정 후)

### AreaType = "Explosion"일 때

```
1. 미사일 발사
   [PolarMissile] Launched: Damage=5, AreaType=Explosion, ...

2. 충돌
   [PolarMissile] Collision detected at angle=90.0°, radius=12.34
   [PolarMissile] Applying 3-stage explosion damage at sector 24, Base Damage: 5

3. 폭발 범위 계산
   [PolarMissile] 3-Stage Explosion: Core=1, Effective=5, Max=8, BaseDamage=5
   
4. 데미지 적용
   [Core] Center sector 24: 5 damage (100%)
   섹터 23: 5 damage (Core)
   섹터 25: 5 damage (Core)
   섹터 22: 4.5 damage (Effective)
   섹터 26: 4.5 damage (Effective)
   ...
   섹터 16: 0.5 damage (Outer)
   섹터 32: 0.5 damage (Outer)

5. 완료
   [PolarMissile] Explosion complete: 17 sectors hit
```

---

## 🛠️ 코드 개선 사항

### 1. AreaType 체크 개선

**이전:**
```csharp
if (props.AreaType == PolarAreaType.Explosion)
{
    ApplyExplosionDamage(...);
}
else
{
    // 단일 섹터만 타격
    _field.ApplyDamageToSector(centerIndex, props.Damage);
}
```

**개선 후:**
```csharp
// PolarMissileWeaponData가 있으면 무조건 3단계 폭발 시스템 사용
var missileData = _weaponData as PolarMissileWeaponData;
if (missileData != null)
{
    ApplyExplosionDamage(...);  // ← AreaType과 무관하게 작동!
}
else if (props.AreaType == PolarAreaType.Explosion)
{
    ApplyExplosionDamage(...);  // Fallback
}
else
{
    // 경고 로그와 함께 단일 섹터 타격
    Debug.LogWarning($"No explosion data! AreaType: {props.AreaType}");
    _field.ApplyDamageToSector(centerIndex, props.Damage);
}
```

**장점:**
- `PolarMissileWeaponData`를 사용하면 `AreaType`이 잘못 설정되어도 폭발 데미지 적용
- 하위 호환성 유지 (일반 `PolarWeaponData` + `Explosion` 타입도 지원)

### 2. 상세한 디버그 로그

모든 주요 지점에 로그 추가:
- Launch 시점
- Collision 시점
- 데미지 적용 시점
- 각 폭발 단계별 적용 현황

---

## 📊 테스트 체크리스트

### 수정 전 확인
- [ ] Console에서 `[PolarMissile]` 로그 확인
- [ ] `AreaType: Fixed` 메시지 확인
- [ ] `No explosion data!` 경고 확인

### 수정 후 확인
- [ ] AreaType을 `Explosion`으로 변경
- [ ] 미사일 발사 테스트
- [ ] Console에서 `3-Stage Explosion` 로그 확인
- [ ] `X sectors hit` 메시지 확인 (17개 섹터)
- [ ] 실제 필드에 데미지 적용 확인

---

## 💡 추가 팁

### DPS 계산

현재 설정:
```
Damage: 5 (BaseDamage)
FireRate: 0.5 (초당 0.5발 = 2초마다 1발)
CoreRadius: 1 (±1 섹터 = 3 섹터)
EffectiveRadius: 5 (±5 섹터 = 11 섹터)
MaxRadius: 8 (±8 섹터 = 17 섹터)

총 데미지 (1발):
- Core: 5 × 3 = 15
- Effective: ~4 × 8 = 32
- Outer: ~2 × 6 = 12
= 약 59 데미지

DPS = 59 × 0.5 = 29.5 DPS (광역)
```

**너무 약하면:**
- `damage`를 10으로 증가 → DPS ~60
- `fireRate`를 1.0으로 증가 → DPS ~60

### 폭발 범위 조정

**더 넓게:**
```
coreRadius: 2 (±2 섹터)
effectiveRadius: 7
maxRadius: 10
```

**더 강하게 (중심 집중):**
```
coreMultiplier: 1.5 (폭심 150%)
effectiveMinMultiplier: 0.5 (유효 범위 최소 50%)
```

---

## ✅ 요약

### 문제
- `areaType`이 `"Fixed"`로 설정되어 단일 섹터만 타격
- 3단계 폭발 시스템이 작동하지 않음

### 해결
1. **Unity Inspector에서** `Area Type`을 `Explosion`으로 변경
2. **또는** 올바른 JSON을 Import

### 개선
- `PolarMissileWeaponData` 감지 시 자동으로 3단계 폭발 적용
- 상세한 디버그 로그 추가
- AreaType 오류 시 경고 메시지 출력

---

**이제 미사일이 정상적으로 폭발 데미지를 적용할 것입니다!**

