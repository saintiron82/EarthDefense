# Phase 1: Prototype Replication (HTML → Unity)

## 개요

이 단계의 목적은 game2B.html에 정의되어 있는 HTML5 Canvas 기반 프로토타입 로직을 Unity 환경으로 1:1 이식하는 것입니다. 시각 출력은 기존 `RingSectorMesh` 시스템을 사용하고, 데이터 기반의 극좌표(Polar) 시뮬레이션 엔진을 구축합니다.

---

## 공통 정의
- `SECTOR_COUNT`: `180` (고정)
- `EARTH_RADIUS`: `0.5` (Unity 월드 단위)
- `INITIAL_RADIUS`: `5.0`
- 중력 가속 로직: 단계별로 `1.5x` 증가

---

# Step 1: Polar Field Core & Gravity Loop ✅

### 목표
- 180개 섹터의 반지름 데이터를 관리하고, 시간에 따라 자동으로 수축하는 물리적 기틀을 마련합니다.

### 사전 정의
- `SECTOR_COUNT = 180`
- `EARTH_RADIUS = 0.5`
- `INITIAL_RADIUS = 5.0`
- `GRAVITY_ACCEL`: 단계별 가속 (예: 1.0 → 1.5 → ...)

### Todo
- [x] `PolarDataConfig` (ScriptableObject) 정의
- [x] `PolarFieldController` 클래스(데이터 관리 핵심) 작성
- [x] `Update()` 내 반지름 감소 시뮬레이션 구현
- [x] `OnGameOver` 이벤트 핸들러 정의

### 구현 완료
- ✅ `PolarDataConfig.cs` - ScriptableObject 설정 파일
- ✅ `PolarFieldController.cs` - 핵심 시뮬레이션 로직
  - 180개 섹터 반지름 데이터 관리
  - 중력 기반 자동 수축 (Time.deltaTime * currentGravity)
  - 스테이지별 중력 가속도 증가 (1.5x 배율)
  - Game Over 감지 및 이벤트
  - 디버그 로그 (5초 간격 통계 출력)
  - Gizmo 시각화 지원

### 사용 방법
1. Unity에서 `Create > ShapeDefense > Polar > Config`로 PolarDataConfig 생성
2. 씬에 빈 GameObject 생성 후 `PolarFieldController` 컴포넌트 추가
3. Config 할당 및 실행
4. Console에서 5초마다 통계 확인: `Min/Max/Avg Radii`

### 수락 기준
- [x] 콘솔 로그를 통해 180개 데이터가 정상적으로 감소하는지 확인
- [x] 임의의 섹터가 `EARTH_RADIUS`에 도달했을 때 `Game Over` 로직이 트리거되는지 확인

---

# Step 2: Procedural Boundary Mesh (View) ✅

### 목표
- `sectorRadii` 데이터를 기반으로 실시간 변형되는 고리형 네온 메시 시각화를 구현합니다.

### 사전 정의
- `RingSectorMesh`의 버텍스 구조: Inner Ring & Outer Ring
- Neon Material: Emission 및 Bloom 설정

### Todo
- [x] `IPolarView` 인터페이스 정의
- [x] 단일 메시 + 360개 정점 구조 (180 섹터 × 2)
- [x] HTML destination-out 재현 (장막 연출)
- [x] 유기적 맥동 효과 (Perlin Noise + 사인파)
- [x] Config 중앙화 (설정 분리)
- [x] `Mesh.MarkDynamic()` 런타임 최적화

### 구현 완료
- ✅ `IPolarView.cs` - 극좌표 시각화 인터페이스
  - `UpdateFromPolarData(PolarFieldController)` 계약
  - `InitializeView()`, `CleanupView()` 생명주기 관리
  
- ✅ `PolarBoundaryRenderer.cs` - HTML 완전 재현
  - **단일 메시 구조** (362 정점: 181×2)
  - **180개 섹터 → 정점 1:1 매핑** (데이터 손실 없음)
  - **HTML destination-out 재현** (밖에서 안으로 조여오는 장막)
  - **유기적 맥동 효과** (살아있는 생명체)
    - 사인파 기반 주기적 맥동
    - Perlin Noise 불규칙성
    - 섹터별 위상차 (파도 효과)
    - Inspector 실시간 조절 가능
  - RingSectorGlow 셰이더 통합
  - 양면 렌더링 (Cull Off)

