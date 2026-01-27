# 중력 무기 만들기 (효과 시스템 사용)

## 기존 방식 (삭제됨 ❌)
```
PolarGravityWeaponData (전용 무기 데이터)
└── PolarGravityProjectile (전용 투사체)
    └── 중력장 로직 내장
```
**문제점**: 
- 미사일에 중력 추가 불가
- 머신건에 중력 추가 불가
- 재사용 불가

## 새로운 방식 (효과 시스템 ✅)
```
PolarMissileWeaponData (일반 미사일)
└── Impact Effects
    └── GravityFieldEffect (중력 효과 추가)
```
**장점**:
- 어떤 무기에도 중력 추가 가능
- 효과 재사용 가능
- 효과 조합 가능

## 단계별 가이드

### 1단계: 중력 효과 에셋 생성

```
Project 창 우클릭
→ Create → EarthDefense → Polar → Effects → Gravity Field
```

**파일명**: `GravityFieldEffect_Standard.asset`

**Inspector 설정**:
```
Field Radius: 10
Speed Multiplier: 0.2 (80% 둔화)
Duration: 5
Use Gaussian Falloff: ✓
Field Color: (0.5, 0.8, 1.0, 0.5)
```

### 2단계: 미사일 무기 데이터 생성

```
Project 창 우클릭
→ Create → EarthDefense → Polar → Weapon Data → Missile
```

**파일명**: `GravityMissile.asset`

**Inspector 설정**:
```
[ID & UI]
- ID: gravity_missile
- Weapon Name: Gravity Missile

[Combat]
- Damage: 50
- Knockback Power: 0.5
- Area Type: Explosion

[Missile Specific]
- Fire Rate: 1
- Missile Speed: 12
- Explosion Radius: 5

[Impact Effects] ⭐
- Size: 1
- Element 0: GravityFieldEffect_Standard
```

### 3단계: 완료!

미사일이 벽에 충돌하면:
1. 폭발 데미지 적용 (50 데미지)
2. 중력장 자동 생성 (반경 10, 5초 지속)

## 다양한 조합 예시

### 예시 1: 중력 머신건
```
PolarMachinegunWeaponData "GravityBullet"
├── Fire Rate: 10/s
├── Spread Angle: 5
└── Impact Effects: [GravityFieldEffect_Standard]
```
**결과**: 빠른 연사로 여러 중력장 생성

### 예시 2: 복합 효과 미사일
```
PolarMissileWeaponData "SuperMissile"
└── Impact Effects: [3]
    ├── GravityFieldEffect_Standard (중력 둔화)
    ├── FireEffect (화염 지속 피해)
    └── StunEffect (적 행동 불가)
```
**결과**: 한 발로 3가지 효과!

### 예시 3: 강력한 중력 미사일
```
PolarMissileWeaponData "BlackHole"
└── Impact Effects: [1]
    └── GravityFieldEffect_Strong
        ├── Field Radius: 20 (넓은 범위)
        ├── Speed Multiplier: 0.1 (90% 둔화)
        └── Duration: 10 (오래 지속)
```

### 예시 4: 약한 중력 탄환 (스팸용)
```
PolarMachinegunWeaponData "SlowBullet"
├── Fire Rate: 20/s (빠른 연사)
└── Impact Effects: [1]
    └── GravityFieldEffect_Weak
        ├── Field Radius: 3 (좁은 범위)
        ├── Speed Multiplier: 0.5 (50% 둔화)
        └── Duration: 2 (짧은 지속)
```

## 효과 재사용

### 시나리오: 모든 무기에 약한 중력 추가

**1. 중력 효과 생성**
```
GravityFieldEffect_Light
├── Field Radius: 5
├── Speed Multiplier: 0.8 (20% 둔화)
└── Duration: 2
```

**2. 여러 무기에 추가**
```
LaserWeaponData → Impact Effects: [GravityFieldEffect_Light]
MissileWeaponData → Impact Effects: [GravityFieldEffect_Light]
MachinegunWeaponData → Impact Effects: [GravityFieldEffect_Light]
```

**3. 밸런스 조정**
```
GravityFieldEffect_Light의 Speed Multiplier를 0.7로 변경
→ 모든 무기의 중력 효과가 일괄 변경됨!
```

## 효과 프리셋 모음

### 전략적 중력장 (방어용)
```
GravityField_Defensive
├── Field Radius: 15 (넓은 범위)
├── Speed Multiplier: 0.3 (70% 둔화)
└── Duration: 8 (긴 지속)
```

### 전술적 중력장 (공격용)
```
GravityField_Tactical
├── Field Radius: 8 (중간 범위)
├── Speed Multiplier: 0.4 (60% 둔화)
└── Duration: 4 (중간 지속)
```

### 순간 중력장 (연사용)
```
GravityField_Burst
├── Field Radius: 3 (좁은 범위)
├── Speed Multiplier: 0.6 (40% 둔화)
└── Duration: 1.5 (짧은 지속)
```

## 기존 중력 전용 무기와의 비교

### 기존 (삭제됨)
```
PolarGravityWeaponData
└── 전용 설정: fieldRadius, speedMultiplier, duration
    └── PolarGravityProjectile (전용 투사체)
        └── 중력장 로직 내장

사용: 중력 무기만 중력장 사용 가능
확장: 불가능
재사용: 불가능
```

### 새로운 방식
```
PolarMissileWeaponData (또는 아무 무기)
└── Impact Effects: [GravityFieldEffect]

GravityFieldEffect (독립 에셋)
└── 설정: fieldRadius, speedMultiplier, duration

사용: 모든 무기에 중력장 추가 가능
확장: 새 효과 추가 쉬움 (FireEffect, PoisonEffect 등)
재사용: 한 효과를 여러 무기에 사용
```

## 마이그레이션 가이드

### 기존 중력 무기 → 새 방식

**Before**:
```
PolarGravityWeaponData "OldGravity"
├── Field Radius: 10
├── Speed Multiplier: 0.2
└── Duration: 5
```

**After**:
```
1. GravityFieldEffect 생성
   ├── Field Radius: 10
   ├── Speed Multiplier: 0.2
   └── Duration: 5

2. PolarMissileWeaponData "NewGravityMissile"
   ├── (미사일 설정)
   └── Impact Effects: [GravityFieldEffect]
```

## 다음 단계

### 추가 효과 구현
1. **화염 효과**: 지속 피해
2. **독 효과**: 시간 경과 피해
3. **빙결 효과**: 완전 정지
4. **폭발 연쇄 효과**: 추가 폭발
5. **텔레포트 효과**: 적 위치 이동

모두 동일한 `IPolarProjectileEffect` 인터페이스로 구현!

