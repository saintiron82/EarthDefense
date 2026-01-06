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

### 사용 방법

1. **PolarField에 컴포넌트 추가:**
   ```
   PolarField GameObject:
     - PolarFieldController
     - PolarBoundaryRenderer
     - PolarInputHandler  ← 추가!
   ```

2. **Inspector 설정:**
   ```
   Polar Input Handler:
     ├─ References
     │  ├─ Controller: (자동 연결)
     │  └─ Main Camera: (자동 연결)
     ├─ Input Settings
     │  ├─ Push Key: Mouse0
     │  └─ Require Key Hold: false
     └─ Debug
        ├─ Show Debug Rays: ✓
        └─ Debug Ray Color: Green
   ```

3. **Config 설정:**
   ```
   DefaultPolarConfig:
     Push & Carving:
       ├─ Push Power: 2.0
       ├─ Smoothing Radius: 8
       └─ Smoothing Strength: 0.8
   ```

4. **Play & Test:**
   ```
   마우스 클릭 → 해당 방향으로 장막 밀려남
   드래그 → 연속 밀어내기
   ```

### 수락 기준
- [x] 마우스 클릭(또는 드래그) 시 해당 지점의 메시가 즉각적으로 바깥쪽으로 팽창하는가? ✅
- [x] 평활화 로직을 통해 톱니 모양이 아닌 부드러운 곡선으로 팽창하는가? ✅
- [x] 극좌표 변환이 정확한가? (Gizmo로 확인) ✅

### 디버그 기능
- **녹색 선:** 중심 → 마우스 방향 (현재 반지름)
- **노란 구:** 마우스 위치
- **청록 선:** 중심 → 마우스

---

# Step 4: Projectile & Collision (Action)

### 목표
- 자동 발사 투사체와 수학적 충돌 판정을 통해 게임성을 구현합니다.

### 사전 정의
- `BULLET_SPEED` (투사체 속도)
- `MISSILE_FORCE` (미사일 폭발 힘)
- `COLLISION_EPSILON` (충돌 감지 정밀도 오차)

### Todo
- [ ] `PolarProjectile` 클래스 이식 (HTML `bullets` 배열 대응)
- [ ] `CheckCollision` 로직 구현 (Radial Distance 비교)
- [ ] `ExecuteAreaExplosion` (미사일 폭발) 로직 이식

### 구현 명세
- 투사체의 `r` 값이 현재 각도의 `sectorRadii[idx]`보다 크거나 같아지는 시점을 충돌로 판별합니다.
- 충돌 시 `ExecuteAreaExplosion`과 동일한 방식으로 주변 섹터의 반지름을 데이터 기반으로 수정합니다.

### 수락 기준
- 발사된 총알이 경계선 메시에 닿았을 때 정확히 소멸하며 메시를 밀어내는가?
- 미사일 폭발 시 일정 범위의 섹터들이 동시에 팽창하는가?

---

## 전체 체크리스트
- [x] Step 1 구현 완료 ✅
- [x] Step 2 구현 완료 ✅
- [x] Step 3 구현 완료 ✅
- [ ] Step 4 구현 완료

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