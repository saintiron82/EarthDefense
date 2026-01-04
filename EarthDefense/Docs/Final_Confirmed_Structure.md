# ✅ 최종 확정: 총알은 불변 고정값, 데이터는 무기에서 수신

## 🎯 최종 합의

**"총알은 그 자체의 불변 고정값들이라서 데이터적인 값은 모조리 무기에서 수신받을거다"**

---

## 📊 최종 구조 확정

### 총알 (Projectile) - 불변 고정값

```csharp
// bullet_normal.prefab
public class Bullet : BaseProjectile
{
    // ✅ 불변 고정값 (총알 타입 특성)
    [Header("Projectile Type")]
    [SerializeField] private ProjectileType projectileType = ProjectileType.Normal;
    
    [Header("Hit Behavior - 타입 특성")]
    [SerializeField] private float rehitCooldown = 0.05f;  // 재타격 쿨
    
    [Header("Hit Detection - 판정")]
    [SerializeField] private float hitRadius = 0.07f;      // 판정 크기
    [SerializeField] private int sweepSteps = 12;          // 충돌 정밀도
    
    [Header("Visual - 비주얼")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private Color color = Color.white;
    [SerializeField] private TrailRenderer trail;
    
    // ❌ 데이터 값 없음! (무기에서 수신)
    // ❌ damage
    // ❌ speed  
    // ❌ lifetime
    // ❌ maxHits (관통도 무기가 제어) ⭐
    
    // 런타임 - 무기로부터 수신받음 ⭐
    private float _damage;      // 무기가 주입
    private float _speed;       // 무기가 주입
    private float _lifetime;    // 무기가 주입
    private int _maxHits;       // 무기가 주입 ⭐
    
    public void Fire(Vector2 direction, float damage, float speed, float lifetime,
                     int maxHits, GameObject source, int teamKey)
    {
        // 무기로부터 데이터 수신 ⭐
        _damage = damage;
        _speed = speed;
        _lifetime = lifetime;
        _maxHits = maxHits;  // ⭐
        // ...
    }
}
```

**총알이 가진 것:**
- ✅ 타입 (Normal/Fire/Ice) - 불변
- ✅ 재타격 쿨타임 - 불변 (타입 특성)
- ✅ 판정 크기 - 불변
- ✅ 비주얼 - 불변
- ❌ 데미지, 속도, 수명, 관통 - 없음! (무기에서 수신) ⭐

---

### 무기 (Weapon) - 데이터 소유자

```csharp
// weapon_machinegun.prefab
public class MachineGunWeapon : BaseWeapon
{
    // ✅ 데이터 값 (업그레이드 가능)
    [Header("Projectile Specs - 무기가 보유")]
    [SerializeField] protected float projectileDamage = 10f;      ⭐
    [SerializeField] protected float projectileSpeed = 20f;       ⭐
    [SerializeField] protected float projectileLifetime = 3f;     ⭐
    [SerializeField] protected int projectileMaxHits = 1;         ⭐
    
    [Header("Fire Settings")]
    [SerializeField] protected float fireRate = 12f;
    [SerializeField] protected FireMode fireMode = FireMode.Automatic;
    
    [Header("Projectile Prefab")]
    [SerializeField] private BaseProjectile projectilePrefab;  // 총알 타입만 선택
    
    protected override void FireInternal(Vector2 direction)
    {
        var projectile = Instantiate(projectilePrefab, muzzlePos, rotation);
        
        // 무기의 데이터를 총알에 주입 ⭐
        projectile.Fire(
            direction,
            projectileDamage,     // 무기 데이터 전송
            projectileSpeed,      // 무기 데이터 전송
            projectileLifetime,   // 무기 데이터 전송
            projectileMaxHits,    // 무기 데이터 전송 ⭐
            _source,
            _sourceTeamKey
        );
    }
}
```

**무기가 가진 것:**
- ✅ 데미지 - 업그레이드 가능
- ✅ 속도 - 업그레이드 가능
- ✅ 수명 (사거리) - 업그레이드 가능
- ✅ 관통 횟수 - 업그레이드 가능 ⭐
- ✅ 연사율 - 업그레이드 가능
- ✅ 총알 타입 선택 - 교체 가능