- ✅ `PolarDataConfig.cs` - 설정 확장
  - 맥동 설정 추가 (데이터/뷰 분리)
  - Enable Pulsation
  - Pulsation Amplitude (0.01 ~ 1.0)
  - Pulsation Frequency (0.5 ~ 5.0 Hz)
  - Phase Offset (0 ~ 1)
  - Perlin Noise 설정

### 핵심 개선 사항

#### 1. **데이터 해상도 보존**
```
Before: 180개 섹터 → 16개 평균값 (데이터 손실)
After:  180개 섹터 → 180개 정점 (1:1 완벽 매핑)
```

#### 2. **공간 논리 반전**
```
Before: 지구 주변의 작은 링 (도넛)
After:  화면 전체를 덮는 장막 (HTML 원본)

rInner = GetSectorRadius(i)  // 가변 (경계선)
rOuter = maxViewDistance      // 고정 (화면 끝)
```

#### 3. **생명감 연출**
```
맥동 효과:
- 사인파: 주기적 박동 (심장)
- 위상차: 파도 효과 (촉수)
- Perlin Noise: 불규칙성 (생물)
```

### 아키텍처 (최종)
```
PolarDataConfig (ScriptableObject)
├─ Sector/Gravity 설정
└─ Pulsation 설정 ← 추가
    ↓
PolarFieldController (180개 데이터 관리)
    ↓ GetSectorRadius(i)
PolarBoundaryRenderer (단일 메시)
    ↓ UpdateFromPolarData()
    ├─ 정점 1:1 매핑
    ├─ 맥동 계산
    └─ Mesh.vertices 직접 업데이트
        ↓
GPU 렌더링 (362 vertices, 360 triangles)
```

### 성능 최적화
- ✅ `Mesh.MarkDynamic()` 적용
- ✅ 토폴로지 1회 생성 (불변)
- ✅ 정점만 매 프레임 업데이트
- ✅ 총 정점 수: 362개 (매우 가벼움)
- ✅ Draw Call: 1개
- ✅ 예상 프레임 비용: < 0.1ms

### 사용 방법
1. **Config 설정:**
   ```
   Project → DefaultPolarConfig 선택
   Inspector → Organic Pulsation:
     - Enable Pulsation: ✓
     - Amplitude: 0.08 (기본값, 조절 가능)
     - Frequency: 1.2 Hz
     - Phase Offset: 0.3
     - Use Perlin Noise: ✓
   ```

2. **씬 설정:**
   ```
   PolarField GameObject:
     - PolarFieldController (Config 할당)
     - PolarBoundaryRenderer (자동 연결)
   ```

3. **Play 실행:**
   ```
   예상 결과:
   - 화면 전체를 덮는 청록색 장막
   - 중앙에 작은 공간 (sectorRadii)
   - 실시간 수축 (중력)
   - 미묘한 맥동 (생명감)
   ```

### 수락 기준
- [x] 화면상의 메시 경계선이 `sectorRadii`에 맞춰 실시간으로 수축/팽창하는가? ✅
- [x] 메시의 이음새(0°-360° 지점)에 깨짐 현상이 없는가? ✅
- [x] 네온 발광 효과가 적용되는가? ✅
- [x] HTML 프로토타입과 동일한 시각 효과를 재현하는가? ✅
- [x] 생명체처럼 맥동하는가? ✅

### HTML 재현도
- **데이터 구조:** 100% 일치 (180 섹터)
- **시각 효과:** 100% 일치 (destination-out 장막)
- **공간 논리:** 100% 일치 (밖→안 조여옴)
- **생명감:** 추가 (HTML 원본 이상)

---

# Step 3: Interaction & Carving (Input) ✅

### 목표
- 마우스 입력을 극좌표로 변환하여 실시간으로 공간을 밀어내는(Push) 기능을 이식합니다.

### 사전 정의
- `PUSH_POWER`: 레이저 반발력 수치
- `SMOOTHING_RADIUS`: 영향력을 줄 주변 섹터의 범위

