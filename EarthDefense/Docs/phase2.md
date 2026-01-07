# Phase 2: Combat Refinement (Resistance & Knockback)

## 개요

Phase 1에서 구축한 극좌표 시뮬레이션 엔진을 기반으로, 정교한 전투 메커니즘을 추가합니다. 적의 최전선을 "라인(Line)"이라는 개념으로 정의하고, 저항력(Resistance) 시스템과 넉백(Knockback) 메커니즘을 통해 전략적 깊이를 부여합니다.

---

## 핵심 개념

### 라인(Line)
- **정의:** 각 섹터의 최전선 (가장 플레이어에 가까운 지점)
- **데이터:** `sectorRadii[i]` (기존 Phase 1 데이터 재활용)
- **역할:** 적의 진격 경계선이자 전투 발생 지점

### 저항력(Resistance) - **적(Enemy) 속성**
- **정의:** 라인이 버틸 수 있는 에너지 총량
- **단위:** 임의 단위 (예: 100.0 = 기본 저항력)
- **특성:**
  - 섹터별 독립적 관리
  - 공격받으면 감소
  - 0 도달 시 라인 붕괴 → 넉백 발생
- **설정 위치:** `PolarDataConfig` (Enemy Settings)

### 넉백(Knockback) - **적(Enemy) 속성**
- **정의:** 라인 붕괴 시 적이 후퇴하는 거리
- **계산:** `knockbackDistance = weapon.knockbackPower`
- **효과:**
  - 라인 위치 변경: `sectorRadii[i] += knockbackDistance`
  - 저항력 리셋: `sectorResistance[i] = maxResistance`
  - 일시적 안전 공간 확보
- **설정 위치:** `PolarDataConfig` (Enemy Settings)

---

## 개념 분리: 무기 vs 적

| 속성 | 무기 (Weapon) | 적 (Enemy) |
|------|---------------|------------|
| **데이터 위치** | `WeaponData` (ScriptableObject) | `PolarDataConfig` (Enemy Settings) |
| **역할** | 공격 특성 정의 | 방어 특성 정의 |
| **Phase 2 핵심** | Damage, KnockbackPower, AreaType | BaseResistance, KnockbackRange, ResistanceRegen |
| **구현 단계** | **Step 2** | **Step 1** |

---

## 공통 정의 (Phase 1 상속)
- `SECTOR_COUNT`: `180` (고정)
- `EARTH_RADIUS`: `0.5` (Unity 월드 단위)
- `INITIAL_RADIUS`: `5.0`
- 중력 가속 로직: 단계별로 `1.5x` 증가

## 새로운 정의 (Phase 2)

### 적(Enemy) 설정 - PolarDataConfig (Step 1)
- `BASE_RESISTANCE = 100.0f` - 기본 저항력
- `RESISTANCE_REGEN_RATE = 0.0f` - 자동 회복 (초당 %, 0 = 비활성, 0.05 = 5%/초)
- `RESISTANCE_DAMAGE_MULTIPLIER = 1.0f` - 피해 배율
- `MIN_KNOCKBACK = 0.1f` - 최소 넉백 거리
- `MAX_KNOCKBACK = 2.0f` - 최대 넉백 거리
- `KNOCKBACK_COOLDOWN = 0.5f` - 연속 넉백 방지 (초)

### 무기(Weapon) 설정 - WeaponData (Step 2)
- **타격 범위 타입:** `Fixed` (고정), `Gaussian` (가우시안), `Explosion` (폭발)
- **레이저 (Drill):** Damage=5, Knockback=0.1, AreaType=Fixed, TickRate=10
- **머신건 (Ripper):** Damage=8, Knockback=0.3, AreaType=Gaussian, FireRate=10, Radius=3
- **미사일 (Hammer):** Damage=50, Knockback=1.5, AreaType=Explosion, FireRate=0.5, Radius=10

---

# Step 1: Enemy System (Resistance & Knockback) ✅ 완료

