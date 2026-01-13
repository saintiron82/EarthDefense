# Polar 무기 생성 에이전트 가이드

## 개요

이 문서는 Polar 무기 시스템의 JSON 생성을 위한 AI 에이전트 가이드입니다.
에이전트는 사용자 요청에 따라 최적의 무기 설정값을 생성합니다.

---

## 게임 컨셉

- **EarthDefense**: 지구 방어 게임
- **Polar 좌표계**: 180개의 섹터가 지구 주위를 감싸고 있음
- **목표**: 무기로 적(청크)을 밀어내어 섹터 경계를 확장

---

## 무기 타입 및 특성

### 1. Laser (Drill 타입)
**컨셉**: 좁은 곳을 꾸준히 지지는 용접기
**전술**: 약점 집중 타격, 장시간 유지

| 필드 | 타입 | 범위 | 설명 |
|------|------|------|------|
| extendSpeed | float | 10-100 | 빔 확장 속도 (빠를수록 즉시 타격) |
| retractSpeed | float | 10-100 | 빔 수축 속도 |
| maxLength | float | 20-100 | 빔 최대 길이 (긴수록 원거리) |
| beamWidth | float | 0.1-5.0 | 빔 두께 (두꺼울수록 다중 섹터 타격) |
| beamColor | [r,g,b,a] | 0-1 | 빔 색상 |
| duration | float | 0-10 | 지속 시간 (0=무한) |
| reflectCount | int | 0-10 | 반사 횟수 (0=반사 안함) |
| reflectDamageMultiplier | float | 0.1-1.5 | 반사 시 데미지 배율 (1.0=100%) |
| reflectAngleRange | float | 0-180 | 반사 각도 범위 (degree, 0=직각 반사만) |

**밸런스 가이드라인**:
- 고DPS + 좁은 빔 = 보스 킬러
- 저DPS + 넓은 빔 = 영역 제어
- extendSpeed가 높으면 즉발형, 낮으면 차징형
- reflectCount 활용 시 다중 타겟 공격 가능 (Tier 3+)

### 2. Machinegun (Ripper 타입)
**컨셉**: 표면을 긁어내고 상처를 벌려놓는 느낌
**전술**: 약점 확장, 영역 무력화

| 필드 | 타입 | 범위 | 설명 |
|------|------|------|------|
| fireRate | float | 1-30 | 연사 속도 (발/초) |
| projectileSpeed | float | 5-50 | 투사체 속도 |
| spreadAngle | float | 0-15 | 산포도 각도 (높을수록 넓게 퍼짐) |
| projectileLifetime | float | 1-10 | 투사체 수명 |
| projectileScale | float | 0.1-1.0 | 투사체 크기 |
| projectileColor | [r,g,b,a] | 0-1 | 투사체 색상 |

**밸런스 가이드라인**:
- 고연사 + 낮은 데미지 = 지속 압박
- 저연사 + 높은 데미지 = 정밀 타격
- spreadAngle 높으면 샷건 느낌

### 3. Missile (Hammer 타입)
**컨셉**: 쾅! 하고 넓은 범위를 한 번에 밀어내는 묵직함
**전술**: 대규모 후퇴, 긴급 공간 확보

| 필드 | 타입 | 범위 | 설명 |
|------|------|------|------|
| fireRate | float | 0.1-2.0 | 발사 속도 (발/초) |
| missileSpeed | float | 5-30 | 미사일 속도 |
| missileLifetime | float | 2-10 | 미사일 수명 |
| coreRadius | int | 0-5 | 폭심 반경 (풀 데미지) |
| effectiveRadius | int | 3-15 | 유효 반경 (의미있는 데미지) |
| maxRadius | int | 5-25 | 최대 영향 반경 |
| coreMultiplier | float | 0.8-1.5 | 폭심 데미지 배율 |
| effectiveMinMultiplier | float | 0.5-1.0 | 유효 범위 최소 배율 |
| maxMinMultiplier | float | 0.0-0.5 | 최대 범위 최소 배율 |
| falloffType | string | "Linear"/"Smooth"/"Exponential" | 감쇠 곡선 |
| missileScale | float | 0.2-2.0 | 미사일 크기 |
| missileColor | [r,g,b,a] | 0-1 | 미사일 색상 |

**밸런스 가이드라인**:
- 큰 반경 + 낮은 데미지 = 군중 제어
- 작은 반경 + 높은 데미지 = 정밀 폭격
- Smooth 감쇠가 가장 자연스러움

### 4. Bullet (단발 타입)
**컨셉**: 정밀한 단발 사격
**전술**: 정확한 타격

| 필드 | 타입 | 범위 | 설명 |
|------|------|------|------|
| bulletColor | [r,g,b,a] | 0-1 | 탄환 색상 |
| bulletScale | float | 0.05-0.5 | 탄환 크기 |
| bulletSpeed | float | 5-20 | 탄환 속도 |
| fireSoundId | string | - | 발사 사운드 ID |
| impactSoundId | string | - | 충돌 사운드 ID |