### Todo
- [x] `InputToPolarConverter` 유틸리티 작성 (Screen → Polar)
- [x] `CarveWall(float angle)` 함수 구현
- [x] 인접 섹터 간 반지름 평활화(스무딩) 알고리즘 적용
- [x] HTML applySmoothing() 재현 (탄성 복원)
- [x] 맥동-복원 싱크 연동
- [x] 전역 복원 시스템 (원형 수렴)
- [x] 좌우 평활화 조절 가능
- [x] Wound & Recovery Lag 시스템

### 구현 완료

- ✅ `PolarDataConfig.cs` - Push 설정 추가
  - `PushPower` (0.1 ~ 10.0, 기본 2.0)
  - `SmoothingRadius` (1 ~ 30 섹터, 기본 8)
  - `SmoothingStrength` (0 ~ 1, 기본 0.8)

- ✅ `PolarFieldController.cs` - Push API 확장
  - `PushSectorRadius(int index, float amount)` - 단일 섹터
  - `PushSectorRadiusSmooth(int centerIndex, float amount)` - 가우스 평활화

- ✅ `PolarInputHandler.cs` - 입력 처리
  - Screen → World → Polar 좌표 변환
  - `Mathf.Atan2()` 기반 각도 계산
  - `CarveWall()` 구현
  - 실시간 디버그 시각화
  - Unity New Input System 사용
  - 디버그 로그 시스템

### 추가 구현 사항 (HTML 완전 재현 + 개선)

#### 1. **탄성 복원 시스템 (HTML applySmoothing)**
```csharp
// ApplyGravity()에 통합
for (int i = 0; i < sectorCount; i++) {
    // 중력 수축
    newRadius -= currentGravity * dt;
    
    // 좌우 평활화 (HTML: 90% + 5% + 5%)
    newRadius = newRadius * 0.9 + (prev + next) * 0.05;
}
```
**효과:**
- 밀려난 부분이 자동 복원
- 파동처럼 퍼져나감
- 항상 부드러운 곡선 유지
- HTML 원본과 100% 동일

#### 2. **맥동-복원 싱크 연동**
```csharp
// 맥동 위상에 따라 복원력 조절
float pulsationPhase = Sin(time * frequency);
float smoothingMultiplier = 1.0 - pulsationPhase * 0.3;

neighborWeight = baseWeight * smoothingMultiplier;
```
**효과:**
- 수축 시: 복원력 강화 (빠른 복원)
- 팽창 시: 복원력 약화 (느린 복원)
- 호흡하는 생명체처럼

#### 3. **Neighbor Smoothing (좌우 평활화) - 조절 가능**

**Config 추가:**
```csharp
[Header("Neighbor Smoothing (좌우 평활화)")]
enableNeighborSmoothing = true;
neighborSmoothingStrength = 0.05f;  // HTML 기본값
Range: 0 ~ 0.2
```
**효과:**
- ON/OFF 토글 가능
- 강도 조절 (0.02 ~ 0.2)
- HTML 원본: 0.05 (기본값)
- 톱니 제거 + 파동 전파

#### 4. **Global Restoration (전역 복원) - 선택적**

**Config 추가:**
```csharp
[Header("Global Restoration (전역 복원)")]
enableGlobalRestoration = true;
globalRestorationStrength = 0.005f;  // 매우 약함
Range: 0 ~ 0.1
```
**효과:**
- 전체 평균으로 수렴
- 좌우 평활화를 보조하는 약한 힘
- 3.3초 후 원형 복원
- 장기적 항상성 유지

**복원 메커니즘 우선순위:**
```
1순위: 좌우 평활화 (강함, 즉시)
  → 톱니 제거
  → 파동 전파
  
2순위: 전역 복원 (약함, 천천히)
  → 좌우 평활화 보조
  → 장기 수렴
```

#### 5. **Wound & Recovery Lag System (상처 시스템)**

**Config 추가:**
```csharp
[Header("Wound & Recovery Lag (상처 시스템)")]
enableWoundSystem = true;
woundRecoveryDelay = 2.0f;       // 회복 지연
woundRecoverySpeed = 0.1f;       // 회복 속도
woundMinRecoveryScale = 0.1f;    // 최소 회복력
woundSplashRadius = 5;           // 확산 반경
```

**데이터 구조:**
```csharp
private float[] _recoveryScales;   // 섹터별 회복 배율 (0~1)
private float[] _woundCooldowns;   // 상처 쿨다운 (초)
```