## Step 1-1: Resistance Data Layer ✅
### 구현 완료
- ✅ `PolarDataConfig` 저항력 설정 추가
- ✅ `PolarFieldController` 저항력 데이터 레이어 추가
- ✅ 저항력 회복 로직 구현 (비율 기반)
- ✅ 디버그 로그 추가

### 수락 기준
- ✅ 콘솔에서 180개 저항력 값 정상 초기화
- ✅ `ApplyDamageToSector()` 호출 시 저항력 감소
- ✅ 저항력 0 도달 시 `OnLineBreak` 이벤트 발생
- ✅ 디버그 로그에 `Min/Max/Avg Resistance` 통계 출력
- ✅ `ResistanceRegenRate > 0` 설정 시 자동 회복

## Step 1-2: Knockback Mechanics ✅
### 구현 완료
- ✅ `PolarDataConfig` 넉백 설정 추가
- ✅ `PolarFieldController` 넉백 메커니즘 추가
- ✅ 넉백 쿨다운 시스템 구현
- ✅ Gizmo 시각화 (노란색 구)

### 수락 기준
- ✅ 라인 붕괴 시 `sectorRadii` 증가
- ✅ 넉백 후 저항력 `BaseResistance`로 리셋
- ✅ 쿨다운 중 추가 넉백 무시
- ✅ Scene View에서 쿨다운 상태 Gizmo 표시
- ✅ `OnKnockbackExecuted` 이벤트 정상 발동
- ✅ `SetLastWeaponKnockback()`으로 넉백 파워 전달

---

# Step 2: Weapon System (Damage & Area Types) (예상 완료 시간: 2시간)

## 개요
무기의 공격 특성을 정의하고, 저항력 시스템과 연동합니다. 타격 범위(AreaType)에 따라 다른 피해 계산 방식을 구현합니다.

---

## 무기 타입별 특성 정의

### 1. **레이저 (Drill) - 드릴형**
**느낌:** 좁은 곳을 꾸준히 지지는 용접기

**전술:** 약점 집중 타격, 장시간 유지

---

### 2. **머신건 (Ripper) - 갈퀴형**
**느낌:** 표면을 긁어내고 상처를 벌려놓는 느낌

**전술:** 약점 확장, 영역 무력화

---

### 3. **미사일 (Hammer) - 해머형**
**느낌:** 쾅! 하고 넓은 범위를 한 번에 밀어내는 묵직함

**전술:** 대규모 후퇴, 긴급 공간 확보

---

## Step 2-1: WeaponData Extension (예상 30분)

### Todo
- [ ] `WeaponData.cs` 확장
  - [ ] `AreaType` 열거형 정의 (Fixed, Gaussian, Explosion)
  - [ ] `damage` 속성 추가
  - [ ] `knockbackPower` 속성 추가
  - [ ] `areaType` 속성 추가
  - [ ] `useGaussianFalloff` 속성 추가
  - [ ] `damageRadius` 속성 추가
  - [ ] `woundIntensity` 속성 추가

### 수락 기준
- [ ] `AreaType` 열거형 정의 완료
- [ ] `WeaponData` 새로운 속성 추가 완료
- [ ] Inspector에서 각 속성 조정 가능

## Step 2-2: Projectile Damage Integration (예상 1시간)

### Todo
- [ ] `PolarProjectile` 확장 (미사일용)
  - [ ] `_weaponData` 필드 추가
  - [ ] 충돌 로직 변경: `ApplyWeaponDamage()`
  - [ ] `ApplyGaussianDamage()` 메서드 작성
  - [ ] `ApplyExplosionDamage()` 메서드 작성

- [ ] `BeamProjectile` 확장 (레이저용)
  - [ ] 틱 데미지를 저항력 감소로 변경
  - [ ] `CalculateSectorIndexFromChunk()` 유틸리티 작성

### 수락 기준
- [ ] 투사체 충돌 시 저항력 감소
- [ ] 저항력 0 도달 시 넉백 자동 실행
- [ ] 레이저: 단일 섹터 타격 (AreaType.Fixed)
- [ ] 머신건: 가우시안 분포 피해 (AreaType.Gaussian)
- [ ] 미사일: 폭발 범위 타격 (AreaType.Explosion)
- [ ] 각 무기별 Damage/Knockback 차이 체감

