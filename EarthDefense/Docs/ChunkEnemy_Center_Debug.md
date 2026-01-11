# 극좌표 시스템 중심 이탈 문제 해결 가이드

**작성일:** 2026-01-11  
**시스템:** PolarFieldController (극좌표 필드 시스템)  
**문제:** 오브젝트들이 화면 중앙이 아닌 왼쪽에 위치함

---

## 🎯 핵심 원인

**PolarFieldController의 Transform 위치가 화면 중앙이 아닙니다!**

### 극좌표 시스템 구조

```
PolarFieldController.transform.position = (x, y, z)
  ↓ 이 위치가 극좌표 시스템의 중심!
  
모든 극좌표 오브젝트:
  - PolarProjectile (투사체)
  - ChunkEnemy (적 - Sector 시스템 사용 시)
  - PolarBoundaryRenderer (경계선)
  
→ 모두 PolarFieldController.transform.position을 중심으로 계산됨
```

### 코드 예시

```csharp
// PolarProjectile.cs - UpdatePosition()
float angleRad = angle * Mathf.Deg2Rad;
Vector3 polarPos = new Vector3(
    Mathf.Cos(angleRad) * radius,
    Mathf.Sin(angleRad) * radius,
    0f
);

// ← 여기가 핵심!
transform.position = _fieldController.transform.position + polarPos;
```

**만약 `_fieldController.transform.position`이 (-5, 0, 0)이라면,**
**모든 오브젝트가 화면 왼쪽에 생성됩니다!**

---

## ✅ 해결 방법

### 1. PolarFieldController 위치 확인 및 수정

**Unity Inspector에서:**

1. **Hierarchy에서 PolarFieldController 찾기**
   - "PolarField" 또는 "FieldController" 이름의 GameObject
   - PolarFieldController 컴포넌트가 붙어있음

2. **Transform 확인**
   ```
   Transform
     Position: X=0, Y=0, Z=0  ← 반드시 (0,0,0)이어야 함!
     Rotation: X=0, Y=0, Z=0
     Scale: X=1, Y=1, Z=1
   ```

3. **Position을 (0, 0, 0)으로 변경**

---

## 🔍 디버그 확인 방법

### Scene Gizmo 확인

**PolarFieldController에 Gizmo가 있음:**
- 빨간 원: Earth Radius (중심)
- 청록 원들: 각 섹터의 반지름

**정상:** 빨간 원이 화면 정중앙에 있어야 함  
**문제:** 빨간 원이 왼쪽이나 다른 곳에 있음

### Console 로그 확인

게임 실행 시:
```
[PolarFieldController] Initialized with 180 sectors, InitialRadius=5, EarthRadius=0.5
```

이 로그가 나오면 정상 작동 중입니다.

---

## 🐛 추가 체크 사항

### 1. Sector 시스템과 함께 사용하는 경우

**SectorManager도 확인:**
```
SectorManager.center (Transform)
  ↓
Sector.center (Transform)  
  ↓
ChunkEnemy.center (Transform)
```

**SectorManager의 center 필드:**
- PolarFieldController와 **같은 오브젝트**를 가리켜야 함
- 또는 동일한 위치 (0, 0, 0)에 있어야 함

### 2. 카메라 위치

**Main Camera 확인:**
```
Camera Position: 
  X = 0 (화면 좌우 중앙)
  Y = 0 (화면 상하 중앙)
  Z = -10 (2D 게임의 일반적인 거리)
```

카메라가 중앙을 보고 있어야 PolarFieldController가 화면 중앙에 보입니다.

---

## 📊 시각적 확인

### 이미지 분석

```
┌─────────────────────────────────┐
│                                 │
│    ⊕ ← 왼쪽에 있는 십자선       │  ← 문제!
│                                 │
│         ◯                       │  ← PolarFieldController가
│      (큰 원)                    │     여기에 있어야 함
│                                 │
└─────────────────────────────────┘
```

