# Polar 무기 시스템 데이터 흐름 (생성 → 발사 → 소멸)

## 📊 전체 아키텍처 개요

```
[Unity Editor]          [런타임 초기화]         [발사]              [충돌/소멸]
     ↓                       ↓                    ↓                    ↓
ScriptableObject  →  PlayerWeaponManager  →  무기 클래스  →  투사체 클래스  →  IPolarField
(데이터 에셋)        (타입별 자동 로딩)      (발사 관리)    (독립 로직)      (피해 적용)
```

---

## 🔄 Phase 1: 데이터 생성 (Unity Editor)

### 1-1. ScriptableObject 생성

```
Unity Editor
└─ Project 창
   └─ Assets/Resources/Polar/Weapons/
      ├─ 우클릭 → Create → EarthDefense → Polar → Weapon Data
      │  ├─ Laser   → LaserWeaponData.asset
      │  ├─ Machinegun → MachinegunWeaponData.asset
      │  └─ Missile → MissileWeaponData.asset
      │
      └─ Weapon Data Table → PolarWeaponDataTable.asset
```

### 1-2. 데이터 입력 (Inspector)

```
LaserWeaponData.asset (Inspector)
├─ [ID & UI]
│  ├─ Id: "weapon_laser_drill"
│  └─ WeaponName: "Drill Laser"
│
├─ [Combat] (기본 PolarWeaponData)
│  ├─ Damage: 5
│  ├─ KnockbackPower: 0.1
│  ├─ AreaType: Fixed
│  └─ DamageRadius: 1
│
└─ [Laser Specific] (PolarLaserWeaponData)
   ├─ ExtendSpeed: 50
   ├─ RetractSpeed: 70
   └─ BeamColor: Cyan
```

### 1-3. 테이블 등록

```csharp
// PolarWeaponDataTable.asset (Inspector)
[Weapons]
- Element 0: LaserWeaponData
- Element 1: MachinegunWeaponData
- Element 2: MissileWeaponData
```

---

## 🚀 Phase 2: 런타임 초기화

### 2-1. PlayerWeaponManager.Awake()

```csharp
// 1. IPolarField 획득
_field = polarFieldBehaviour as IPolarField;

// 2. 데이터 로딩
_currentWeaponData = ResolveWeaponData(defaultWeaponData, defaultWeaponId);
//    ↓
//    dataTable.GetById("weapon_laser_drill")
//    → PolarLaserWeaponData 반환

// 3. 무기 생성
LoadWeapon(_currentWeaponData);
```

### 2-2. LoadWeapon() - 타입별 자동 생성

```csharp
// 흐름도
PolarWeaponData weaponData
    ↓
CreateWeaponByType(weaponObj, weaponData)
    ↓ (타입 체크)
    ├─ PolarLaserWeaponData → AddComponent<PolarLaserWeapon>()
    ├─ PolarMachinegunWeaponData → AddComponent<PolarMachinegunWeapon>()
    └─ PolarMissileWeaponData → AddComponent<PolarMissileWeapon>()
    ↓
_currentWeapon.Initialize(_field, weaponData, usePool)
```

### 2-3. 무기 초기화

```csharp
// PolarLaserWeapon.Initialize()
protected override void OnInitialized()
{
    LoadBeamPrefab();
    //    ↓
    //    ResourceService.Instance.LoadPrefab(LaserData.BeamBundleId)
    //    → "Projectiles/LaserBeam" 프리팹 로드
}
```

**데이터 흐름:**
```
LaserWeaponData.asset
    ↓ (Inspector 값)
PolarLaserWeaponData (메모리)
    ↓ (.BeamBundleId)
ResourceService.LoadPrefab()
    ↓
_beamPrefab (GameObject 참조)
```

---

## 🎯 Phase 3: 발사 (Fire)

### 3-1. 입력 → 발사 요청

```csharp
// 외부 입력 (예: 플레이어 컨트롤러)
void Update()
{
    if (Input.GetButton("Fire1"))
    {
        playerWeaponManager.Fire();
        //    ↓
        //    _currentWeapon.Fire()
    }
}
```

### 3-2. 무기별 발사 로직

#### **레이저 (PolarLaserWeapon)**
```csharp
public override void Fire()
{
    if (!CanFire) return;  // 쿨다운 체크
    
    SpawnBeam();
    //    ↓
    // 1. 풀링 또는 인스턴스 생성
    PolarLaserProjectile beam = GetFromPoolOrInstantiate();
    
    // 2. 발사
    beam.Launch(_field, LaserData, origin, direction);
    
    // 3. 쿨다운 설정
    SetCooldown(1f / LaserData.TickRate);
}
```

