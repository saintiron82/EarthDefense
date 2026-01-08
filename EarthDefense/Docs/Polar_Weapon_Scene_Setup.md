# Polar 무기 발사를 위한 씬 세팅 가이드

Polar 무기 시스템을 Unity 씬에서 설정하는 전체 절차를 안내합니다.

---

## 📋 사전 준비물

### 1. **필수 컴포넌트**
- ✅ PolarFieldController (ShapeDefense)
- ✅ PolarFieldAdapter
- ✅ PlayerWeaponManager
- ✅ PolarWeaponInputHandler (WeaponSystem 기반)
- ✅ 무기 데이터 (ScriptableObject)

### 2. **필수 서비스**
- ✅ ResourceService (SGSystem)
- ✅ PoolService (SGSystem)

---

## 🎯 씬 설정 절차

### **Step 1: 시스템 서비스 설정**

```
Hierarchy:
SystemServices (GameObject)
├─ ResourceService (Component)
│  └─ [Inspector] Bundles: [리소스 번들 할당]
└─ PoolService (Component)
   └─ [Inspector] ResourceService: [ResourceService 참조]
```

**설정 방법:**
1. Hierarchy 우클릭 → Create Empty → "SystemServices"
2. Add Component → ResourceService
3. Add Component → PoolService
4. PoolService Inspector:
   - Resource Service: ResourceService 드래그

---

### **Step 2: Polar Field 설정**

```
Hierarchy:
PolarField (GameObject)
├─ PolarFieldController (Component) ⭐
├─ PolarFieldAdapter (Component) ⭐
└─ PolarBoundaryRenderer (Component) - 선택 사항
```

**설정 방법:**
1. Hierarchy 우클릭 → Create Empty → "PolarField"
2. Add Component → PolarFieldController
3. Add Component → PolarFieldAdapter
4. (선택) Add Component → PolarBoundaryRenderer (시각화)

**PolarFieldController 설정:**
```
[Config]
- Initial Radius: 10
- Sector Count: 180
- Base Resistance: 100
- Enable Wound System: true
```

**PolarFieldAdapter 설정:**
```
[Controller]
- Controller: PolarFieldController (자동 할당됨)
```

---

### **Step 3: Player 무기 계층 설정**

```
Hierarchy:
Player (GameObject)
├─ PolarWeaponInputHandler (Component) ⭐ (WeaponSystem 기반)
└─ WeaponSlot (GameObject)
   └─ PlayerWeaponManager (Component) ⭐
```

**설정 방법:**

#### **3-1. Player 생성**
1. Hierarchy 우클릭 → Create Empty → "Player"
2. Position: (0, 0, 0)

#### **3-2. WeaponSlot 생성**
1. Player 자식 → Create Empty → "WeaponSlot"
2. Add Component → PlayerWeaponManager

**PlayerWeaponManager Inspector:**
```
[Field]
- PolarFieldBehaviour: PolarField의 PolarFieldAdapter 드래그

[Weapon]
- WeaponSlot: WeaponSlot Transform 드래그
- DefaultWeaponData: LaserWeaponData.asset 할당
- DataTable: PolarWeaponDataTable.asset 할당
- DefaultWeaponId: "weapon_laser_drill"

[Aim Settings]
- EnableMouseAim: true (마우스 조준 활성화) ⭐
```

#### **3-3. 입력 핸들러 추가 (WeaponSystem 기반)**
1. Player에 Add Component → PolarWeaponInputHandler

**PolarWeaponInputHandler Inspector:**
```
[References]
- WeaponManager: PlayerWeaponManager 드래그 (자동)
- AimCamera: Main Camera (자동)

[Input]
- InputActions: (비워둠 - Fallback 사용)
- ActionMapName: "Player"
- AttackActionName: "Attack"

[Settings]
- HoldToFire: true (홀드 모드)

[Debug]
- EnableDebugLogs: false
```

