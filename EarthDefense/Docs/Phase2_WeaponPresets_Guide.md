# Phase 2: 무기 프리셋 생성 가이드

## Unity Editor에서 생성

### 1. 레이저 무기 (Drill 타입)

**생성 경로:**
1. Project 창에서 `Assets/Resources/Polar/Weapons/` 폴더 생성
2. 우클릭 → `Create` → `EarthDefense` → `Polar` → `Weapon Data` → `Laser`
3. 파일명: `LaserWeaponData.asset`

**속성 설정 (Inspector):**
```
[ID & UI]
- Id: "weapon_laser_drill"
- WeaponName: "Drill Laser"
- Icon: (레이저 아이콘)

[Bundles]
- WeaponBundleId: "Weapons/LaserWeapon" (필요시)
- ProjectileBundleId: "Projectiles/LaserBeam" ⭐ (빔도 투사체)

[Combat]
- Damage: 5
- KnockbackPower: 0.1
- AreaType: Fixed
- DamageRadius: 1
- UseGaussianFalloff: false
- WoundIntensity: 0.3

[Beam (optional)]
- TickRate: 10

[Laser Specific]
- ExtendSpeed: 50
- RetractSpeed: 70
- MaxLength: 50
- BeamWidth: 0.1
- BeamColor: Cyan (R:0, G:255, B:255)
- Duration: 2
```

---

### 2. 머신건 무기 (Ripper 타입)

**생성 경로:**
1. `Assets/Resources/Polar/Weapons/`
2. 우클릭 → `Create` → `EarthDefense` → `Polar` → `Weapon Data` → `Machinegun`
3. 파일명: `MachinegunWeaponData.asset`

**속성 설정 (Inspector):**
```
[ID & UI]
- Id: "weapon_machinegun_ripper"
- WeaponName: "Ripper Machinegun"
- Icon: (머신건 아이콘)

[Bundles]
- WeaponBundleId: "Weapons/MachinegunWeapon" (필요시)
- ProjectileBundleId: "Projectiles/Bullet" ⭐

[Combat]
- Damage: 8
- KnockbackPower: 0.3
- AreaType: Gaussian
- DamageRadius: 3
- UseGaussianFalloff: true
- WoundIntensity: 0.6

[Beam (optional)]
- TickRate: 10

[Machinegun Specific]
- FireRate: 10 (발/초)
- ProjectileSpeed: 15
- SpreadAngle: 2
- ProjectileLifetime: 3
- ProjectileScale: 0.3
- ProjectileColor: Yellow (R:255, G:255, B:0)
```

---

### 3. 미사일 무기 (Hammer 타입)

**생성 경로:**
1. `Assets/Resources/Polar/Weapons/`
2. 우클릭 → `Create` → `EarthDefense` → `Polar` → `Weapon Data` → `Missile`
3. 파일명: `MissileWeaponData.asset`

**속성 설정 (Inspector):**
```
[ID & UI]
- Id: "weapon_missile_hammer"
- WeaponName: "Hammer Missile"
- Icon: (미사일 아이콘)

[Bundles]
- WeaponBundleId: "Weapons/MissileWeapon" (필요시)
- ProjectileBundleId: "Projectiles/Missile" ⭐

[Combat]
- Damage: 50
- KnockbackPower: 1.5
- AreaType: Explosion
- DamageRadius: 10
- UseGaussianFalloff: false
- WoundIntensity: 0.8

[Beam (optional)]
- TickRate: 10

[Missile Specific]
- FireRate: 0.5 (발/초)
- MissileSpeed: 12
- MissileLifetime: 5
- ExplosionRadius: 10
- ExplosionVFXPrefab: (폭발 이펙트 프리팹)
- MissileScale: 0.5
- MissileColor: Red (R:255, G:0, B:0)
```

---

## 무기 데이터 테이블 생성

**생성 경로:**
1. `Assets/Resources/Polar/Weapons/`
2. 우클릭 → `Create` → `EarthDefense` → `Polar` → `Weapon Data Table`
3. 파일명: `PolarWeaponDataTable.asset`

**속성 설정 (Inspector):**
```
[Weapons] (리스트에 추가)
- Element 0: LaserWeaponData
- Element 1: MachinegunWeaponData
- Element 2: MissileWeaponData
```

**Validate Data:**
- Inspector에서 `우클릭` → `Validate Data` 실행
- 콘솔에서 "Validation OK" 메시지 확인

---

## 번들 ID 규칙 ⭐

### **단순화된 구조**

모든 무기는 **ProjectileBundleId** 하나만 사용합니다:

| 무기 타입 | ProjectileBundleId | 설명 |
|-----------|-------------------|------|
| **Laser** | "Projectiles/LaserBeam" | 빔도 투사체로 취급 |
| **Machinegun** | "Projectiles/Bullet" | 일반 탄환 |
| **Missile** | "Projectiles/Missile" | 미사일 |

**변경 사항:**
- ❌ **BeamBundleId 제거** - 더 이상 사용하지 않음
- ✅ **ProjectileBundleId로 통합** - 빔/탄환/미사일 모두 포함

---

## 빠른 설정 체크리스트

### 레이저 (Drill)
- [x] ProjectileBundleId = "Projectiles/LaserBeam" ⭐
- [x] Damage=5, Knockback=0.1, AreaType=Fixed
- [x] TickRate=10, ExtendSpeed=50, RetractSpeed=70
- [x] MaxLength=50, BeamWidth=0.1, BeamColor=Cyan

### 머신건 (Ripper)
- [x] ProjectileBundleId = "Projectiles/Bullet" ⭐
- [x] Damage=8, Knockback=0.3, AreaType=Gaussian
- [x] FireRate=10, ProjectileSpeed=15, SpreadAngle=2
- [x] DamageRadius=3, ProjectileColor=Yellow

### 미사일 (Hammer)
- [x] ProjectileBundleId = "Projectiles/Missile" ⭐
- [x] Damage=50, Knockback=1.5, AreaType=Explosion
- [x] FireRate=0.5, MissileSpeed=12, DamageRadius=10
- [x] ExplosionRadius=10, MissileColor=Red

---

## 테스트 방법

### 1. PlayerWeaponManager 설정
```
[Field]
- PolarFieldBehaviour: (PolarFieldAdapter 컴포넌트)

[Weapon]
- DefaultWeaponData: LaserWeaponData
- DataTable: PolarWeaponDataTable
- DefaultWeaponId: "weapon_laser_drill"
```

### 2. 런타임 테스트
```csharp
// 발사
playerWeaponManager.Fire();

// 무기 교체
playerWeaponManager.NextWeapon();

// 특정 각도 발사 (머신건/미사일)
playerWeaponManager.Fire(45f);

// 레이저 중지
playerWeaponManager.StopFire();
```

---

## 프리셋 수치 (Phase 2 문서 기준)

| 무기 | Damage | Knockback | AreaType | Radius | FireRate |
|------|--------|-----------|----------|--------|----------|
| **Drill** | 5 | 0.1 | Fixed | 1 | 10 (tick) |
| **Ripper** | 8 | 0.3 | Gaussian | 3 | 10 |
| **Hammer** | 50 | 1.5 | Explosion | 10 | 0.5 |

---

## 참고

- Phase 2 문서: `Docs/phase2.md`
- 무기 타입 정의: `Assets/Polar/Weapons/Data/`
- 투사체 구현: `Assets/Polar/Weapons/Projectiles/`
- 데이터 흐름: `Docs/Polar_Weapon_DataFlow.md`