**왼쪽 노란 십자선:**
- PolarProjectile (투사체) 또는
- ChunkEnemy (적) 또는  
- Unity Editor의 선택된 오브젝트 표시

**큰 원(오렌지):**
- PolarBoundaryRenderer가 그리는 경계선

**문제:** 십자선이 큰 원의 중심이 아닌 왼쪽에 있음
**원인:** PolarFieldController의 Position이 화면 중앙이 아님

---

## 🎯 단계별 해결

### Step 1: PolarFieldController 찾기

**Hierarchy 검색:**
```
검색창에 "Polar" 입력
→ "PolarFieldController" 또는 "FieldController" 찾기
```

### Step 2: Transform 확인

**Inspector:**
```
PolarFieldController
  Transform
    Position
      X: ?  ← 이게 0이 아니면 문제!
      Y: ?  ← 이게 0이 아니면 문제!
      Z: 0
```

### Step 3: 위치 수정

**Position을 (0, 0, 0)으로 변경**

### Step 4: 게임 실행

- Scene 뷰에서 Gizmo 확인
- 빨간 원이 화면 중앙에 있는지
- 투사체/적이 중앙 원 안에 있는지

---

## 💡 왜 이런 문제가?

### 일반적인 원인

1. **씬 편집 중 실수로 이동**
   - 오브젝트 선택 후 드래그
   - Transform 값 직접 수정

2. **Prefab 기본값**
   - PolarFieldController Prefab의 기본 Position
   - 재사용 시 위치가 유지됨

3. **Parent-Child 관계**
   ```csharp
   GameManager (Position: -5, 0, 0)
     └─ PolarFieldController (Local: 0, 0, 0)
        → World Position: (-5, 0, 0)  ← 문제!
   ```
   
   **해결:**
   - PolarFieldController를 Root로 이동
   - 또는 Parent의 Position을 (0, 0, 0)으로

---

## 🧪 테스트

### 정상 동작 확인

**Scene Gizmo:**
- ✅ 빨간 원(Earth Radius)이 화면 정중앙
- ✅ 청록 원들(섹터)이 빨간 원 주변
- ✅ 투사체/적이 원 안에 생성

**Game View:**
- ✅ 큰 원이 화면 중앙
- ✅ 모든 오브젝트가 원 안에서 동작
- ✅ 극좌표 회전이 중심 기준

---

## 📝 체크리스트

### 확인 사항

- [ ] PolarFieldController 오브젝트를 찾았다
- [ ] Transform.Position이 (0, 0, 0)이다
- [ ] Parent가 없거나 Parent도 (0, 0, 0)이다
- [ ] Scene Gizmo에서 빨간 원이 중앙에 있다
- [ ] SectorManager.center도 올바르게 설정되었다 (사용 시)
- [ ] Camera가 (0, 0, -10) 위치에서 중앙을 본다
- [ ] 게임 실행 시 모든 오브젝트가 중앙에 생성된다

---

## 🎬 예상 결과

**수정 전:**
```
PolarFieldController Position: (-5, 0, 0)
→ 모든 오브젝트가 화면 왼쪽에 생성
→ 큰 원도 왼쪽으로 치우침
```

**수정 후:**
```
PolarFieldController Position: (0, 0, 0)
→ 모든 오브젝트가 화면 중앙에 생성
→ 큰 원이 화면 중앙
→ 극좌표 시스템이 정상 작동
```

---

## 🔧 추가 디버깅 (필요시)

### PolarFieldController 위치 로그 추가

```csharp
// PolarFieldController.cs - Start()에 추가
Debug.Log($"[PolarFieldController] Position: {transform.position}");

// 실행 시 Console 확인:
// [PolarFieldController] Position: (0.00, 0.00, 0.00)  ← 정상
// [PolarFieldController] Position: (-5.00, 0.00, 0.00)  ← 문제!
```

### Gizmo 추가 (이미 있음)