---

## 🔄 전체 계층 구조 (최종)

```
Scene Hierarchy:

SystemServices
├─ ResourceService
└─ PoolService

PolarField
├─ PolarFieldController
├─ PolarFieldAdapter
└─ PolarBoundaryRenderer (선택)

Player
├─ PolarWeaponInputHandler ⭐ (WeaponSystem 기반)
└─ WeaponSlot
   └─ PlayerWeaponManager
      └─ (무기 동적 생성됨)
         └─ WeaponArm
            └─ Muzzle

Main Camera
```

---

## 🎮 조작 방법

### **WeaponSystem 기반** ✅
- **좌클릭 (홀드)**: 무기 발사/홀드
- **마우스 이동**: 조준 (PlayerWeaponManager.enableMouseAim = true)
- **Tab**: 무기 교체 (추후 구현)

### **입력 소스**
- **PlayerInputActionsRuntime** (Fallback 자동 생성)
- **Player/Attack** 액션: 좌클릭, Enter, Gamepad 버튼
- **Player/Look** 액션: 마우스 위치

---

## 📦 ScriptableObject 데이터 생성

### **Step 4: 무기 데이터 생성**

#### **레이저 데이터**
1. Project 창 → `Assets/Resources/Polar/Weapons/` 폴더 생성
2. 우클릭 → Create → EarthDefense → Polar → Weapon Data → **Laser**
3. 파일명: `LaserWeaponData.asset`

**Inspector 설정:**
```
[ID & UI]
- Id: "weapon_laser_drill"
- WeaponName: "Drill Laser"
- Icon: (스프라이트)

[Bundles]
- WeaponBundleId: "" (비워둠 - 동적 생성)
- ProjectileBundleId: "Projectiles/LaserBeam"

[Combat]
- Damage: 5
- KnockbackPower: 0.1
- AreaType: Fixed
- DamageRadius: 1

[Beam]
- TickRate: 10

[Laser Specific]
- ExtendSpeed: 50
- RetractSpeed: 70
- MaxLength: 50
- BeamWidth: 0.1
- BeamColor: Cyan
```

#### **무기 데이터 테이블**
1. 같은 폴더에서 우클릭 → Create → EarthDefense → Polar → **Weapon Data Table**
2. 파일명: `PolarWeaponDataTable.asset`

**Inspector:**
```
[Weapons]
- Size: 1
  - Element 0: LaserWeaponData
```

3. 우클릭 → Validate Data → 콘솔 확인

---

## ✅ 테스트 체크리스트

### **씬 설정 확인**
- [ ] SystemServices 존재 (ResourceService, PoolService)
- [ ] PolarField 존재 (Controller, Adapter)
- [ ] Player 존재 (WeaponManager, PolarWeaponInputHandler)
- [ ] PlayerWeaponManager.polarFieldBehaviour = PolarFieldAdapter
- [ ] PlayerWeaponManager.enableMouseAim = true ⭐
- [ ] PolarWeaponInputHandler.weaponManager = PlayerWeaponManager

### **데이터 확인**
- [ ] LaserWeaponData.asset 존재
- [ ] LaserWeaponData.ProjectileBundleId = "Projectiles/LaserBeam"
- [ ] PolarWeaponDataTable.Weapons[0] = LaserWeaponData
- [ ] Validate Data 성공

### **런타임 테스트**
- [ ] Play 모드 진입 성공
- [ ] 좌클릭 홀드 → 레이저 발사
- [ ] 마우스 이동 → 조준 변경
- [ ] 레이저 빔 시각화 확인
- [ ] PolarField 섹터에 피해 적용 확인
- [ ] 좌클릭 떼기 → 레이저 리트랙트

---

## 🚨 문제 해결

### **문제 1: "PoolService not available!"**

**원인:** PoolService 초기화 안 됨

