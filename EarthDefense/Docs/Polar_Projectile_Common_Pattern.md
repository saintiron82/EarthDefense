# PolarProjectileBase 공통 기능 가이드

## 개요
극좌표 기반 투사체들의 공통 로직을 PolarProjectileBase에 추상화했습니다.
이동, 충돌 감지 등의 반복 코드를 제거하고 각 투사체는 고유 로직만 구현하면 됩니다.

## 공통 제공 기능

### 1. 극좌표 이동 필드
```csharp
protected float _angleDeg;      // 발사 각도 (도)
protected float _radius;        // 현재 반지름
protected float _speed;         // 이동 속도
protected bool _hasReachedWall; // 벽 충돌 여부
```

### 2. 극좌표 발사
```csharp
protected void LaunchPolar(IPolarField field, PolarWeaponData weaponData, 
                          float angleDeg, float startRadius, float speed)
```
- 극좌표 필드 초기화 및 위치 설정
- 모든 극좌표 투사체의 공통 발사 로직

### 3. 위치 업데이트
```csharp
protected void UpdatePolarPosition()
```
- 극좌표 → 월드 좌표 변환
- Transform 위치 자동 업데이트

### 4. 이동 및 충돌 감지
```csharp
protected bool UpdatePolarMovementAndCheckWallCollision(
    float deltaTime, 
    out int hitSectorIndex, 
    out Vector2 hitPosition)
```
- 매 프레임 외부로 이동
- 벽 충돌 자동 감지
- 충돌 시 섹터 인덱스 및 위치 반환

### 5. 유틸리티 메서드
```csharp
protected int GetCurrentSectorIndex()     // 현재 섹터 인덱스
protected Vector2 GetCurrentPosition()    // 현재 월드 좌표
```

## 사용 패턴

### 기본 극좌표 투사체 구현
```csharp
public class MyPolarProjectile : PolarProjectileBase
{
    // 1. 추상 메서드 구현
    public override void Launch(IPolarField field, PolarWeaponData weaponData)
    {
        _field = field;
        _weaponData = weaponData;
        _isActive = true;
        
        // 시각 초기화 등
    }
    
    // 2. 극좌표 발사 오버로드 (편의 메서드)
    public void Launch(IPolarField field, PolarWeaponData weaponData, 
                      float angleDeg, float startRadius, float speed)
    {
        Launch(field, weaponData);
        
        // 베이스 클래스 공통 로직 사용
        LaunchPolar(field, weaponData, angleDeg, startRadius, speed);
    }
    
    // 3. 업데이트 로직 (충돌 감지 자동)
    protected override void OnUpdate(float deltaTime)
    {
        if (!_isActive || _field == null) return;
        
        // 베이스 클래스 충돌 감지 사용
        if (UpdatePolarMovementAndCheckWallCollision(deltaTime, 
            out int sectorIndex, out Vector2 position))
        {
            // 충돌 시 처리
            OnWallHit(sectorIndex, position);
        }
    }
    
    // 4. 충돌 처리 (각 투사체별 고유 로직)
    private void OnWallHit(int sectorIndex, Vector2 position)
    {
        // 폭발, 효과, 데미지 등
        ReturnToPool();
    }
    
    // 5. 정리
    protected override void OnPoolReturn()
    {
        // 시각 요소 정리 등 (극좌표 필드는 베이스에서 처리)
    }
}
```

## 실제 예시: PolarGravityProjectile

### Before (중복 코드)
```csharp
// 개별 필드
private float _angleDeg;
private float _radius;
private float _speed;

// 위치 업데이트
private void UpdatePosition()
{
    float angleRad = _angleDeg * Mathf.Deg2Rad;
    Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    Vector3 center = (_field as Component).transform.position;
    transform.position = center + (Vector3)(dir * _radius);
}

// 충돌 감지
protected override void OnUpdate(float deltaTime)
{
    _radius += _speed * deltaTime;
    UpdatePosition();
    
    float angleRad = _angleDeg * Mathf.Deg2Rad;
    Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    Vector2 currentPos = (Vector2)(_field as Component).transform.position + dir * _radius;
    
    int sectorIndex = _field.AngleToSectorIndex(_angleDeg);
    float sectorRadius = _field.GetSectorRadius(sectorIndex);
    
    if (_radius >= sectorRadius - 0.1f)
    {
        Explode(sectorIndex, currentPos);
    }
}
```