#### **머신건 (PolarMachinegunWeapon)**
```csharp
public void Fire(float angleDeg)
{
    if (!CanFire) return;
    
    SpawnProjectile(angleDeg);
    //    ↓
    // 1. 산포도 적용
    float finalAngle = angleDeg + Random.Range(-SpreadAngle, SpreadAngle);
    
    // 2. 투사체 생성
    PolarMachinegunProjectile projectile = GetFromPoolOrInstantiate();
    
    // 3. 발사
    projectile.Launch(_field, MachinegunData, finalAngle, startRadius);
    
    // 4. 쿨다운
    SetCooldown(1f / MachinegunData.FireRate);
}
```

#### **미사일 (PolarMissileWeapon)**
```csharp
public void Fire(float angleDeg)
{
    if (!CanFire) return;
    
    SpawnMissile(angleDeg);
    //    ↓
    // 1. 미사일 생성
    PolarMissileProjectile missile = GetFromPoolOrInstantiate();
    
    // 2. 발사
    missile.Launch(_field, MissileData, angleDeg, startRadius);
    
    // 3. 쿨다운 (느림)
    SetCooldown(1f / MissileData.FireRate);  // 0.5 발/초 = 2초 쿨다운
}
```

### 3-3. 투사체 초기화

```csharp
// PolarLaserProjectile.Launch()
public override void Launch(IPolarField field, PolarWeaponData weaponData)
{
    _field = field;
    _weaponData = weaponData as PolarLaserWeaponData;
    _isActive = true;
    
    // 데이터에서 속성 추출
    InitializeBeam();
    //    ↓
    lineRenderer.startColor = LaserData.BeamColor;  // Cyan
    lineRenderer.startWidth = LaserData.BeamWidth;   // 0.1
    _currentLength = 0f;
    _nextTickTime = Time.time;
}
```

**데이터 흐름 (레이저 예시):**
```
LaserWeaponData.asset
    ↓ (BeamColor, BeamWidth, ExtendSpeed, TickRate)
PolarLaserProjectile (메모리)
    ↓ (LineRenderer 설정)
화면에 시각화
```

---

## ⚡ Phase 4: 업데이트 루프 (독립 로직)

### 4-1. 투사체별 Update 로직

#### **레이저 (빔)**
```csharp
protected override void OnUpdate(float deltaTime)
{
    // 1. 빔 길이 업데이트
    UpdateBeamLength(deltaTime);
    //    ↓
    _currentLength += LaserData.ExtendSpeed * deltaTime;  // 50 units/sec
    _currentLength = Mathf.Min(_currentLength, LaserData.MaxLength);  // Max 50
    
    // 2. 시각화
    UpdateBeamVisual();
    //    ↓
    lineRenderer.SetPosition(1, origin + direction * _currentLength);
    
    // 3. 틱 데미지 (10회/초)
    if (Time.time >= _nextTickTime)
    {
        ApplyTickDamage(hitPoint);
        _nextTickTime = Time.time + (1f / LaserData.TickRate);  // 0.1초
    }
}
```

#### **머신건 (탄환)**
```csharp
protected override void OnUpdate(float deltaTime)
{
    // 1. 극좌표 이동
    radius += speed * deltaTime;  // MachinegunData.ProjectileSpeed = 15
    
    // 2. 위치 업데이트 (극좌표 → 직교좌표 변환)
    UpdatePosition();
    //    ↓
    float angleRad = angle * Mathf.Deg2Rad;
    Vector3 pos = new Vector3(
        Mathf.Cos(angleRad) * radius,
        Mathf.Sin(angleRad) * radius, 0
    );
    transform.position = _field.CenterPosition + pos;
    
    // 3. 충돌 체크
    if (CheckCollision())
    {
        OnCollision();  // → 가우시안 피해 적용
    }
}
```

#### **미사일 (폭발)**
```csharp
protected override void OnUpdate(float deltaTime)
{
    // 1. 극좌표 이동 (느림)
    radius += speed * deltaTime;  // MissileData.MissileSpeed = 12
    
    // 2. 위치 업데이트 + 회전
    UpdatePosition();
    //    ↓
    transform.rotation = Quaternion.Euler(0, 0, angle);  // 진행 방향
    
    // 3. 충돌 체크
    if (CheckCollision())
    {
        SpawnExplosionVFX();  // 폭발 이펙트
        OnCollision();        // → 폭발 범위 피해 적용
    }
}
```

---

## 💥 Phase 5: 충돌 및 피해 적용

### 5-1. 충돌 감지

