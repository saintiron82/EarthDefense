# Effect 독립 데이터 시스템 가이드

## 개요

Effect는 **무기와 완전히 독립된 데이터 시스템**입니다.
- ScriptableObject로 에셋 생성
- JSON으로 내보내기/가져오기 가능
- Inspector에서 직접 JSON 편집 가능
- 무기 데이터처럼 모듈화된 관리
- **발동 시점을 자유롭게 설정 가능** ⭐

## Effect 발동 시점 (Trigger Types)

### 지원되는 발동 시점

```csharp
public enum EffectTriggerType
{
    OnImpact,       // 충돌 시 (벽에 닿을 때) - 기본값
    OnLaunch,       // 발사 시 (투사체 생성 즉시)
    OnDestroy,      // 소멸 시 (투사체가 사라질 때)
    OnInterval,     // 주기적 (N초마다)
    OnDistance,     // 거리 기반 (N만큼 이동할 때마다)
    OnTime,         // 시간 기반 (발사 후 N초 뒤 1회)
    OnPenetrate,    // 관통 시 (적을 뚫을 때)
    OnKill,         // 적 처치 시
    OnLowHealth,    // 저체력 시
    OnCharge,       // 충전 완료 시
    Manual          // 수동 (외부 호출)
}
```

### 발동 조건 설정

```
[Trigger Condition]
- Trigger Type: OnImpact (발동 시점)
- Probability: 1.0 (발동 확률, 0~1)
- Delay: 0 (발동 지연, 초)
- Interval: 1 (주기, OnInterval용)
- Distance Step: 5 (거리 간격, OnDistance용)
- Trigger Time: 2 (발동 시간, OnTime용)
- Max Trigger Count: 1 (최대 발동 횟수, 0=무제한)
- Cooldown: 0 (쿨다운, 초)
```

## 발동 시점별 사용 예시

### 1. OnImpact (충돌 시)
```json
{
  "triggerType": "OnImpact",
  "probability": 1.0,
  "delay": 0,
  "maxTriggerCount": 1
}
```
**용도**: 벽에 닿을 때 중력장 생성, 폭발 등
**예시**: 일반 중력 미사일

### 2. OnLaunch (발사 시)
```json
{
  "triggerType": "OnLaunch",
  "probability": 1.0,
  "delay": 0,
  "maxTriggerCount": 1
}
```
**용도**: 발사 즉시 버프, 발사 지점에 효과 생성
**예시**: 
- 발사 지점에 화염 생성
- 발사 시 자가 버프

### 3. OnInterval (주기적)
```json
{
  "triggerType": "OnInterval",
  "interval": 0.5,
  "probability": 1.0,
  "maxTriggerCount": 0
}
```
**용도**: 이동 중 지속적으로 효과 발동
**예시**:
- 0.5초마다 작은 폭발
- 이동 경로에 화염 흔적
- 주기적 독 살포

### 4. OnDistance (거리 기반)
```json
{
  "triggerType": "OnDistance",
  "distanceStep": 3.0,
  "probability": 1.0,
  "maxTriggerCount": 0
}
```
**용도**: 일정 거리마다 효과 발동
**예시**:
- 3m마다 작은 중력장 생성
- 경로에 지뢰 설치

### 5. OnTime (시간 기반)
```json
{
  "triggerType": "OnTime",
  "triggerTime": 2.0,
  "probability": 1.0,
  "maxTriggerCount": 1
}
```
**용도**: 발사 후 특정 시간에 1회 발동
**예시**:
- 2초 후 공중 폭발
- 지연 중력장

### 6. OnDestroy (소멸 시)
```json
{
  "triggerType": "OnDestroy",
  "probability": 1.0,
  "maxTriggerCount": 1
}
```
**용도**: 투사체가 사라질 때 효과
**예시**:
- 소멸 위치에 폭발
- 마지막 위치에 중력장

### 7. 확률 기반 (50% 발동)
```json
{
  "triggerType": "OnImpact",
  "probability": 0.5,
  "maxTriggerCount": 1
}
```
**용도**: 랜덤 효과
**예시**: 50% 확률로 중력장 생성

### 8. 쿨다운 (재발동 대기)
```json
{
  "triggerType": "OnInterval",
  "interval": 0.1,
  "cooldown": 1.0,
  "maxTriggerCount": 0
}
```
**용도**: 너무 자주 발동 방지
**예시**: 0.1초마다 체크하지만 1초 쿨다운

