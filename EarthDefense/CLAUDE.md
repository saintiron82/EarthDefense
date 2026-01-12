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