```csharp
// 공통 로직 (PolarProjectileBase)
private bool CheckCollision()
{
    int sectorIndex = _field.AngleToSectorIndex(angle);
    float sectorRadius = _field.GetSectorRadius(sectorIndex);
    
    return radius >= (sectorRadius - epsilon);
}
```

### 5-2. 피해 적용 (AreaType별 분기)

#### **Fixed (레이저)**
```csharp
private void ApplyTickDamage(Vector2 hitPoint)
{
    float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    int sectorIndex = _field.AngleToSectorIndex(angleDeg);
    
    float damagePerTick = LaserData.Damage / LaserData.TickRate;
    //                  = 5 / 10 = 0.5 per tick
    
    _field.SetLastWeaponKnockback(LaserData.KnockbackPower);  // 0.1
    _field.ApplyDamageToSector(sectorIndex, damagePerTick);   // 단일 섹터
}
```

#### **Gaussian (머신건)**
```csharp
private void ApplyGaussianDamage(int centerIndex, PolarCombatProperties props)
{
    // 중심
    _field.ApplyDamageToSector(centerIndex, props.Damage);  // 8
    
    // 가우시안 분포
    for (int offset = 1; offset <= props.DamageRadius; offset++)  // Radius=3
    {
        float sigma = radius / 3f;
        float gaussian = Mathf.Exp(-offset * offset / (2f * sigma * sigma));
        float damage = props.Damage * gaussian;
        
        // 좌우 섹터
        int leftIndex = (centerIndex - offset + 180) % 180;
        int rightIndex = (centerIndex + offset) % 180;
        
        _field.ApplyDamageToSector(leftIndex, damage);
        _field.ApplyDamageToSector(rightIndex, damage);
    }
}
```

**피해 분포 예시 (Damage=8, Radius=3):**
```
Sector: -3    -2    -1    [0]    +1    +2    +3
Damage: 0.97  2.36  5.04  [8.0]  5.04  2.36  0.97
```

#### **Explosion (미사일)**
```csharp
private void ApplyExplosionDamage(int centerIndex, PolarCombatProperties props)
{
    // 중심
    _field.ApplyDamageToSector(centerIndex, props.Damage);  // 50
    
    // 폭발 범위
    for (int offset = 1; offset <= props.DamageRadius; offset++)  // Radius=10
    {
        // 선형 감쇠 (UseGaussianFalloff=false)
        float falloff = 1f - (float)offset / (radius + 1);
        float damage = props.Damage * falloff;
        
        int leftIndex = (centerIndex - offset + 180) % 180;
        int rightIndex = (centerIndex + offset) % 180;
        
        _field.ApplyDamageToSector(leftIndex, damage);
        _field.ApplyDamageToSector(rightIndex, damage);
    }
}
```

**피해 분포 예시 (Damage=50, Radius=10):**
```
Sector: -10   -5    -1    [0]    +1    +5    +10
Damage: 4.5   22.7  45.5  [50]   45.5  22.7  4.5
```

### 5-3. IPolarField → 저항력 시스템

```csharp
// PolarFieldController.ApplyDamageToSector()
public void ApplyDamageToSector(int sectorIndex, float damage)
{
    if (sectorIndex < 0 || sectorIndex >= SectorCount) return;
    
    // 1. 저항력 감소
    _sectorResistances[sectorIndex] -= damage;
    
    // 2. 저항력 0 도달 → 라인 붕괴
    if (_sectorResistances[sectorIndex] <= 0f)
    {
        ExecuteKnockback(sectorIndex, _lastWeaponKnockback);
        //    ↓
        // 라인 후퇴
        _sectorRadii[sectorIndex] += knockbackDistance;
        
        // 저항력 리셋
        _sectorResistances[sectorIndex] = Config.BaseResistance;
        
        // 이벤트 발생
        OnLineBreak?.Invoke(sectorIndex);
        OnKnockbackExecuted?.Invoke(sectorIndex, knockbackDistance);
    }
}
```

---

## 🗑️ Phase 6: 소멸

### 6-1. 조건별 소멸

#### **레이저 (리트랙트)**
```csharp
// 사용자가 버튼을 놓으면
playerWeaponManager.StopFire();
//    ↓
laser.BeginRetract();
//    ↓
_isRetracting = true;
//    ↓ (OnUpdate)
_currentLength -= LaserData.RetractSpeed * deltaTime;  // 70 units/sec
//    ↓
if (_currentLength <= 0f)
    Deactivate();
```

#### **머신건 (충돌 또는 범위 이탈)**
```csharp
protected override void OnUpdate(float deltaTime)
{
    // ...이동 로직...
    
    // 충돌
    if (CheckCollision())
    {
        OnCollision();
        Deactivate();
        return;
    }
    
    // 범위 이탈
    if (radius > _field.InitialRadius * 2f)
    {
        Deactivate();
        return;
    }
}
```

