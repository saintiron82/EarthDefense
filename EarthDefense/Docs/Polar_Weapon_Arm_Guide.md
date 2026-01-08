# Polar 무기 시스템 Arm 방식 가이드

## 📊 개요

Polar 무기 시스템은 **PolarWeaponArm**을 통한 **Arm 방식**을 기본으로 채택합니다.
- **발사 위치**: Muzzle Transform (Arm의 자식)
- **조준 방식**: 선택 사항 (PolarAngle/MouseFollow/Fixed 등)
- **ShapeDefense 호환**: 동일한 Arm 계층 구조

---

## 🏗️ 계층 구조

### **표준 계층**

```
Player
└─ WeaponSlot (Transform)
   └─ Weapon_LaserDrill (GameObject)
      ├─ PolarLaserWeapon (Component) ⭐
      └─ PolarWeaponArm (Transform 노드) ⭐
         └─ Muzzle (Transform) ⭐
```

### **최소 계층 (Arm 없이)**

```
Player
└─ WeaponSlot
   └─ Weapon_LaserDrill
      ├─ PolarLaserWeapon
      └─ (Arm 없음 - transform.position 사용)
```

---

## 🎯 PolarWeaponArm 동작 모드

### **PolarArmBehaviorType 열거형**

| 모드 | 설명 | 사용 사례 |
|------|------|-----------|
| **PolarAngle** | 극좌표 각도 직접 지정 ⭐ | 플레이어가 각도 입력 |
| **MouseFollow** | 마우스 위치 추적 ⭐ | ShapeDefense 호환 |
| **FollowParent** | 부모 Transform 따라감 | AimPivot 사용 시 |
| **Fixed** | 고정 각도 유지 | 고정 포탑 |
| **Rotate** | 자동 회전 | 회전 포탑 |
| **Independent** | 독립 조준 | 추후 구현 |

---

## 📝 Unity Editor 설정

### 1. **PolarWeaponArm GameObject 생성**

```
1. Hierarchy에서 Weapon GameObject 생성
2. 자식으로 Empty GameObject 생성 → "WeaponArm"으로 이름 변경
3. WeaponArm의 자식으로 Empty GameObject 생성 → "Muzzle"로 이름 변경
4. WeaponArm에 PolarWeaponArm 컴포넌트 추가
```

### 2. **PolarWeaponArm Inspector 설정**

```
[Arm Settings]
- BehaviorType: PolarAngle (극좌표 각도) 또는 MouseFollow (마우스)
- FixedAngle: 0 (Fixed 모드용)
- RotationSpeed: 90 (Rotate 모드용)

[References]
- Muzzle: (자동 할당됨 - Reset 버튼)
```

### 3. **PolarWeapon 컴포넌트 추가**

```
1. Weapon GameObject에 PolarLaserWeapon (또는 다른 무기) 추가
2. Inspector에서 WeaponArm 필드 할당 (자동 할당됨 - Reset)
```

---

## 🔧 코드 예시

### **1. 각도로 조준 (PolarAngle 모드)**

```csharp
// PlayerWeaponManager 사용
playerWeaponManager.UpdateAimAngle(45f);  // 45도 방향
playerWeaponManager.Fire();

// 또는 직접 접근
polarWeapon.UpdateAimAngle(45f);
polarWeapon.Fire();
```

### **2. 마우스로 조준 (MouseFollow 모드)**

```csharp
// Update에서
void Update()
{
    if (Input.GetMouseButton(0))
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        playerWeaponManager.UpdateAim(mousePos);  // 마우스 위치 추적
        playerWeaponManager.Fire();
    }
}
```

### **3. PlayerWeaponManager 초기화**

```csharp
// Inspector 설정
[SerializeField] private PlayerWeaponManager weaponManager;
[SerializeField] private bool enableMouseAim = false;  // true: MouseFollow, false: PolarAngle

// 런타임
void Start()
{
    // enableMouseAim에 따라 자동으로 조준 방식 결정
}
```

---

## 📊 Muzzle 위치 동기화

### **발사 위치 계산**

```csharp
// Before (Arm 없이 - 잘못된 방식)
Vector2 origin = transform.position;      // ❌ 무기 중심
Vector2 direction = transform.right;       // ❌ 무기 방향

// After (Arm 사용 - 올바른 방식)
Vector2 origin = Muzzle.position;         // ✅ 머즐 위치
Vector2 direction = Muzzle.right;          // ✅ 머즐 방향
```

### **극좌표 반지름 계산**

```csharp
// Before (고정값)
float startRadius = 0.8f;  // ❌ 하드코딩

// After (Muzzle 거리 계산)
float startRadius = Vector2.Distance(Muzzle.position, GetFieldCenter());  // ✅ 동적 계산
```

