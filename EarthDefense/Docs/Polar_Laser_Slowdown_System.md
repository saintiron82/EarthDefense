# 적 필드 이동 속도 둔화 시스템 (중력장)

## 개요
특정 섹터 영역의 중력 효과를 둔화시켜 적 필드가 천천히 수축하도록 만드는 시스템입니다.
중력장처럼 특정 구역에서 라인이 느리게 밀려들어와 방어 시간을 확보할 수 있습니다.

## 구현 내용

### 1. IPolarField 인터페이스 확장
```csharp
float GetSectorSpeedMultiplier(int sectorIndex);
```
- 특정 섹터의 속도 배율 조회

### 2. PolarFieldController 확장
- **필드**: `_speedMultipliers[]` - 섹터별 속도 배율 저장
- **중력 시뮬레이션**: `ApplyGravity` 메서드에서 섹터별 속도 배율 적용
  - 기본 중력 수축: `newRadius = _sectorRadii[i] - (reductionAmount * speedMultiplier)`
  - 둔화 영역에서는 중력 효과가 감소하여 라인이 천천히 수축됨
  
- **메서드**:
  - `GetSectorSpeedMultiplier(int sectorIndex)` - 속도 배율 조회
  - `SetSectorSpeedMultiplier(int sectorIndex, float multiplier)` - 속도 배율 설정
  - `SetSpeedMultiplierForAngleRange(float startAngle, float endAngle, float multiplier)` - 각도 범위로 속도 배율 설정

### 3. 레이저 파티클 둔화 (부가 효과)
- PolarLaserProjectile에서도 동일한 속도 배율을 적용하여 레이저 빔도 둔화 영역에서 느려짐

## 사용 방법

### 기본 사용
```csharp
// 특정 섹터의 중력을 50%로 둔화 (라인이 절반 속도로 밀려옴)
polarFieldController.SetSectorSpeedMultiplier(90, 0.5f);

// 45도~135도 범위를 30% 속도로 둔화 (거의 정지)
polarFieldController.SetSpeedMultiplierForAngleRange(45f, 135f, 0.3f);
```

### 예제 컴포넌트 사용
1. PolarFieldController가 있는 GameObject에 `LaserSlowdownZoneExample` 추가
2. Inspector에서 설정:
   - **Slowdown Start Angle**: 둔화 시작 각도 (기본 45°)
   - **Slowdown End Angle**: 둔화 종료 각도 (기본 135°)
   - **Speed Multiplier**: 속도 배율 (0.3 = 70% 둔화)
   - **Enable Dynamic Rotation**: 영역 회전 활성화
   - **Rotation Speed**: 회전 속도 (도/초)

### 속도 배율 값
- `1.0` = 정상 중력 속도 (기본)
- `0.5` = 50% 둔화 (라인이 절반 속도로 수축)
- `0.3` = 70% 둔화 (느린 이동)
- `0.1` = 90% 둔화 (거의 정지)
- `0.0` = 완전 정지 (해당 영역 중력 무효화)

## 게임플레이 활용

### 중력 제어 포탄 전술
```csharp
// 위험한 섹터에 긴급 방어
public class EmergencyDefense : MonoBehaviour
{
    [SerializeField] private PolarFieldController field;
    [SerializeField] private GameObject gravityProjectilePrefab;
    [SerializeField] private PolarGravityWeaponData weaponData;
    
    public void DeployEmergencyField(int dangerSectorIndex)
    {
        float angle = field.SectorIndexToAngle(dangerSectorIndex);
        
        var proj = Instantiate(gravityProjectilePrefab).GetComponent<PolarGravityProjectile>();
        proj.Launch(field, weaponData, angle, 0.8f, 8f);
        
        Debug.Log($"Emergency gravity field deployed at sector {dangerSectorIndex}");
    }
}
```

### GravityFieldManager 사용
```csharp
// 직접 중력장 생성 (포탄 없이)
GravityFieldManager manager = fieldController.GetComponent<GravityFieldManager>();
manager.CreateGravityField(
    centerSectorIndex: 90,
    radius: 15,
    speedMultiplier: 0.1f,  // 90% 둔화
    duration: 10f,          // 10초 지속
    useGaussianFalloff: true
);
```

### 방어 타워 효과
```csharp
// 특정 타워 주변 영역을 둔화 필드로 설정
public class DefenseTower : MonoBehaviour
{
    [SerializeField] private PolarFieldController field;
    [SerializeField] private float coverageAngle = 60f; // 커버 각도
    [SerializeField] private float slowdownRate = 0.2f; // 80% 둔화
    
    private void Update()
    {
        // 타워가 바라보는 방향 계산
        Vector2 dir = transform.right;
        float centerAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        
        // 주변 영역 둔화
        field.SetSpeedMultiplierForAngleRange(
            centerAngle - coverageAngle * 0.5f,
            centerAngle + coverageAngle * 0.5f,
            slowdownRate
        );
    }
}
```

### 스킬 효과 (중력장)
```csharp
public class GravityFieldSkill : MonoBehaviour
{
    public IEnumerator ActivateGravityField(PolarFieldController field, float centerAngle, float duration)
    {
        // 중력장 활성화 (해당 영역 90% 둔화)
        field.SetSpeedMultiplierForAngleRange(centerAngle - 30f, centerAngle + 30f, 0.1f);
        
        yield return new WaitForSeconds(duration);
        
        // 중력장 해제
        field.SetSpeedMultiplierForAngleRange(centerAngle - 30f, centerAngle + 30f, 1f);
    }
}
```

### 회전하는 둔화 영역
```csharp
public class RotatingSlowdownZone : MonoBehaviour
{
    [SerializeField] private PolarFieldController field;
    [SerializeField] private float zoneWidth = 45f;
    [SerializeField] private float rotationSpeed = 30f;
    private float currentAngle = 0f;
    
    private void Update()
    {
        currentAngle += rotationSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;
        
        field.SetSpeedMultiplierForAngleRange(
            currentAngle,
            currentAngle + zoneWidth,
            0.2f
        );
    }
}
```

## 주의사항
- 속도 배율은 0.0~2.0 범위로 자동 클램핑됨
- 중력 수축에 직접 적용되므로 모든 모드에서 작동
- 매 프레임 `ApplyGravity`에서 적용되므로 실시간 변경 가능
- 둔화 영역 설정 시 기존 영역은 초기화됨 (예제 스크립트 참고)

## 성능 고려사항
- 섹터별로 독립 적용되므로 성능 영향 미미
- 중력 계산 시에만 참조되므로 오버헤드 최소화
- 동적으로 변경 가능하여 실시간 게임플레이 조정 가능