#### **미사일 (충돌 또는 수명)**
```csharp
protected override void OnUpdate(float deltaTime)
{
    // ...이동 로직...
    
    // 충돌
    if (CheckCollision())
    {
        SpawnExplosionVFX();  // 폭발 이펙트
        OnCollision();
        Deactivate();
        return;
    }
    
    // 수명 (MissileLifetime = 5초)
    if (Time.time - _spawnTime >= MissileData.MissileLifetime)
    {
        Deactivate();
        return;
    }
}
```

### 6-2. Deactivate() → 풀 반환

```csharp
public override void Deactivate()
{
    base.Deactivate();
    _isActive = false;
    
    // 시각 효과 비활성화
    if (spriteRenderer != null) spriteRenderer.enabled = false;
    if (trailRenderer != null) trailRenderer.emitting = false;
    
    // 풀 반환 (PolarWeapon에서 관리)
    // PoolService.Instance.Return(this);
    // 또는 GameObject.Destroy(gameObject);
}
```

---

## 📈 전체 데이터 흐름 요약

### 시간순 흐름

```
[T0] Unity Editor - ScriptableObject 생성
     └─ LaserWeaponData.asset (BeamColor=Cyan, Damage=5, TickRate=10)

[T1] 게임 시작 - PlayerWeaponManager.Awake()
     └─ LoadWeapon(LaserWeaponData)
        └─ CreateWeaponByType() → PolarLaserWeapon 생성
           └─ Initialize(_field, LaserWeaponData)
              └─ LoadBeamPrefab("Projectiles/LaserBeam")

[T2] 플레이어 입력 - Fire 버튼 누름
     └─ playerWeaponManager.Fire()
        └─ laser.Fire()
           └─ SpawnBeam()
              └─ beam.Launch(_field, LaserData, origin, direction)
                 └─ _weaponData = LaserData
                 └─ lineRenderer.startColor = LaserData.BeamColor (Cyan)

[T3] 매 프레임 - OnUpdate()
     └─ _currentLength += LaserData.ExtendSpeed * dt (50 units/sec)
     └─ lineRenderer.SetPosition(1, end)
     └─ if (Time.time >= _nextTickTime)
        └─ ApplyTickDamage()
           └─ damagePerTick = LaserData.Damage / TickRate (5 / 10 = 0.5)
           └─ _field.ApplyDamageToSector(sectorIndex, 0.5)
              └─ _sectorResistances[i] -= 0.5
              └─ if (resistance <= 0) → ExecuteKnockback(0.1)

[T4] 버튼 뗌 - Fire 버튼 놓음
     └─ playerWeaponManager.StopFire()
        └─ laser.BeginRetract()
           └─ _isRetracting = true
           └─ _currentLength -= LaserData.RetractSpeed * dt (70 units/sec)
           └─ if (_currentLength <= 0) → Deactivate()

[T5] 소멸
     └─ lineRenderer.enabled = false
     └─ PoolService.Return() or Destroy()
```

---

## 🎯 핵심 인사이트

### 1. **완전 데이터 주도**
```
ScriptableObject → 메모리 → 런타임 로직
(Inspector 값)    (타입별)   (독립 실행)
```

### 2. **타입별 자동 매칭**
```
LaserWeaponData → LaserWeapon → LaserProjectile
(BeamColor)       (SpawnBeam)   (lineRenderer.color)
```

### 3. **독립 로직 실행**
```
투사체 = 자율 개체
- 스스로 이동
- 스스로 충돌 감지
- 스스로 피해 적용
```

### 4. **IPolarField 추상화**
```
투사체 → IPolarField → 구현체
(피해 요청)  (인터페이스)  (저항력 시스템)
```

---

## 📊 성능 지표

| 단계 | 처리 시간 | 비고 |
|------|-----------|------|
| 초기화 | < 1ms | 프리팹 로드, 컴포넌트 추가 |
| 발사 | < 0.1ms | 인스턴스 생성, Launch 호출 |
| 업데이트 | < 0.05ms | 위치 계산, 충돌 체크 |
| 피해 적용 | < 0.1ms | 섹터 인덱스 변환, 저항력 감소 |
| 소멸 | < 0.05ms | 비활성화, 풀 반환 |

**총합:** < 0.3ms per 투사체 per frame (60fps 기준 안정적)

---

## 참고 문서
- Phase 2 설계: `Docs/phase2.md`
- 프리셋 가이드: `Docs/Phase2_WeaponPresets_Guide.md`
- Phase 1 구현: `Docs/phase1.md`