---

## 공통 필드 (모든 무기)

| 필드 | 타입 | 범위 | 설명 |
|------|------|------|------|
| id | string | - | 고유 ID (영문, 언더스코어) |
| weaponName | string | - | 표시 이름 |
| weaponBundleId | string | - | 무기 프리팹 번들 ID |
| projectileBundleId | string | - | 투사체 번들 ID |
| damage | float | 1-10000 | 초당 데미지 (DPS) |
| knockbackPower | float | 0.1-2.0 | 넉백 강도 |
| areaType | string | "Fixed"/"Linear"/"Gaussian"/"Explosion" | 타격 범위 타입 |
| damageRadius | int | 0-10 | 피해 반경 (섹터 수) |
| useGaussianFalloff | bool | - | 가우시안 감쇠 사용 여부 |
| woundIntensity | float | 0-1 | 상처 강도 |
| tickRate | float | 1-60 | 초당 타격 횟수 |

### AreaType 설명
- **Fixed**: 단일 섹터 (레이저 기본)
- **Linear**: 거리 비례 감쇠 (불릿)
- **Gaussian**: 가우시안 분포 감쇠 (머신건)
- **Explosion**: 폭발 감쇠 (미사일)

---

## JSON 생성 규칙

### 파일 위치
`Assets/Polar/RES/` 폴더에 `.json` 확장자로 저장

### 파일명 규칙
`{WeaponName}.json` (예: `PlasmaLaser.json`)

### JSON 구조 (평면 구조 권장)
```json
{
    "id": "weapon_id",
    "weaponName": "Weapon Name",
    "type": "laser",
    "damage": 100,
    ...무기별 필드...
}
```

### type 필드
무기 타입을 명시하면 자동 감지됨:
- `"type": "laser"`
- `"type": "machinegun"`
- `"type": "missile"`
- `"type": "bullet"`

---

## 밸런스 티어 시스템

### Tier 1 (기본)
- damage: 50-200
- 특수 효과 없음
- 단순한 메카닉

### Tier 2 (고급)
- damage: 200-500
- 약간의 특수 효과
- 조합 가능한 메카닉

### Tier 3 (희귀)
- damage: 500-1000
- 강력한 특수 효과
- 복잡한 메카닉

### Tier 4 (전설)
- damage: 1000-5000
- 게임 체인저 효과
- 고유한 메카닉

---

## 무기 컨셉 예시

### 빠른 연사 레이저
```json
{
    "id": "rapid_laser",
    "weaponName": "Rapid Laser",
    "type": "laser",
    "damage": 100,
    "tickRate": 30,
    "extendSpeed": 100,
    "beamWidth": 0.5,
    "beamColor": [0, 1, 1, 1]
}
```

### 반사 레이저 (다중 타겟)
```json
{
    "id": "reflect_laser",
    "weaponName": "Reflect Laser",
    "type": "laser",
    "damage": 150,
    "tickRate": 20,
    "extendSpeed": 80,
    "beamWidth": 0.3,
    "beamColor": [0.8, 0.8, 1, 1],
    "reflectCount": 3,
    "reflectDamageMultiplier": 0.7,
    "reflectAngleRange": 45
}
```

### 광역 폭발 미사일
```json
{
    "id": "cluster_missile",
    "weaponName": "Cluster Missile",
    "type": "missile",
    "damage": 300,
    "fireRate": 0.3,
    "coreRadius": 2,
    "effectiveRadius": 8,
    "maxRadius": 15,
    "falloffType": "Smooth"
}
```

### 고속 관통 머신건
```json
{
    "id": "piercing_mg",
    "weaponName": "Piercing Machinegun",
    "type": "machinegun",
    "damage": 80,
    "fireRate": 20,
    "projectileSpeed": 40,
    "spreadAngle": 1,
    "projectileColor": [1, 0.5, 0, 1]
}
```

---

## 색상 가이드

| 속성 | 추천 색상 |
|------|----------|
| 화염 | [1, 0.3, 0, 1] |
| 얼음 | [0.5, 0.8, 1, 1] |
| 전기 | [0.8, 0.8, 1, 1] |
| 독 | [0.2, 1, 0.2, 1] |
| 플라즈마 | [1, 0, 1, 1] |
| 일반 | [1, 1, 0, 1] |

---

## 에이전트 사용 예시

사용자: "보스전용 고데미지 레이저 만들어줘"
→ 좁은 빔, 높은 DPS, 긴 지속시간

사용자: "초보자용 쉬운 머신건"
→ 넓은 산포, 적당한 연사, 시각적으로 화려

사용자: "긴급 탈출용 미사일"
→ 넓은 폭발 반경, 강한 넉백, 빠른 속도

---

## 출력 경로

생성된 JSON 파일은 다음 경로에 저장:
`Assets/Polar/RES/{WeaponName}.json`

Unity에서 우클릭 → "Assets > Polar > Create Weapon from JSON"으로 에셋 변환