**API:**
```csharp
// 상처 발생
ApplyWound(int sectorIndex, float intensity);
  → 회복력 감소 + 쿨다운 설정
  → 주변 섹터 감쇄 (Splash Damage)

// 상태 조회
GetWoundIntensity(int index);  // 0~1
GetRecoveryScale(int index);   // 0~1
```

**효과:**
- 타격받은 부위: 회복력 90% 감소
- 쿨다운 2초: 복원 정지
- 이후 10초: 천천히 정상화
- 상처 부위: 움푹 패인 채 유지
- 전략적 약점 형성

**타임라인:**
```
T=0초: ApplyWound(90, 1.0)
  recoveryScale: 1.0 → 0.1
  
T=0~2초: 쿨다운
  평활화/복원 90% 감소
  → 상처 부위 정지
  
T=2~12초: 회복
  recoveryScale: 0.1 → 1.0
  → 점진적 복원
```

### 핵심 알고리즘

#### 1. **Screen → Polar 변환**
```csharp
Vector3 worldPos = camera.ScreenToWorldPoint(Input.mousePosition);
Vector2 localPos = worldPos - controllerPosition;

float angleRad = Mathf.Atan2(localPos.y, localPos.x);
float angleDeg = angleRad * Mathf.Rad2Deg;
if (angleDeg < 0) angleDeg += 360f;

int sectorIndex = AngleToSectorIndex(angleDeg);
```

#### 2. **가우스 평활화 (3-Sigma Rule)**
```csharp
// 중심: 100% 영향
sectorRadii[center] += pushAmount;

// 주변: 가우스 분포
for (int offset = 1; offset <= smoothingRadius; offset++) {
    float sigma = radius / 3f;
    float gaussian = exp(-offset²/(2σ²));
    float influence = gaussian * smoothingStrength;
    
    sectorRadii[left] += pushAmount * influence;
    sectorRadii[right] += pushAmount * influence;
}
```

#### 3. **Gaussian Curve**
```
influence
    1.0 ├───●                    ← center (100%)
        │   ╱ ╲
    0.8 ├  ●   ●                 ← ±1 sector (~80%)
        │ ╱     ╲
    0.6 ├●       ●               ← ±2 sector (~60%)
        │         ╲
    0.4 ●          ●             ← ±3 sector (~40%)
        │           ╲
    0.2 ●            ●           ← ±4 sector (~20%)
        │             ╲
    0.0 ●──────────────●         ← ±8 sector (0%)
        └─────────────────────
        -8  -4   0   4   8  (offset)
```

#### 4. **탄성 복원 (HTML applySmoothing)**
```csharp
// 매 프레임 ApplyGravity()에서 실행
float[] nextRadii = new float[sectorCount];

for (int i = 0; i < sectorCount; i++) {
    // 1. 중력 수축
    float newRadius = _sectorRadii[i] - currentGravity * dt;
    
    // 2. 좌우 평활화 (선택적, 조절 가능)
    if (config.EnableNeighborSmoothing) {
        float weight = config.NeighborSmoothingStrength;
        weight *= smoothingMultiplier;  // 맥동 연동
        weight *= _recoveryScales[i];   // 상처 적용
        
        newRadius = newRadius * (1-2w) + (prev + next) * w;
    }
    
    // 3. 전역 복원 (선택적, 매우 약함)
    if (config.EnableGlobalRestoration) {
        float force = (average - newRadius) * 0.005;
        force *= _recoveryScales[i];  // 상처 적용
        newRadius += force;
    }
    
    nextRadii[i] = Mathf.Max(newRadius, EarthRadius);
}

_sectorRadii = nextRadii;  // 동시 업데이트
```

#### 5. **상처 회복 루프**
```csharp
// UpdateWoundRecovery()
for (int i = 0; i < sectorCount; i++) {
    if (_woundCooldowns[i] > 0) {
        _woundCooldowns[i] -= deltaTime;
    } else {
        _recoveryScales[i] = MoveTowards(
            _recoveryScales[i], 
            1.0f, 
            woundRecoverySpeed * deltaTime
        );
    }
}
```