### 9. 지연 발동
```json
{
  "triggerType": "OnImpact",
  "delay": 1.5,
  "probability": 1.0
}
```
**용도**: 충돌 후 잠시 뒤 발동
**예시**: 
- 충돌 1.5초 후 폭발
- 지연 중력장

### 10. 제한 발동
```json
{
  "triggerType": "OnInterval",
  "interval": 0.5,
  "maxTriggerCount": 5
}
```
**용도**: 최대 N회만 발동
**예시**: 0.5초마다 최대 5회만 발동

## 복합 효과 조합 예시

### 예시 1: 연막탄
```
Projectile: Bullet
Effects: [2]
├── Effect 1: Smoke (OnLaunch)
│   └── 발사 즉시 연막 생성
└── Effect 2: Smoke (OnInterval, 0.2s)
    └── 0.2초마다 연막 생성 (경로 추적)
```

### 예시 2: 클러스터 미사일
```
Projectile: Missile
Effects: [3]
├── Effect 1: SubMissile (OnTime, 2s)
│   └── 2초 후 자탄 분리
├── Effect 2: Explosion (OnImpact)
│   └── 충돌 시 폭발
└── Effect 3: Fire (OnDestroy)
    └── 소멸 시 화염
```

### 예시 3: 추적 중력탄
```
Projectile: Bullet
Effects: [2]
├── Effect 1: GravityField (OnDistance, 5m)
│   └── 5m마다 작은 중력장
└── Effect 2: GravityField_Strong (OnImpact)
    └── 충돌 시 강력한 중력장
```

### 예시 4: 시한폭탄
```
Projectile: Grenade
Effects: [2]
├── Effect 1: Explosion (OnTime, 3s)
│   └── 3초 후 폭발
└── Effect 2: Shrapnel (OnTime, 3.1s)
    └── 3.1초 후 파편 (약간의 지연)
```

### 예시 5: 랜덤 효과탄
```
Projectile: Bullet
Effects: [3]
├── Effect 1: Fire (OnImpact, 33% 확률)
├── Effect 2: Ice (OnImpact, 33% 확률)
└── Effect 3: Poison (OnImpact, 33% 확률)
```

## Inspector 설정 예시

### 중력장 (충돌 시)
```
[Trigger Condition]
- Trigger Type: OnImpact
- Probability: 1.0
- Delay: 0
- Max Trigger Count: 1
```

### 화염 흔적 (주기적)
```
[Trigger Condition]
- Trigger Type: OnInterval
- Interval: 0.3
- Probability: 1.0
- Max Trigger Count: 10
```

### 시한 폭발 (시간 기반)
```
[Trigger Condition]
- Trigger Type: OnTime
- Trigger Time: 2.5
- Probability: 1.0
- Max Trigger Count: 1
```

## JSON 형식 (발동 조건 포함)

```json
{
  "effectId": "gravity_interval",
  "effectName": "Periodic Gravity Trail",
  "triggerCondition": {
    "triggerType": "OnInterval",
    "probability": 1.0,
    "delay": 0.0,
    "interval": 0.5,
    "distanceStep": 5.0,
    "triggerTime": 2.0,
    "maxTriggerCount": 10,
    "cooldown": 0.0
  },
  "fieldRadius": 5,
  "speedMultiplier": 0.3,
  "duration": 3.0,
  "useGaussianFalloff": true,
  "fieldColor": [0.5, 0.8, 1.0, 0.5]
}
```

## 구조

```
PolarEffectBase (베이스 클래스)
├── effectId (고유 ID)
├── effectName (표시 이름)
├── ToJson() (JSON 내보내기)
└── FromJson() (JSON 가져오기)

PolarGravityFieldEffect : PolarEffectBase
├── (상속) effectId, effectName
├── fieldRadius, speedMultiplier, duration
├── (구현) ToJson(), FromJson()
└── (구현) OnImpact()

(미래) PolarFireEffect : PolarEffectBase
(미래) PolarPoisonEffect : PolarEffectBase
...
```

## 사용 방법

### 1. Unity Inspector에서 생성

```
Project 창 우클릭
→ Create → EarthDefense → Polar → Effects → Gravity Field
```

**Inspector 설정**:
```
[Effect ID]
- Effect Id: gravity_standard
- Effect Name: Standard Gravity Field

[Gravity Field Settings]
- Field Radius: 10
- Speed Multiplier: 0.2
- Duration: 5
- Use Gaussian Falloff: ✓

[Visual]
- Field Color: (0.5, 0.8, 1.0, 0.5)
```

