# Y축 오프셋 문제 해결 가이드

**문제:** Polar 필드와 플레이어가 모두 화면에서 Y축으로 1 유닛 정도 틀어져 보임

---

## 🎯 즉시 확인 사항

### 1. Main Camera 위치

**Unity Hierarchy:**
1. "Main Camera" 선택
2. Inspector → Transform → Position 확인

**올바른 설정 (2D 게임):**
```
Position: X=0, Y=0, Z=-10
```

**문제가 있는 경우:**
```
Position: X=0, Y=1, Z=-10   ← Y가 0이 아님!
또는
Position: X=0, Y=-1, Z=-10  ← Y가 0이 아님!
```

**수정:** Y를 **0**으로 변경

---

### 2. PolarFieldController 위치

**Unity Hierarchy:**
1. "PolarFieldController" (또는 필드 오브젝트) 선택
2. Inspector → Transform → Position 확인

**올바른 설정:**
```
Position: X=0, Y=0, Z=0
```

**문제가 있는 경우:**
```
Position: X=0, Y=1, Z=0   ← Y가 0이 아님!
```

**수정:** Y를 **0**으로 변경

---

### 3. PlayerCore 위치

**Unity Hierarchy:**
1. "Player" 또는 "PlayerCore" 오브젝트 선택
2. Inspector → Transform → Position 확인

**올바른 설정:**
```
Position: X=0, Y=0, Z=0
```

**문제가 있는 경우:**
```
Position: X=0, Y=1, Z=0   ← Y가 0이 아님!
```

**수정:** Y를 **0**으로 변경

---

## 🔍 Console 로그 확인

게임 실행 시 Console 창에서 다음 로그 확인:

```
[PolarFieldController] Transform Position: (0.00, ?, 0.00)
[PolarFieldController] Main Camera Position: (0.00, ?, -10.00)
[PlayerCore] Position: (0.00, ?, 0.00)
```

**Y 값이 모두 0.00이어야 합니다!**

---

## 📊 가능한 원인

### 1. Canvas 위치

만약 UI Canvas를 사용한다면:

**Hierarchy:**
1. "Canvas" 선택
2. Inspector 확인

**Screen Space - Overlay 모드:**
- Position은 자동으로 (0, 0, 0)
- 문제 없음

**Screen Space - Camera 또는 World Space 모드:**
- Position 확인 필요
- Y를 0으로 설정

### 2. Parent-Child 관계

```
GameManager (Y=1)
  └─ PolarFieldController (Local Y=0)
     → World Position: Y=1 ← 문제!
```

**확인 방법:**
- Hierarchy에서 오브젝트 선택
- Inspector에서 Transform 확인
  - **Position:** Local Position
  - World Position은 Scene 뷰 우측 상단에 표시

**해결:**
- PolarFieldController를 Root로 이동
- 또는 Parent의 Y도 0으로 설정

### 3. Prefab 기본값

**원인:**
- Prefab의 기본 Position이 Y=1 또는 Y=-1
- 씬에 배치할 때마다 이 값으로 생성됨

**해결:**
1. Prefab 에셋 직접 수정
2. Prefab Mode로 진입 (Hierarchy에서 우클릭 → Open Prefab)
3. Transform → Position Y를 0으로
4. 저장 (Ctrl+S)

---

## ✅ 빠른 수정 체크리스트

### 모든 핵심 오브젝트의 Y Position을 0으로:

- [ ] **Main Camera**: Y = 0 (Z는 -10 유지)
- [ ] **PolarFieldController**: Y = 0
- [ ] **PlayerCore**: Y = 0
- [ ] **SectorManager** (있다면): Y = 0
- [ ] **Canvas** (World Space 사용 시): Y = 0

---

## 🎬 수정 후 결과

**Before:**
```
게임 화면이 위 또는 아래로 치우침
카메라가 중심을 못 봄
```

**After:**
```
게임이 화면 정중앙에 표시됨
모든 오브젝트가 정확한 위치에 보임
```

---

## 🔧 고급 디버깅

### Scene Gizmo 확인

**Scene 뷰에서:**
1. PolarFieldController 선택
2. Gizmo (빨간 원, 청록 원) 확인
3. 원들이 Scene 뷰 중앙에 있는지 확인

**Game 뷰와 Scene 뷰 비교:**
- Scene 뷰: 원이 중앙 → 정상
- Game 뷰: 원이 치우침 → 카메라 문제!

### 카메라 설정 추가 확인

**Main Camera:**
```
Projection: Orthographic
Orthographic Size: 10 (또는 적절한 값)
Clipping Planes:
  Near: 0.3
  Far: 1000
```

---

## 💡 왜 Y=0이어야 하나?

### 2D 게임의 표준 좌표계

```
        ↑ Y (위)
        |
        |
−−−−−−−−+−−−−−−−−→ X (오른쪽)
        |
        | (0, 0) = 화면 중심
        |
```

### 카메라 위치

```
X = 0  : 좌우 중앙
Y = 0  : 상하 중앙
Z = -10: 카메라는 뒤에서 앞을 봄 (2D)
```

---

## 🎯 결론

**단 하나의 규칙:**

> 모든 게임 오브젝트와 카메라의 Y Position을 0으로!

**예외:**
- 의도적으로 위아래로 움직이는 오브젝트
- UI 요소 (Canvas가 자동 처리)

---

**이제 Unity에서 위 항목들을 확인하고 Y Position을 0으로 설정하면 문제가 해결됩니다!**