```csharp
// PolarFieldController.cs - OnDrawGizmos()
// 이미 구현되어 있음:
// - 빨간 원: Earth Radius
// - 청록 원: 각 섹터
```

---

**PolarFieldController의 Position을 (0, 0, 0)으로 설정하면 모든 문제가 해결됩니다!**

---

## 🔍 구조 분석

### 중심(Center) 전달 흐름

```
SectorManager.center (Transform)
  ↓ Initialize()
Sector.center (Transform)
  ↓ Spawn() → Configure()
ChunkEnemy.center (Transform)
  ↓ transform.position = center.position
ChunkEnemy가 중심에 위치
```

### 핵심 코드

```csharp
// SectorManager.cs
public void RebuildSectors()
{
    for (int i = 0; i < sectorCount; i++)
    {
        var sector = Instantiate(sectorPrefab, transform);
        sector.Initialize(center, player, i, startDeg, arc);  // ← center 전달
    }
}

// Sector.cs
public void Initialize(Transform centerTransform, ...)
{
    center = centerTransform;  // ← center 저장
}

private void Spawn()
{
    enemy.Configure(center, ...);  // ← center 전달
}

// ChunkEnemy.cs
public void Configure(Transform centerTransform, ...)
{
    center = centerTransform;
    transform.position = center.position;  // ← 중심에 위치
}
```

---

## 🐛 가능한 원인

### 1. SectorManager의 center가 잘못 설정됨

**확인 방법:**
1. Unity Hierarchy에서 `SectorManager` 선택
2. Inspector에서 `Center` 필드 확인
3. 이 필드가 **게임 월드의 중심 오브젝트**를 가리키는지 확인

**올바른 설정:**
- 빈 GameObject를 (0, 0, 0) 위치에 생성
- 이름: "FieldCenter" 또는 "GameCenter"
- SectorManager의 Center 필드에 이 오브젝트 할당

### 2. Center Transform이 (0, 0, 0)이 아님

**확인 방법:**
1. Hierarchy에서 Center 오브젝트 선택
2. Inspector의 Transform 확인
3. Position이 (0, 0, 0)인지 확인

**수정:**
```
Transform
  Position: X=0, Y=0, Z=0
  Rotation: X=0, Y=0, Z=0
  Scale: X=1, Y=1, Z=1
```

### 3. Sector 자체가 잘못된 위치에 생성

**확인 방법:**
```csharp
// SectorManager.RebuildSectors()
var sector = Instantiate(sectorPrefab, transform);  // ← transform은 SectorManager의 위치
```

만약 SectorManager가 (0, 0, 0)이 아닌 곳에 있다면, Sector들도 그 위치에 생성됩니다.

**수정:**
- SectorManager의 Position을 (0, 0, 0)으로 설정
- 또는 `Instantiate(sectorPrefab, Vector3.zero, Quaternion.identity);` 사용

---

## ✅ 추가된 디버깅 도구

### 1. Console 로그

```csharp
// ChunkEnemy.Configure()에 추가됨
Debug.Log($"[ChunkEnemy] Configured at Sector {sectorIndex}: center position = {center.position}");
Debug.Log($"[ChunkEnemy] transform.position set to {transform.position}");
```

**실행 시 확인:**
```
[ChunkEnemy] Configured at Sector 0: center position = (-5, 0, 0)  ← 문제!
[ChunkEnemy] transform.position set to (-5, 0, 0)

올바른 경우:
[ChunkEnemy] Configured at Sector 0: center position = (0, 0, 0)  ← 정상
[ChunkEnemy] transform.position set to (0, 0, 0)
```

### 2. Scene Gizmo 시각화

**추가된 Gizmo (Editor 전용):**
```csharp
private void OnDrawGizmos()
{
    // 빨간 구체: 중심(center) 위치
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(center.position, 0.3f);

    // 노란 구체: 청크 위치
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, 0.2f);

    // 청록 선: 중심에서 청크까지
    Gizmos.color = Color.cyan;
    Gizmos.DrawLine(center.position, transform.position);
}
```

