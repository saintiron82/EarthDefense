# Polar 무기 시스템 가이드

## 구조

### 베이스 클래스
```
PolarWeaponBase (추상)
├── PolarLaserWeapon (레이저 전용)
└── PolarProjectileWeapon (일반 투사체)
```

### 무기 프리팹 설정

#### 1. 일반 투사체 무기 (미사일, 머신건, 중력 등)
```
GameObject "Weapon_Missile"
├── PolarProjectileWeapon (컴포넌트)
├── Transform Muzzle (발사 위치)
└── SpriteRenderer (선택)
```

#### 2. 레이저 무기
```
GameObject "Weapon_Laser"
├── PolarLaserWeapon (컴포넌트)
├── Transform Muzzle (발사 위치)
└── LineRenderer (레이저 빔)
```

## 무기 프리팹 생성 방법

### 1. GameObject 생성
```
Hierarchy 우클릭 → Create Empty → "Weapon_Missile"
```

### 2. 컴포넌트 추가
- 일반 투사체: `Add Component → PolarProjectileWeapon`
- 레이저: `Add Component → PolarLaserWeapon`

### 3. Muzzle 설정
```
Weapon_Missile 우클릭 → Create Empty → "Muzzle"
Inspector에서 PolarProjectileWeapon의 Muzzle 필드에 드래그
```

### 4. Prefab 저장
```
Assets/Resources/Weapons/Weapon_Missile.prefab
```

### 5. WeaponData 설정
```
Inspector:
- Weapon Bundle Id: "Weapon_Missile"
- Projectile Bundle Id: "GravityProjectile" (투사체 프리팹)
```

## 사용 방법

### PlayerWeaponManager
```csharp
// 무기 교체 (자동으로 프리팹 로드 및 초기화)
playerWeaponManager.SetWeaponData(gravityWeaponData);

// 발사
playerWeaponManager.FireProjectile(angleDeg);

// 레이저 전용
playerWeaponManager.StartFireLaser();
playerWeaponManager.StopFireLaser();
```

## 무기 추가 절차

1. **WeaponData 생성**: `Create → EarthDefense → Polar → Weapon Data → Gravity`
2. **무기 프리팹 생성**:
   - GameObject + `PolarProjectileWeapon` 컴포넌트
   - Muzzle Transform 설정
   - Prefab 저장
3. **WeaponData 설정**:
   - `Weapon Bundle Id`: 무기 프리팹 경로
   - `Projectile Bundle Id`: 투사체 프리팹 경로
4. **완료**: PlayerWeaponManager가 자동으로 로드 및 사용

## 주의사항

- 무기 프리팹에 반드시 `PolarWeaponBase` 상속 컴포넌트 필요
- `WeaponBundleId`는 `Resources/Weapons/` 폴더 기준
- Muzzle이 없으면 무기 자신의 Transform 사용