### 2. JSON으로 내보내기

#### 방법 A: Inspector에서
```
1. Effect 에셋 선택
2. Inspector 하단 "JSON 보기/편집" 펼치기
3. "현재 데이터 → JSON 생성" 클릭
4. "파일로 내보내기" 클릭
```

#### 방법 B: 메뉴에서
```
1. Effect 에셋 선택 (여러 개 가능)
2. 메뉴: Assets → Polar Effects → Export Selected to JSON
3. Assets/Polar/Data/Effects/Exported/ 에 저장됨
```

#### 방법 C: 모든 Effect 일괄 내보내기
```
메뉴: Assets → Polar Effects → Export All to JSON
```

### 3. JSON 형식

```json
{
  "effectId": "gravity_standard",
  "effectName": "Standard Gravity Field",
  "fieldRadius": 10,
  "speedMultiplier": 0.2,
  "duration": 5.0,
  "useGaussianFalloff": true,
  "fieldColor": [0.5, 0.8, 1.0, 0.5]
}
```

### 4. JSON에서 가져오기

#### 방법 A: Inspector에서
```
1. Effect 에셋 선택
2. Inspector에서 JSON 텍스트 에디터에 JSON 붙여넣기
3. "JSON → 데이터 적용" 클릭
```

#### 방법 B: 단일 파일 가져오기
```
메뉴: Assets → Polar Effects → Import from JSON
파일 선택 → 자동으로 새 Effect 에셋 생성
```

#### 방법 C: 폴더 일괄 가져오기
```
메뉴: Assets → Polar Effects → Bulk Import from Folder
폴더 선택 → 모든 JSON 파일 자동 임포트
```

## 메뉴 목록

### Assets → Polar Effects

```
├── Export Selected to JSON       (선택한 Effect들 내보내기)
├── Export All to JSON            (모든 Effect 내보내기)
├── Import from JSON              (JSON 파일 1개 가져오기)
├── Bulk Import from Folder       (폴더의 모든 JSON 가져오기)
└── Open Export Folder            (내보내기 폴더 열기)
```

## Inspector 기능

Effect 에셋을 선택하면 Inspector 하단에 추가 기능이 표시됩니다:

### JSON 보기/편집 섹션

```
[현재 데이터 → JSON 생성]
현재 설정값을 JSON으로 변환하여 텍스트 영역에 표시

[JSON 텍스트 에디어]
200줄 스크롤 가능한 텍스트 에리어
- 복사/붙여넣기 가능
- 직접 편집 가능

[JSON → 데이터 적용]
텍스트 에리어의 JSON을 파싱하여 현재 Effect에 적용

[클립보드 복사]
JSON을 시스템 클립보드에 복사

[파일로 내보내기]
JSON을 파일로 저장 (저장 대화상자)
```

### Effect 정보 섹션

```
ID: gravity_standard
Name: Standard Gravity Field
Type: PolarGravityFieldEffect
```

## 워크플로우 예시

### 시나리오 1: Effect 복제 및 수정

```
1. 기존 Effect 선택 (예: GravityField_Standard)
2. Inspector에서 "현재 데이터 → JSON 생성"
3. "클립보드 복사"
4. 새 Effect 생성 (Create → Effects → Gravity Field)
5. 새 Effect의 Inspector에서 JSON 붙여넣기
6. effectId와 effectName 수정
7. 원하는 값 수정 (예: fieldRadius: 20)
8. "JSON → 데이터 적용" 클릭
9. 완료! 새 Effect 생성됨
```

### 시나리오 2: 외부 툴로 대량 생성

```
1. Effect 1개를 JSON으로 내보내기
2. 외부 에디터/스크립트로 JSON 복사 및 수정
   - gravity_weak.json
   - gravity_medium.json
   - gravity_strong.json
3. "Bulk Import from Folder"로 일괄 임포트
4. 완료! 여러 Effect가 자동 생성됨
```

### 시나리오 3: 버전 관리

```
1. "Export All to JSON" 실행
2. Exported 폴더를 Git에 커밋
3. 팀원이 최신 JSON 가져오기
4. "Bulk Import from Folder"로 최신 Effect 동기화
```

## Effect 프리셋 JSON 예시

### gravity_weak.json
```json
{
  "effectId": "gravity_weak",
  "effectName": "Weak Gravity",
  "fieldRadius": 5,
  "speedMultiplier": 0.5,
  "duration": 2.0,
  "useGaussianFalloff": false,
  "fieldColor": [0.6, 0.9, 1.0, 0.3]
}
```