## Step 2-3: Weapon Presets Creation (예상 30분)

### Todo
- [ ] `LaserWeaponData.asset` 생성
  - damage=5, knockbackPower=0.1, areaType=Fixed, damageRadius=1, woundIntensity=0.3
- [ ] `MachinegunWeaponData.asset` 생성
  - damage=8, knockbackPower=0.3, areaType=Gaussian, damageRadius=3, woundIntensity=0.6
- [ ] `MissileWeaponData.asset` 생성
  - damage=50, knockbackPower=1.5, areaType=Explosion, damageRadius=10, woundIntensity=0.8

### 생성 위치
```
Assets/Resources/Weapons/Data/
├─ LaserWeaponData.asset (Drill)
├─ MachinegunWeaponData.asset (Ripper)
└─ MissileWeaponData.asset (Hammer)
```

### 수락 기준
- [ ] 3개의 무기 프리셋 생성 완료
- [ ] 각 프리셋 수치 정상 설정
- [ ] Inspector에서 수정 가능

---

# Step 3: Visual & Audio Feedback (예상 1시간)

### Todo
- [ ] 저항력 시각화
  - [ ] `PolarBoundaryRenderer` 확장: 저항력 기반 색상 변경
  - [ ] 색상 그라디언트 정의 (100% = 청록색, 50% = 노란색, 10% = 빨간색)
  - [ ] Vertex Color 적용
  - [ ] 실시간 업데이트

- [ ] 넉백 이펙트 (선택적)
  - [ ] `PolarKnockbackVFX` 클래스 작성
  - [ ] 충격파 파티클 시스템

- [ ] 오디오 (선택적)
  - [ ] `PolarAudioManager` 클래스 작성
  - [ ] 저항력 감소/라인 붕괴/넉백 사운드

### 수락 기준
- [ ] 저항력이 낮은 섹터가 붉게 표시
- [ ] 색상 변화가 부드럽고 직관적
- [ ] 넉백 발생 시 시각 효과 재생 (선택적)
- [ ] 각 이벤트마다 적절한 사운드 재생 (선택적)

---

# Step 4: Balancing & Polish (예상 30분)

### Todo
- [ ] 수치 조정
  - [ ] 저항력 기본값 테스트 (50 / 100 / 200)
  - [ ] 넉백 거리 범위 조정
  - [ ] 무기별 DPS 밸런싱

- [ ] 에지 케이스
  - [ ] 넉백으로 `InitialRadius` 초과 방지
  - [ ] 저항력 음수 방지
  - [ ] 섹터 인덱스 범위 검증

- [ ] 디버그 툴
  - [ ] Inspector 버튼: "Reset All Resistances"
  - [ ] Inspector 버튼: "Clear All Cooldowns"
  - [ ] Gizmo: 저항력 % 표시

### 수락 기준
- [ ] 게임플레이가 직관적이고 반응성 좋음
- [ ] 극단적 상황에서 버그 없음
- [ ] 디버그 툴로 쉽게 조정 가능

---

## 전체 체크리스트
- [x] **Step 1: Enemy System (Resistance & Knockback)** ✅
  - [x] Step 1-1: Resistance Data Layer ✅
  - [x] Step 1-2: Knockback Mechanics ✅
- [ ] **Step 2: Weapon System (Damage & Area Types)**
  - [ ] Step 2-1: WeaponData Extension
  - [ ] Step 2-2: Projectile Damage Integration
  - [ ] Step 2-3: Weapon Presets Creation
- [ ] **Step 3: Visual & Audio Feedback**
- [ ] **Step 4: Balancing & Polish**

---

## 구현 파일

### Step 1: Enemy System ✅
- `PolarDataConfig.cs` - 저항력/넉백 설정
- `PolarFieldController.cs` - 저항력/넉백 로직