---

## 🎯 역할 분담 (최종)

### 총알 = 불변 특성

```
총알이 정의하는 것 (프리팹에 고정):
✅ 타입 특성
   - projectileType: Normal/Fire/Ice
   - explosionRadius: (폭발탄만)
   
✅ 판정
   - hitRadius: 0.07f
   - sweepSteps: 12
   - rehitCooldown: 0.05f
   
✅ 비주얼
   - sprite, color, trail
   - particle effects
   
✅ 특수 효과 로직
   - ApplySpecialEffect() 구현
   - 화상, 빙결, 폭발 등

❌ 데이터 값 (무기가 주입)
   - damage, speed, lifetime, maxHits
```

### 무기 = 데이터 값

```
무기가 정의하는 것 (업그레이드 가능):
✅ 성능 데이터
   - projectileDamage
   - projectileSpeed
   - projectileLifetime
   - projectileMaxHits  ⭐
   - fireRate
   
✅ 총알 선택
   - projectilePrefab 참조
   
❌ 총알 특성 (총알이 정의)
```

---

## 💡 핵심 원칙 (최종)

### 원칙 1: 총알은 "타입"

```
총알 = 어떤 종류인가?
- 일반탄: 기본 타입
- 화염탄: 화상 효과, 빨간 비주얼
- 빙결탄: 슬로우 효과, 파란 비주얼
- 폭발탄: 범위 데미지, 폭발 효과

→ 프리팹에 타입 특성 고정
→ 데이터 값 없음
```

### 원칙 2: 무기는 "성능"

```
무기 = 얼마나 강한가?
- 데미지: 10 → 15 (업그레이드)
- 속도: 20 → 25 (업그레이드)
- 사거리: 3 → 4 (업그레이드)
- 연사율: 12 → 15 (업그레이드)

→ 무기에 데이터 보유
→ 총알에 주입
```

### 원칙 3: 주입 방식

```
무기 → 총알 데이터 전송

weapon.Fire()
    ↓
projectile.Fire(
    direction,
    weapon.projectileDamage,    // ⭐
    weapon.projectileSpeed,     // ⭐
    weapon.projectileLifetime   // ⭐
)
    ↓
projectile._damage = weapon.projectileDamage
projectile._speed = weapon.projectileSpeed
projectile._lifetime = weapon.projectileLifetime
```

---

## 🎮 실전 예시

### 예시 1: 무기 업그레이드

```
[레벨 1 머신건]
weapon_machinegun:
  projectileDamage: 10    ⭐
  projectileSpeed: 20     ⭐
  projectileLifetime: 3   ⭐
  projectilePrefab: bullet_normal

[업그레이드!]
weapon_machinegun:
  projectileDamage: 15    ⭐ +5
  projectileSpeed: 25     ⭐ +5
  projectileLifetime: 4   ⭐ +1
  projectilePrefab: bullet_normal (동일)

결과:
→ 총알 프리팹은 그대로 ✅
→ 무기 데이터만 변경 ✅
→ 15 데미지, 25 속도로 발사! ✅
```

### 예시 2: 총알 교체

```
[일반탄]
weapon_machinegun:
  projectileDamage: 15 (그대로)
  projectilePrefab: bullet_normal ⭐

bullet_normal:
  projectileType: Normal
  maxHits: 1
  sprite: white_bullet

발사 → 15 데미지, 흰색 총알

[화염탄으로 교체]
weapon_machinegun:
  projectileDamage: 15 (그대로)
  projectilePrefab: bullet_fire ⭐

bullet_fire:
  projectileType: Fire
  maxHits: 3 (관통!)
  sprite: red_bullet
  ApplySpecialEffect() { 화상 }

발사 → 15 데미지 (동일), 빨간 총알, 화상 효과, 3회 관통!
```

### 예시 3: 같은 총알, 다른 무기