### After (공통 기능 사용)
```csharp
// 베이스 필드 사용 (선언 불필요)

// Launch에서 공통 로직 호출
public void Launch(IPolarField field, PolarWeaponData weaponData, 
                  float angleDeg, float startRadius, float speed)
{
    Launch(field, weaponData);
    LaunchPolar(field, weaponData, angleDeg, startRadius, speed); // ✅ 공통 로직
}

// 충돌 감지 간소화
protected override void OnUpdate(float deltaTime)
{
    if (!_isActive || _field == null || _hasExploded) return;
    
    // ✅ 한 줄로 이동 + 충돌 감지
    if (UpdatePolarMovementAndCheckWallCollision(deltaTime, 
        out int sectorIndex, out Vector2 position))
    {
        Explode(sectorIndex, position);
    }
}
```

**결과**: 약 40줄 → 10줄로 감소, 가독성 향상

## 다른 투사체 적용 예시

### 1. 일반 폭발 투사체
```csharp
public class PolarExplosiveProjectile : PolarProjectileBase
{
    protected override void OnUpdate(float deltaTime)
    {
        if (UpdatePolarMovementAndCheckWallCollision(deltaTime, 
            out int sectorIndex, out Vector2 position))
        {
            // 폭발 데미지 적용
            ApplyExplosionDamage(sectorIndex, position);
            ReturnToPool();
        }
    }
}
```

### 2. 관통 투사체
```csharp
public class PolarPiercingProjectile : PolarProjectileBase
{
    private int _pierceCount = 0;
    private int _maxPierces = 3;
    
    protected override void OnUpdate(float deltaTime)
    {
        if (UpdatePolarMovementAndCheckWallCollision(deltaTime, 
            out int sectorIndex, out Vector2 position))
        {
            ApplyDamage(sectorIndex);
            _pierceCount++;
            
            if (_pierceCount >= _maxPierces)
            {
                ReturnToPool();
            }
            else
            {
                // 관통 계속 (충돌 플래그 리셋)
                _hasReachedWall = false;
            }
        }
    }
}
```

### 3. 추적 투사체
```csharp
public class PolarHomingProjectile : PolarProjectileBase
{
    private Transform _target;
    
    protected override void OnUpdate(float deltaTime)
    {
        // 타겟 추적 (각도 조정)
        if (_target != null)
        {
            Vector2 toTarget = _target.position - transform.position;
            float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            _angleDeg = Mathf.MoveTowardsAngle(_angleDeg, targetAngle, 90f * deltaTime);
        }
        
        // 공통 충돌 감지
        if (UpdatePolarMovementAndCheckWallCollision(deltaTime, 
            out int sectorIndex, out Vector2 position))
        {
            ApplyDamage(sectorIndex);
            ReturnToPool();
        }
    }
}
```

## 확장 가능성

### 1. 속도 변경
```csharp
// 이동 중 가속
_speed += acceleration * deltaTime;
```

### 2. 각도 변경
```csharp
// 곡선 이동
_angleDeg += rotationSpeed * deltaTime;
UpdatePolarPosition(); // 수동 위치 갱신
```

### 3. 조기 충돌 체크
```csharp
// 적과의 충돌 등
int currentSector = GetCurrentSectorIndex();
if (IsEnemyInSector(currentSector))
{
    OnEnemyHit();
}

// 벽 충돌은 여전히 자동
if (UpdatePolarMovementAndCheckWallCollision(...))
```

## 주의사항

1. **LaunchPolar 호출 필수**: 극좌표 이동을 사용하려면 반드시 호출
2. **UpdatePolarMovementAndCheckWallCollision**: 매 프레임 OnUpdate에서 호출
3. **_hasReachedWall**: 충돌 후 관통 등 특수 동작 시 수동 리셋 필요
4. **OnPoolReturn**: 베이스 클래스가 극좌표 필드 정리, 시각 요소만 처리

## 성능 이점

- 중복 코드 제거로 IL2CPP 최적화 향상
- 공통 로직 버그 수정 시 모든 투사체에 자동 반영
- 새 투사체 구현 시간 단축 (핵심 로직만 집중)

## 관련 파일
- `PolarProjectileBase.cs` - 공통 기능
- `PolarGravityProjectile.cs` - ✅ 적용 완료 (중력 제어)
- `PolarMissileProjectile.cs` - ✅ 적용 완료 (폭발)
- `PolarMachinegunProjectile.cs` - ✅ 적용 완료 (단일 타격)
- `PolarLaserProjectile.cs` - 레이저 (다른 패턴, 연속 빔)