---

## 🎮 사용 시나리오

### **시나리오 1: 극좌표 각도 조준 (기본)**

```
1. PolarWeaponArm.BehaviorType = PolarAngle
2. 플레이어가 각도 입력 (예: WASD 키)
3. PlayerWeaponManager.UpdateAimAngle(angle)
4. Fire()
```

**프리팹 설정:**
```
Weapon_LaserDrill
├─ PolarLaserWeapon
│  └─ WeaponData: LaserWeaponData
└─ WeaponArm (PolarWeaponArm)
   ├─ BehaviorType: PolarAngle ⭐
   └─ Muzzle
```

---

### **시나리오 2: 마우스 추적 (ShapeDefense 호환)**

```
1. PolarWeaponArm.BehaviorType = MouseFollow
2. PlayerWeaponManager.enableMouseAim = true
3. Update()에서 UpdateAim(mousePos)
4. Fire()
```

**프리팹 설정:**
```
Weapon_LaserDrill
├─ PolarLaserWeapon
│  └─ WeaponData: LaserWeaponData
└─ WeaponArm (PolarWeaponArm)
   ├─ BehaviorType: MouseFollow ⭐
   └─ Muzzle
```

---

### **시나리오 3: 고정 포탑**

```
1. PolarWeaponArm.BehaviorType = Fixed
2. FixedAngle = 0 (오른쪽)
3. 자동 발사 (Fire() 반복)
```

**프리팹 설정:**
```
Turret_Laser
├─ PolarLaserWeapon
│  └─ WeaponData: LaserWeaponData
└─ WeaponArm (PolarWeaponArm)
   ├─ BehaviorType: Fixed ⭐
   ├─ FixedAngle: 0
   └─ Muzzle
```

---

## 🔄 데이터 흐름

### **조준 → 발사 → 투사체**

```
[입력]
플레이어 입력 (각도 또는 마우스)
    ↓
PlayerWeaponManager.UpdateAim() 또는 UpdateAimAngle()
    ↓
PolarWeaponBase.UpdateAim() 또는 UpdateAimAngle()
    ↓
PolarWeaponArm.SetAimAngle() 또는 SetAimFromWorldPosition()
    ↓
[발사]
PolarLaserWeapon.Fire()
    ↓
origin = Muzzle.position  ⭐
direction = Muzzle.right   ⭐
    ↓
PoolService.Get<PolarLaserProjectile>()
    ↓
beam.Launch(_field, LaserData, origin, direction)
```

---

## 🎯 Arm vs 비-Arm 비교

| 항목 | Arm 방식 (권장) | 비-Arm 방식 |
|------|----------------|------------|
| **발사 위치** | Muzzle.position | transform.position |
| **발사 방향** | Muzzle.right | transform.right 또는 고정 |
| **조준** | Arm 회전 | transform 회전 |
| **유연성** | 높음 (모드 선택) | 낮음 (고정) |
| **계층** | Weapon → Arm → Muzzle | Weapon |
| **ShapeDefense 호환** | ✅ 가능 | ❌ 불가 |
| **무기 위치 조정** | ✅ Muzzle만 이동 | ❌ 전체 이동 |

---

## 📋 체크리스트

### **프리팹 생성 시**

- [ ] WeaponArm GameObject 생성
- [ ] Muzzle Transform 생성 (WeaponArm 자식)
- [ ] PolarWeaponArm 컴포넌트 추가
- [ ] BehaviorType 설정 (PolarAngle/MouseFollow 등)
- [ ] PolarWeapon 컴포넌트 추가
- [ ] weaponArm 필드 할당 (자동 또는 수동)
- [ ] WeaponData 할당

### **런타임 테스트**

- [ ] Muzzle.position 확인 (Scene View)
- [ ] Arm 회전 확인 (BehaviorType에 따라)
- [ ] 발사 위치 확인 (Muzzle 위치와 일치)
- [ ] 발사 방향 확인 (Muzzle.right 방향)

---

## 🚀 다음 단계

1. **프리팹 생성**: Unity Editor에서 무기 프리팹 생성
2. **Arm 설정**: PolarWeaponArm 추가 및 BehaviorType 설정
3. **테스트**: Play 모드에서 조준 및 발사 확인
4. **최적화**: 불필요한 Arm 제거 (선택 사항)

---

## 참고 문서

- **데이터 흐름**: `Docs/Polar_Weapon_DataFlow.md`
- **프리셋 가이드**: `Docs/Phase2_WeaponPresets_Guide.md`
- **SGSystem 통합**: `Docs/Polar_SGSystem_Integration.md`
- **ShapeDefense WeaponArm**: `Assets/ShapeDefense/Scripts/WeaponArm.cs`