```
[머신건]
weapon_machinegun:
  projectileDamage: 10
  projectileSpeed: 20
  projectilePrefab: bullet_normal

[저격총]
weapon_sniper:
  projectileDamage: 50 (다름!)
  projectileSpeed: 40 (다름!)
  projectilePrefab: bullet_normal (같은 총알!)

결과:
→ 같은 bullet_normal 사용
→ 머신건: 10 데미지, 20 속도
→ 저격총: 50 데미지, 40 속도
→ 완전히 다른 느낌! ✅
```

---

## 📋 최종 정리표

| 항목 | 총알 (불변) | 무기 (데이터) | 설명 |
|------|-------------|---------------|------|
| **성능** |
| damage | ❌ | ✅ | 무기가 주입 |
| speed | ❌ | ✅ | 무기가 주입 |
| lifetime | ❌ | ✅ | 무기가 주입 |
| maxHits | ❌ | ✅ | 무기가 주입 ⭐ |
| fireRate | ❌ | ✅ | 무기 특성 |
| **타입** |
| projectileType | ✅ | ❌ | 총알 고정 |
| explosionRadius | ✅ | ❌ | 타입 특성 |
| **판정** |
| hitRadius | ✅ | ❌ | 총알 고정 |
| sweepSteps | ✅ | ❌ | 총알 고정 |
| rehitCooldown | ✅ | ❌ | 총알 고정 |
| **비주얼** |
| sprite | ✅ | ❌ | 총알 고정 |
| color | ✅ | ❌ | 총알 고정 |
| trail | ✅ | ❌ | 총알 고정 |
| **효과** |
| 특수효과 로직 | ✅ | ❌ | 총알 구현 |

---

## 🎉 최종 확정

### 핵심 원칙:

```
1. 총알 = 불변 고정값 ✅
   - 타입 특성 (Normal/Fire/Ice)
   - 판정 (hitRadius)
   - 비주얼 (sprite, trail)
   - 특수 효과 로직
   - 프리팹에 고정

2. 무기 = 데이터 값 ✅
   - 성능 (damage, speed, lifetime)
   - 연사율 (fireRate)
   - 업그레이드 가능
   
3. 주입 방식 ✅
   - 무기 → 총알로 데이터 전송
   - Fire(damage, speed, lifetime)
   
4. 데이터 파일 불필요 ❌
   - 총알 프리팹만으로 충분
   - ScriptableObject 불필요
```

---

## 📊 최종 구조

```
WeaponDataTable.asset (카탈로그만)
└─ 무기 ID, 이름, 아이콘

weapon_machinegun.prefab (데이터 + 로직)
└─ MachineGunWeapon
   ├─ projectileDamage: 10      ⭐ 데이터
   ├─ projectileSpeed: 20       ⭐ 데이터
   ├─ projectileLifetime: 3     ⭐ 데이터
   ├─ projectileMaxHits: 1      ⭐ 데이터
   ├─ fireRate: 12              ⭐ 데이터
   └─ projectilePrefab: [bullet_normal]

bullet_normal.prefab (타입 + 비주얼)
└─ Bullet
   ├─ projectileType: Normal    ⭐ 불변
   ├─ hitRadius: 0.07           ⭐ 불변
   ├─ rehitCooldown: 0.05       ⭐ 불변
   ├─ sprite, trail             ⭐ 불변
   └─ ApplySpecialEffect()      ⭐ 불변
   
   런타임:
   ├─ _damage (무기 수신)       ⭐
   ├─ _speed (무기 수신)        ⭐
   ├─ _lifetime (무기 수신)     ⭐
   └─ _maxHits (무기 수신)      ⭐
```

---

## ✅ 완벽한 데이터 드리븐 구조 완성!

### 장점:

```
1. 업그레이드 간편
   weapon.projectileDamage += 5
   → 총알 프리팹 수정 불필요

2. 총알 교체 가능
   weapon.projectilePrefab = bullet_fire
   → 타입/효과 변경

3. 같은 총알, 다른 성능
   머신건/저격총이 같은 bullet_normal 사용
   → 무기 데이터로 차별화

4. 명확한 책임
   무기 = 데이터
   총알 = 타입/비주얼/효과
   
5. 중복 없음
   데이터 파일 불필요
   프리팹만으로 완벽
```

**총알은 불변 고정값, 데이터는 무기에서 수신!** 🎯
**완벽한 구조 확정!** ✅

