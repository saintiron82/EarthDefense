# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

EarthDefense is a Unity 2D game using a **polar coordinate system** for gameplay. The player defends Earth from incoming threats by pushing back sector boundaries using weapons. The core mechanic involves 180 sectors arranged radially around Earth, each with its own radius and resistance (HP).

## Key Technologies

- **Unity 6** (URP - Universal Render Pipeline)
- **UniTask** for async operations (`com.cysharp.unitask`)
- **Unity Input System** (`com.unity.inputsystem`)
- **Unity Addressables** for asset management
- **Unity Localization** for multi-language support

## Architecture

### SGSystem Framework

The project uses a custom framework called **SGSystem** located in `Assets/SGSystem/`. Always prefer using SGSystem components over creating new solutions.

**Service Layer** (`SG` namespace):
- `App` - Application entry point, manages ServiceHome lifecycle
- `ServiceHome` - Service container with dependency injection
- `ServiceBase` / `MonoServiceBase` - Base classes for services (pure C# vs MonoBehaviour)
- `IService` - Service interface with `Init()`, `Prepare()`, `Release()`, `Destroy()` lifecycle

**Manager Layer**:
- `ManagerMaster` - Container for runtime managers (shorter lifecycle than services)
- `ManagerBase` / `MonoManagerBase` - Base classes for managers
- Managers are registered via `ManagerMaster.AddNewManager<T>()`

**Core Services**:
- `ResourceService` - Asset loading with caching and fallback
- `PoolService` - Object pooling using Unity's `ObjectPool<T>`
- `UIService` - UI management with Presenter pattern
- `SaveService` - Save/load functionality
- `AudioService` - Audio playback management
- `TableService` - Data table loading

### Polar System

Located in `Assets/Polar/`, this is the game-specific implementation:

**Field** (`Polar.Field` namespace):
- `PolarFieldController` - Core controller managing 180 sectors with gravity simulation, resistance system, knockback mechanics
- `PolarDataConfig` - ScriptableObject configuration for field parameters
- `IPolarField` - Interface for field access

**Weapons** (`Polar.Weapons` namespace):
- `PolarWeaponBase` - Abstract base for all weapons
- `PolarWeaponArm` - Weapon mount point with aiming
- `PolarProjectileBase` - Base for projectiles
- Weapon types: `PolarMachinegunWeapon`, `PolarLaserWeapon`, `PolarMissileWeapon`
- Data: `PolarWeaponData` and type-specific ScriptableObjects

**Input** (`Polar.Input` namespace):
- `PolarWeaponInputHandler` - Handles weapon input
- `PolarInputActionsRuntime` - Input action mappings

### Service Registration Pattern

```csharp
// In App.InstallServices():
home.AddNewService<SaveService>();
home.AddNewService<ManagerMaster>();
home.AddNewService<UIService>();
```

### Accessing Services and Managers

```csharp
// Services
var saveService = App.Instance.ServiceHome.GetService<SaveService>();
ResourceService.Instance.LoadPrefab("key");
PoolService.Instance.Get("poolKey");

// Managers
var manager = ManagerMaster.Get<MyManager>();
```

## Code Conventions

- Use Korean comments where existing code uses Korean
- Prefer async/await with UniTask over coroutines
- Use `[SerializeField]` for Inspector-exposed fields
- Use `#if UNITY_EDITOR` for editor-only code
- Place Editor scripts in `Editor/` subdirectories

## Important Namespaces

- `SG` - Core framework
- `SG.UI` - UI system
- `SG.Audio` - Audio system
- `SG.Save` - Save system
- `Polar.Field` - Polar coordinate field system
- `Polar.Weapons` - Weapon system
- `Polar.Input` - Input handling

---

## Weapon Creation Agent (무기 생성 에이전트)

사용자가 무기 생성을 요청하면 다음 절차를 따릅니다:

### 1. 가이드 문서 참조
**필수**: `Docs/Polar_Weapon_Agent_Guide.md` 파일을 먼저 읽어서 무기 시스템 구조를 파악합니다.

### 2. 무기 타입 결정
- **Laser**: 지속 빔 공격, 약점 집중 타격
- **Machinegun**: 연사 공격, 영역 무력화
- **Missile**: 폭발 공격, 광역 넉백
- **Bullet**: 단발 정밀 타격

### 3. JSON 파일 생성
위치: `Assets/Polar/RES/{WeaponName}.json`

```json
{
    "id": "weapon_id",
    "weaponName": "Display Name",
    "type": "laser|machinegun|missile|bullet",
    "damage": 100,
    ...타입별 필드...
}
```

### 4. 밸런스 고려사항
- Tier 1 (기본): damage 50-200
- Tier 2 (고급): damage 200-500
- Tier 3 (희귀): damage 500-1000
- Tier 4 (전설): damage 1000-5000

### 5. 에셋 변환 안내
JSON 생성 후 사용자에게 안내:
> Unity에서 해당 JSON 파일 우클릭 → "Assets > Polar > Create Weapon from JSON"

### 예시 요청 및 응답

**요청**: "보스전용 고데미지 레이저 만들어줘"
**분석**: 좁은 빔 + 높은 DPS + 긴 지속시간
**결과**:
```json
{
    "id": "boss_killer_laser",
    "weaponName": "Boss Killer Laser",
    "type": "laser",
    "damage": 2000,
    "beamWidth": 0.3,
    "duration": 5,
    "beamColor": [1, 0, 0, 1]
}
```