**해결:**
1. SystemServices GameObject 확인
2. PoolService 컴포넌트 존재 확인
3. PoolService.ResourceService 참조 확인

---

### **문제 2: "polarFieldBehaviour는 IPolarField를 구현해야 합니다"**

**원인:** PolarFieldAdapter 연결 안 됨

**해결:**
1. PolarField에 PolarFieldAdapter 컴포넌트 추가
2. PlayerWeaponManager Inspector:
   - PolarFieldBehaviour: PolarFieldAdapter 드래그

---

### **문제 3: 레이저가 발사되지 않음**

**원인:** 입력 핸들러 미연결

**해결:**
1. Player에 PolarWeaponInputHandler 컴포넌트 확인
2. PolarWeaponInputHandler.weaponManager 참조 확인
3. 좌클릭 입력 테스트

---

### **문제 4: 조준이 안 됨**

**원인:** PlayerWeaponManager.enableMouseAim = false

**해결:**
1. PlayerWeaponManager Inspector
2. EnableMouseAim = true ⭐

---

## 🎯 빠른 시작 (5분 설정)

1. **SystemServices 생성**
   - Empty GameObject → Add ResourceService, PoolService

2. **PolarField 생성**
   - Empty GameObject → Add PolarFieldController, PolarFieldAdapter

3. **Player 생성**
   - Empty GameObject → Add PolarWeaponInputHandler
   - 자식 WeaponSlot → Add PlayerWeaponManager
   - PlayerWeaponManager:
     - PolarFieldBehaviour: PolarFieldAdapter
     - EnableMouseAim: true ⭐
     - DefaultWeaponId: "weapon_laser_drill"

4. **LaserWeaponData 생성**
   - Create → Polar → Weapon Data → Laser
   - ProjectileBundleId: "Projectiles/LaserBeam"

5. **Play!**
   - 좌클릭 홀드로 레이저 발사
   - 마우스로 조준

---

## 🎮 WeaponSystem 통합 패턴

### **ShapeDefense 패턴 (참고)**

```
Player
└─ WeaponController
   ├─ PlayerInputActionsRuntime (Fallback)
   ├─ Attack 액션
   └─ WeaponSlot
      └─ BaseWeapon (프리팹)
         └─ WeaponArm → Muzzle
```

### **Polar 패턴 (적용)**

```
Player
└─ PolarWeaponInputHandler ⭐
   ├─ PlayerInputActionsRuntime (Fallback)
   ├─ Attack 액션
   └─ WeaponSlot
      └─ PlayerWeaponManager
         └─ PolarWeaponBase (동적 생성)
            └─ PolarWeaponArm → Muzzle
```

**차이점:**
| 항목 | ShapeDefense | Polar |
|------|-------------|-------|
| **무기 관리** | WeaponController | PlayerWeaponManager |
| **무기 베이스** | BaseWeapon | PolarWeaponBase |
| **프리팹 로딩** | ResourceService (필수) | ResourceService (선택) |
| **동적 생성** | 없음 | 타입별 자동 생성 ⭐ |
| **필드 연동** | 없음 | IPolarField 주입 ⭐ |

---

## 📚 참고 문서

- **무기 프리셋**: `Docs/Phase2_WeaponPresets_Guide.md`
- **Arm 가이드**: `Docs/Polar_Weapon_Arm_Guide.md`
- **데이터 흐름**: `Docs/Polar_Weapon_DataFlow.md`
- **SGSystem 통합**: `Docs/Polar_SGSystem_Integration.md`
- **Phase 2 문서**: `Docs/phase2.md`
- **WeaponController**: `Assets/ShapeDefense/Scripts/WeaponController.cs`

---

이제 씬 설정이 완료되었습니다! 

**테스트:**
- **좌클릭 홀드**: 레이저 발사 (WeaponSystem 통합)
- **마우스 이동**: 조준 변경

모든 공격이 WeaponSystem 기반으로 통합되었습니다! 🚀
