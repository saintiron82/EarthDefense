# ✅ 발사 모드: 오토매틱 & 메뉴얼

## 🎯 발사 모드 추가

**"발사도 오토매틱과 메뉴얼로 정리하자"**

---

## 📊 FireMode Enum

```csharp
public enum FireMode
{
    Manual,      // 수동: 클릭할 때마다 1발
    Automatic    // 자동: 버튼을 누르고 있으면 연속 발사
}
```

---

## 🔧 BaseWeapon 수정

### 추가된 필드:

```csharp
[Header("Fire Mode")]
[SerializeField] protected FireMode fireMode = FireMode.Manual;

// Runtime
protected bool _isFiring;  // 발사 중 여부 (자동 모드용)

// Property
public FireMode CurrentFireMode => fireMode;
```

### 추가된 메서드:

```csharp
// 자동 발사 처리
protected virtual void Update()
{
    if (fireMode == FireMode.Automatic && _isFiring)
    {
        if (CanFire)
        {
            Fire(_currentAimTarget);
        }
    }
}

// 발사 시작 (자동 모드용)
public virtual void StartFire()
{
    _isFiring = true;
}

// 발사 중지 (자동 모드용)
public virtual void StopFire()
{
    _isFiring = false;
}
```

---

## 🎮 WeaponController 수정

### 발사 모드별 처리:

```csharp
void Update()
{
    // 조준 업데이트
    _currentWeapon.UpdateAim(world);

    // 입력 확인
    bool attackPressed = _attackAction.IsPressed();
    bool attackJustPressed = _attackAction.WasPressedThisFrame();
    bool attackReleased = _attackAction.WasReleasedThisFrame();

    // 발사 모드에 따라 처리
    if (_currentWeapon.CurrentFireMode == FireMode.Automatic)
    {
        // 자동: 버튼 누르는 동안 연속 발사 ⭐
        if (attackJustPressed)
            _currentWeapon.StartFire();
        else if (attackReleased)
            _currentWeapon.StopFire();
    }
    else // Manual
    {
        // 수동: 클릭할 때마다 1발 ⭐
        if (attackJustPressed)
            _currentWeapon.Fire(world);
    }
}
```

---

## 🔄 동작 흐름

### Manual 모드 (수동):

```
클릭
    ↓
attackJustPressed == true
    ↓
weapon.Fire(world)
    ↓
1발 발사
    ↓
릴리즈 (아무 일 없음)
```

**특징:**
- 클릭할 때마다 정확히 1발
- 연사하려면 계속 클릭 필요
- 정밀 사격에 적합

---

### Automatic 모드 (자동):

```
버튼 누름
    ↓
attackJustPressed == true
    ↓
weapon.StartFire()
    ↓
_isFiring = true
    ↓
Update()마다
    ├─ if (_isFiring && CanFire)
    └─ weapon.Fire(world)  // 연속 발사! ⭐
    
버튼 릴리즈
    ↓
attackReleased == true
    ↓
weapon.StopFire()
    ↓
_isFiring = false
    ↓
발사 중지
```

**특징:**
- 버튼 누르는 동안 연속 발사
- fireRate에 따라 자동 연사
- 압도적인 화력

---

## 💡 사용 예시

### Manual 모드 무기 (저격총):

```csharp
weapon_sniper.prefab
└─ SniperWeapon : BaseWeapon
   ├─ Fire Mode: Manual ⭐
   ├─ Fire Rate: 1
   └─ Damage: 100

결과:
→ 클릭할 때마다 1발
→ 정확한 조준 필요
→ 높은 데미지
```

### Automatic 모드 무기 (머신건):

```csharp
weapon_machinegun.prefab
└─ MachineGunWeapon : BaseWeapon
   ├─ Fire Mode: Automatic ⭐
   ├─ Fire Rate: 12
   └─ Damage: 10

결과:
→ 버튼 누르는 동안 연속 발사
→ 초당 12발 자동 발사
→ 압도적인 화력
```

### Automatic 모드 무기 (레이저):

```csharp
weapon_laser.prefab
└─ LaserWeapon : BaseWeapon
   ├─ Fire Mode: Automatic ⭐
   ├─ Fire Rate: 1
   └─ Laser Duration: 2

결과:
→ 버튼 누르면 레이저 발사
→ 2초 동안 지속
→ 릴리즈하면 즉시 중지
```

---

## 📝 Unity 설정

### 프리팹 제작 시:

```
1. weapon_machinegun 선택
2. MachineGunWeapon 컴포넌트
3. Fire Mode: Automatic 선택 ⭐

또는

1. weapon_sniper 선택
2. SniperWeapon 컴포넌트
3. Fire Mode: Manual 선택 ⭐
```

---

## 🎯 비교

| 특징 | Manual (수동) | Automatic (자동) |
|------|---------------|------------------|
| 발사 방식 | 클릭당 1발 | 버튼 누르는 동안 연속 |
| 정밀도 | 높음 | 중간 |
| 화력 | 낮음 | 높음 |
| 탄약 소모 | 낮음 | 높음 |
| 적합한 무기 | 저격총, 캐논 | 머신건, 레이저 |
| 사용 방법 | 클릭 연타 | 버튼 홀드 |

---

## ✅ 장점

### 1. 무기 다양성
```
Manual: 정밀 무기
Automatic: 화력 무기

→ 플레이 스타일 다양화
```

### 2. 밸런싱
```
Manual: 높은 데미지, 낮은 연사
Automatic: 낮은 데미지, 높은 연사

→ 각자의 장단점
```

### 3. 사용자 선택
```
프리팹에서 Fire Mode만 변경
→ 같은 무기도 다른 느낌
```

---

## 🎮 실전 활용

### 무기 타입별 추천 모드:

```
✅ Manual 추천:
- 저격총 (1발 고데미지)
- 캐논 (폭발 무기)
- 로켓 런처

✅ Automatic 추천:
- 머신건 (연속 사격)
- 레이저 (지속 발사)
- 플레임 스로워
- 미니건
```

---

## 🔧 확장 가능성

### Burst 모드 추가:

```csharp
public enum FireMode
{
    Manual,      // 클릭당 1발
    Automatic,   // 연속 발사
    Burst        // 클릭당 3발 연사 (미래 확장)
}
```

### Charge 모드 추가:

```csharp
public enum FireMode
{
    Manual,
    Automatic,
    Charge       // 차징 후 발사 (미래 확장)
}
```

---

## 🎉 결과

**발사 모드 완성!**

```
✅ FireMode enum 추가
   - Manual: 수동 (클릭당 1발)
   - Automatic: 자동 (연속 발사)

✅ BaseWeapon 지원
   - Update()에서 자동 발사 처리
   - StartFire() / StopFire()

✅ WeaponController 지원
   - 모드에 따라 다른 입력 처리

✅ 프리팹 설정 가능
   - Fire Mode 필드에서 선택

✅ 에러 없음
```

**무기마다 발사 모드 설정 가능!** 🎯