### gravity_strong.json
```json
{
  "effectId": "gravity_strong",
  "effectName": "Strong Gravity",
  "fieldRadius": 20,
  "speedMultiplier": 0.1,
  "duration": 10.0,
  "useGaussianFalloff": true,
  "fieldColor": [0.3, 0.5, 1.0, 0.8]
}
```

### gravity_burst.json
```json
{
  "effectId": "gravity_burst",
  "effectName": "Burst Gravity",
  "fieldRadius": 3,
  "speedMultiplier": 0.6,
  "duration": 1.0,
  "useGaussianFalloff": false,
  "fieldColor": [0.7, 1.0, 1.0, 0.4]
}
```

## 새 Effect 타입 추가 방법

### 1. Effect 클래스 작성

```csharp
[CreateAssetMenu(fileName = "FireEffect", menuName = "EarthDefense/Polar/Effects/Fire")]
public class PolarFireEffect : PolarEffectBase
{
    [SerializeField] private float fireDamage = 5f;
    [SerializeField] private float duration = 3f;
    [SerializeField] private float tickRate = 1f;
    
    public override void OnImpact(IPolarField field, int sectorIndex, Vector2 position, PolarWeaponData weaponData)
    {
        // 화염 효과 로직
    }
    
    public override string ToJson(bool prettyPrint = true)
    {
        var data = new FireEffectJson {
            effectId = this.effectId,
            effectName = this.effectName,
            fireDamage = this.fireDamage,
            duration = this.duration,
            tickRate = this.tickRate
        };
        return JsonUtility.ToJson(data, prettyPrint);
    }
    
    public override void FromJson(string json)
    {
        var data = JsonUtility.FromJson<FireEffectJson>(json);
        this.effectId = data.effectId;
        this.effectName = data.effectName;
        this.fireDamage = data.fireDamage;
        this.duration = data.duration;
        this.tickRate = data.tickRate;
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    [Serializable]
    private class FireEffectJson
    {
        public string effectId;
        public string effectName;
        public float fireDamage;
        public float duration;
        public float tickRate;
    }
}
```

### 2. PolarEffectJsonUtility 업데이트

```csharp
// ImportFromJson() 메서드에 추가
if (tempData.effectId.StartsWith("gravity"))
{
    effect = ScriptableObject.CreateInstance<PolarGravityFieldEffect>();
}
else if (tempData.effectId.StartsWith("fire"))  // ⭐ 추가
{
    effect = ScriptableObject.CreateInstance<PolarFireEffect>();
}
```

### 3. 완료!

```
Create → Polar → Effects → Fire
→ JSON 내보내기/가져오기 자동 지원
```

## 무기와 Effect 조합

### 1. Effect 생성
```
gravity_standard.json
fire_dot.json
poison_slow.json
```

### 2. 무기 데이터에 추가
```
PolarMissileWeaponData "SuperMissile"
└── Impact Effects: [3]
    ├── GravityFieldEffect_Standard (gravity_standard에서 생성)
    ├── FireEffect_DOT (fire_dot에서 생성)
    └── PoisonEffect_Slow (poison_slow에서 생성)
```

### 3. JSON으로 무기 + Effect 세트 관리
```
weapons/
├── super_missile.json (무기)
effects/
├── gravity_standard.json (효과 1)
├── fire_dot.json (효과 2)
└── poison_slow.json (효과 3)
```

## 장점

### 1. 완전한 독립성
- Effect는 무기와 독립적
- 다른 프로젝트에도 재사용 가능
- Effect만 별도로 버전 관리

### 2. 데이터 기반
- 코드 수정 없이 JSON 편집
- 외부 툴로 대량 생성
- 디자이너가 직접 밸런스 조정

### 3. 모듈화
- 효과 조합 자유로움
- 프리셋 라이브러리 구축
- 재사용성 극대화

### 4. 버전 관리 용이
- Git에 JSON 커밋
- Diff로 변경사항 추적
- 팀원 간 동기화 쉬움

## 다음 단계

### 추가할 Effect 타입
```
PolarFireEffect (화염 지속 피해)
PolarPoisonEffect (독 DoT)
PolarFreezeEffect (빙결 정지)
PolarExplosionChainEffect (연쇄 폭발)
PolarTeleportEffect (텔레포트)
PolarShieldEffect (보호막 생성)
PolarHealEffect (아군 회복)
```

모두 동일한 시스템으로 관리됩니다!