**Scene 뷰에서 확인:**
- 빨간 구체(center)가 화면 중앙에 있어야 함
- 노란 구체(chunk)가 빨간 구체와 겹쳐야 함 (같은 위치)
- 만약 떨어져 있다면 문제!

---

## 🎯 해결 단계

### 1단계: Console 로그 확인

게임 실행 → Console 창 확인:
```
[ChunkEnemy] Configured at Sector 0: center position = (?, ?, ?)
```

**Position이 (0, 0, 0)이 아니면 문제!**

### 2단계: Scene Gizmo 확인

Scene 뷰에서:
- 빨간 구체(center) 위치 확인
- 노란 구체(chunk) 위치 확인
- 두 구체가 떨어져 있으면 문제!

### 3단계: Inspector 설정 확인

**SectorManager:**
```
[ ] Center: FieldCenter (Transform)  ← 이 오브젝트가 (0,0,0)에 있는지
[ ] Player: PlayerCore (PlayerCore)
```

**FieldCenter (또는 Center 오브젝트):**
```
Transform
  Position: X=0, Y=0, Z=0  ← 반드시 (0,0,0)
```

### 4단계: 수정

**방법 1: Center 오브젝트 위치 수정**
1. Hierarchy에서 Center 오브젝트 선택
2. Inspector → Transform → Position을 (0, 0, 0)으로 변경

**방법 2: 새 Center 생성**
1. Hierarchy 우클릭 → Create Empty
2. 이름: "FieldCenter"
3. Position: (0, 0, 0)
4. SectorManager의 Center 필드에 할당

---

## 🧪 테스트

### 정상 동작 확인

**Console 로그:**
```
[ChunkEnemy] Configured at Sector 0: center position = (0, 0, 0)  ✅
[ChunkEnemy] transform.position set to (0, 0, 0)  ✅
```

**Scene Gizmo:**
- 빨간 구체(center)가 화면 정중앙 ✅
- 노란 구체(chunk)가 빨간 구체와 겹침 ✅
- 청록 선의 길이가 거의 0 ✅

**Game View:**
- 적이 화면 중앙 원 안에 생성됨 ✅
- 적이 중심을 향해 이동함 ✅

---

## 📊 디버그 체크리스트

- [ ] Console에서 center position 확인
- [ ] Scene Gizmo에서 빨간 구체 위치 확인
- [ ] SectorManager.Center 필드 할당 확인
- [ ] Center 오브젝트의 Position 확인
- [ ] SectorManager의 Position 확인
- [ ] 게임 실행 시 적이 중앙에 생성되는지 확인

---

## 💡 추가 정보

### 왜 center를 Transform으로 전달?

```csharp
// center를 Vector3가 아닌 Transform으로 전달하는 이유:
// 1. 중심이 이동할 수 있음 (예: 카메라 추적)
// 2. 참조로 전달되어 실시간 업데이트 가능
// 3. GameObject 계층 구조 유지
```

### RingSectorMesh의 로컬 좌표

```csharp
// RingSectorMesh는 로컬 좌표로 메시 생성
// ChunkEnemy의 transform.position이 중심이면
// 메시는 그 중심을 기준으로 그려짐

// 예:
// transform.position = (0, 0, 0)
// InnerRadius = 10, OuterRadius = 11
// → 반지름 10~11인 원형 메시가 (0,0,0) 중심으로 그려짐
```

---

## 🎬 예상 결과

**수정 전:**
```
적이 화면 왼쪽에 생성됨 (center가 잘못 설정)
```

**수정 후:**
```
적이 화면 중앙 원 안에 생성됨
적이 중심을 향해 이동함
RingSectorMesh가 중앙을 중심으로 그려짐
```

---

**이제 Scene 뷰에서 Gizmo를 확인하고, Console 로그를 보면 문제를 쉽게 찾을 수 있습니다!**