### Step 2: Weapon System
- `WeaponData.cs` - 무기 전투 속성
- `PolarProjectile.cs` - 미사일 피해 적용
- `BeamProjectile.cs` - 레이저 틱 데미지
- `LaserWeaponData.asset` - 레이저 프리셋
- `MachinegunWeaponData.asset` - 머신건 프리셋
- `MissileWeaponData.asset` - 미사일 프리셋

### Step 3: Visual & Audio Feedback
- `PolarBoundaryRenderer.cs` - 저항력 색상 시각화
- `PolarKnockbackVFX.cs` - 넉백 이펙트 (선택적)
- `PolarAudioManager.cs` - 사운드 (선택적)

---

## Phase 2 완료 목표

### 핵심 기능
- ✅ 섹터별 저항력 시스템 (Step 1)
- ✅ 라인 붕괴 → 넉백 메커니즘 (Step 1)
- ⏳ 무기-저항력 연동 (Step 2)
- ⏳ 시각/청각 피드백 (Step 3)

### 전략적 깊이 추가
- **공간 관리:** 어느 섹터를 먼저 밀어낼 것인가?
- **리소스 관리:** 화력 집중 vs 분산
- **리스크/리워드:** 저항력 낮은 섹터 = 약점이자 기회
- **무기 선택:** Drill vs Ripper vs Hammer

### HTML 대비 진화
| 항목 | HTML (Phase 1) | Unity (Phase 2) |
|------|----------------|-----------------|
| 공격 메커니즘 | 즉시 밀어내기 | 저항력 → 넉백 |
| 전략성 | 낮음 | 높음 (약점 공략) |
| 피드백 | 시각만 | 시각 + 청각 |
| 데이터 깊이 | 1차원 (반지름) | 2차원 (반지름 + 저항력) |
| 무기 다양성 | 단일 | 3종 (Drill/Ripper/Hammer) |

---

## 아키텍처 (Phase 2)

```
Step 1: Enemy System ✅
PolarDataConfig (ScriptableObject)
└─ Enemy Settings
   ├─ BaseResistance
   ├─ ResistanceRegenRate
   ├─ MinKnockback / MaxKnockback
   └─ KnockbackCooldown
       ↓
PolarFieldController (180개 데이터 × 2)
├─ _sectorRadii[] (Phase 1)
└─ _sectorResistances[] (Phase 2)
    ├─ ApplyDamageToSector(index, damage)
    ├─ ExecuteKnockback(index, power)
    └─ Events: OnLineBreak, OnKnockbackExecuted

Step 2: Weapon System ⏳
WeaponData (ScriptableObject)
└─ Combat Properties
   ├─ Damage
   ├─ KnockbackPower
   ├─ AreaType (Fixed/Gaussian/Explosion)
   ├─ DamageRadius
   └─ WoundIntensity
       ↓
PolarProjectile / BeamProjectile
└─ ApplyWeaponDamage()
    ├─ AreaType.Fixed → 단일 섹터
    ├─ AreaType.Gaussian → 가우시안 분포
    └─ AreaType.Explosion → 폭발 범위
        ↓
PolarFieldController.ApplyDamageToSector()
        ↓
Step 3: Visual Feedback ⏳
PolarBoundaryRenderer
└─ GetResistanceColor() → Vertex Color
```

---

## 성능 지표 (Phase 2 추가)

```
Phase 1:
- 메시: 362 정점, 1 Draw Call, < 0.1ms
- 투사체: 20개 풀, < 0.2ms
- 총합: < 0.5ms

Phase 2 추가:
- 저항력 계산: 180개 배열 순회, < 0.05ms
- 넉백 쿨다운: 180개 배열 순회, < 0.05ms
- 색상 업데이트: 360개 정점 색상, < 0.1ms
- 총합 증가: < 0.2ms

전체 예상:
- 프레임 비용: < 0.7ms
- 60fps 안정적 유지
```

---

## 참고 자료
- `Docs/phase1.md` - Phase 1 구현 완료 사항
- `Docs/ShapeDefense_CoreConcepts.md` - 핵심 개념
- `Docs/ShapeDefense_Terminology.md` - 용어 정의
- `game2B.html` - HTML 원본 (비교용)