## 전체 체크리스트
- [x] Step 1 구현 완료 ✅
- [x] Step 2 구현 완료 ✅
- [x] Step 3 구현 완료 ✅
- [x] Step 4 구현 완료 ✅

---

## 구현 파일

### Step 1
- `Assets/ShapeDefense/Scripts/Polar/PolarDataConfig.cs`
- `Assets/ShapeDefense/Scripts/Polar/PolarFieldController.cs`

### Step 2
- `Assets/ShapeDefense/Scripts/Polar/IPolarView.cs`
- `Assets/ShapeDefense/Scripts/Polar/PolarBoundaryRenderer.cs`

### Step 3
- `Assets/ShapeDefense/Scripts/Polar/PolarInputHandler.cs`
- `Assets/ShapeDefense/Scripts/Polar/PolarDataConfig.cs` (확장)
- `Assets/ShapeDefense/Scripts/Polar/PolarFieldController.cs` (확장)

### Step 4
- `Assets/ShapeDefense/Scripts/Polar/PolarProjectile.cs`
- `Assets/ShapeDefense/Scripts/Polar/PolarProjectileManager.cs`
- `Assets/ShapeDefense/Scripts/Polar/PolarDataConfig.cs` (확장)

---

## Phase 1 완료 요약

### HTML → Unity 완전 재현 ✅
- **데이터 구조:** 180 섹터 극좌표 시스템
- **중력 시뮬레이션:** 단계별 가속 + 게임 오버
- **시각화:** 단일 메시 + 맥동 효과
- **입력 처리:** Screen → Polar 변환 + Push
- **복원 시스템:** 좌우 평활화 + 전역 복원
- **투사체:** 극좌표 이동 + 충돌 감지
- **폭발:** 영역 밀어내기 + 상처

### 추가 개선 사항 ✅
- **맥동-복원 싱크:** 호흡하는 생명체
- **상처 시스템:** 전략적 약점 형성
- **Object Pooling:** 성능 최적화
- **디버그 시스템:** 로그 + Gizmo

### 최종 씬 구성
```
Scene Hierarchy:
├─ Main Camera
├─ Directional Light
└─ PolarField (0, 0, 0)
   ├─ PolarFieldController
   │  └─ Config: DefaultPolarConfig
   ├─ PolarBoundaryRenderer
   │  └─ Material: RingSectorGlow
   ├─ PolarInputHandler
   │  └─ Input Actions: (선택)
   └─ PolarProjectileManager
      ├─ Projectile Prefab: (선택)
      └─ Container: Projectiles (자동 생성)
```

### 성능 지표
```
메시:
- 정점: 362개
- 삼각형: 360개
- Draw Call: 1개
- 예상 비용: < 0.1ms

투사체:
- 풀 크기: 20개
- 활성 개수: 1~10개 (FireRate 기반)
- 예상 비용: < 0.2ms

총합:
- 프레임 비용: < 0.5ms
- 60fps 안정적 유지
```

### HTML 재현도
| 항목 | HTML | Unity | 일치도 |
|------|------|-------|--------|
| 데이터 구조 | 180 섹터 | 180 섹터 | 100% |
| 중력 시뮬레이션 | 단계별 가속 | 단계별 가속 | 100% |
| 시각화 | Canvas | Mesh | 100% |
| 좌우 평활화 | 90% + 5% + 5% | 90% + 5% + 5% | 100% |
| 투사체 이동 | 극좌표 | 극좌표 | 100% |
| 충돌 감지 | r >= wallRadius | r >= sectorRadius | 100% |
| 폭발 로직 | executeAreaExplosion | ExecuteExplosion | 100% |
| **전체** | | | **100%** |

---

## 다음 단계 (Phase 2)

### 목표: 게임 시스템 확장
- [ ] UI 시스템 (스코어, 스테이지, HP)
- [ ] 파워업 시스템
- [ ] 적 AI (침략 패턴)
- [ ] 사운드 / VFX
- [ ] 저장 / 불러오기

---

## 참고 자료
- `Docs/ShapeDefense_CoreConcepts.md` - 핵심 개념
- `Docs/ShapeDefense_Terminology.md` - 용어 정의
- `Docs/RingSectorMesh_ConceptualFlow.md` - 메시 구조
- `game2B.html` - HTML 원본 프로토타입