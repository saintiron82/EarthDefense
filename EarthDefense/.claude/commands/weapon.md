---
description: Polar 무기 JSON 생성 - 요청에 맞는 무기 데이터를 생성합니다
allowed-tools: Read, Write, Edit, Glob, Grep
---

# 무기 생성 명령어

사용자 요청: $ARGUMENTS

## 필수 작업

1. **가이드 읽기**: `Docs/Polar_Weapon_Agent_Guide.md` 파일을 먼저 읽어서 무기 시스템 구조를 파악합니다.

2. **요청 분석**: 사용자가 원하는 무기의 특성을 파악합니다.
   - 무기 타입 (Laser/Machinegun/Missile/Bullet)
   - 용도 (보스전/일반전/광역/단일타겟)
   - 티어 (1-4)
   - 특수 속성 (화염/냉기/전기 등)

3. **JSON 생성**: `Assets/Polar/RES/{WeaponName}.json` 파일을 생성합니다.

4. **안내 출력**:
   > Unity에서 생성된 JSON 파일 우클릭 → **Assets > Polar > Create Weapon from JSON**

## JSON 구조

```json
{
    "id": "weapon_id",
    "weaponName": "Display Name",
    "type": "laser|machinegun|missile|bullet",
    "damage": 100,
    ...타입별 필드...
}
```

## 밸런스 티어

- Tier 1: damage 50-200
- Tier 2: damage 200-500
- Tier 3: damage 500-1000
- Tier 4: damage 1000-5000

이제 "$ARGUMENTS" 요청에 맞는 무기를 생성하세요.
