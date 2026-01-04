# EarthDefense - 다양한 무기 시스템
프로젝트 라이선스를 따릅니다.
## 라이선스

- Object Pooling (PoolService)
- New Input System
- C# 9.0
- Unity 2022.3+
## 기술 스택

- [ShapeDefense 용어집](Docs/ShapeDefense_Terminology.md)
- [상세 가이드](Docs/WeaponSystem_Guide.md)
## 참고 문서

5. **업그레이드 시스템**: 무기 강화 시스템
4. **UI 연동**: 무기 선택 UI 구현
3. **사운드 통합**: 발사/폭발/히트 사운드
2. **이펙트 추가**: 폭발, 레이저 비주얼 개선
1. **프리팹 생성**: 각 무기 타입별 프리팹 제작

## 다음 단계

- 실시간 레이저 경로 확인
- `debugDrawRaycast`: 레이캐스트 디버그 라인
### LaserWeapon

- 씬 뷰에서 폭발 범위 시각화
- `showExplosionRadius`: 폭발 범위 기즈모 표시
### ExplosiveBullet

## 디버그 옵션

- 히트 이펙트 재사용
- LineRenderer 업데이트 최소화
- Raycast 결과 캐싱
### 레이저 최적화

```
PoolService.Instance.Register("ExplosiveMissile", prefab, 10);
stats.BulletPoolId = "ExplosiveMissile";
// PoolService 사용
```csharp
### 오브젝트 풀링

## 성능 최적화

4. `MultiWeaponShooter`에 발사 로직 추가
3. `WeaponStats`에 필요 속성 추가
2. 무기 컴포넌트 클래스 생성
1. `WeaponType` enum에 새 타입 추가
### 확장 방법

- **Turret**: 자동 포탑
- **Beam Cannon**: 관통 빔
- **Chain Lightning**: 연쇄 공격
- **Homing Missile**: 유도 미사일
- **Shotgun**: 부채꼴 다중 발사
### 추가 가능한 무기

## 확장 가능성

```
단점: 단일 대상만, 지속 시간 제한
장점: 높은 단일 대상 DPS, 즉시 타격
DPS: 30 (단일 대상 집중)
```
### 지속형 레이저

```
단점: 느린 발사 속도, 탄속
장점: 범위 공격, 다수 적 동시 처리
DPS: 22.5 (단일), 최대 70+ (다수 적)
```
### 폭발 미사일

## 밸런싱

- 팀 시스템 통합
- 오브젝트 풀링 지원
- 무기 타입별 발사 로직 분기
- 모든 무기 타입 통합 관리
### MultiWeaponShooter 클래스

- LineRenderer를 통한 비주얼
- 틱 기반 데미지 적용
- `UpdateDirection()`: 조준 방향 실시간 업데이트
- `StopFiring()`: 레이저 발사 중지
- `StartFiring()`: 레이저 발사 시작
### LaserWeapon 클래스

- IPoolable 인터페이스 지원 (오브젝트 풀링)
- 거리 기반 데미지 감쇠
- `Explode()`: 범위 내 모든 적에게 데미지
- `Fire()`: 미사일 발사 및 초기화
### ExplosiveBullet 클래스

## 주요 기능

| **ContinuousLaser** | 특수 | 매우 높음 (30/s) | **지속 발사** | **단일 보스** |
| **ExplosiveMissile** | 느림 (1.5/s) | 높음 (15) | **범위 공격** | **밀집 적군** |
| **LaserGun** | 중간 (6/s) | 중간 (4) | 관통 (5회) | 여러 적 관통 |
| **Cannon** | 느림 (2/s) | 높음 (20) | 단발 강력 | 고체력 적 |
| **MachineGun** | 빠름 (12/s) | 낮음 (3) | 연사형 | 일반 적 처리 |
|---------|---------|--------|------|------|
| 무기 타입 | 발사속도 | 데미지 | 특징 | 용도 |

## 무기 타입 비교

Right-click on component → **Apply Weapon Preset**
### 4. Apply Preset

- **Laser Weapon Prefab**: 레이저 무기 프리팹 할당
- **Explosive Bullet Prefab**: 폭발 미사일 프리팹 할당
### 3. 프리팹 설정

  - `ContinuousLaser`: 지속형 레이저
  - `ExplosiveMissile`: 폭발 미사일
- **Weapon Settings > Weapon Type Preset** 선택
Inspector에서:
### 2. 무기 타입 선택

```
GameObject → Add Component → Multi Weapon Shooter
```
### 1. MultiWeaponShooter 컴포넌트 추가

## 빠른 시작

```
└── WeaponSystem_Guide.md        # 상세 사용 가이드
Docs/

└── WeaponStats.cs               # 무기 스탯 및 타입 정의 (확장됨)
├── MultiWeaponShooter.cs        # 통합 무기 시스템
│   └── ExplosionEffect.cs       # 폭발 이펙트 비주얼
│   ├── LaserWeapon.cs           # 레이저 무기 로직
│   ├── ExplosiveBullet.cs       # 폭발 미사일 로직
├── Weapons/
Assets/ShapeDefense/Scripts/
```

## 구현된 파일 구조

```
정확한 타격이 필요한 상황
고체력 단일 적 제거
강력한 보스 적 처리
```
**사용 예시:**

- 단일 대상 집중 공격
- LineRenderer를 통한 시각적 레이저 빔
- 즉시 타격 (광선)
- 초당 데미지: 30 (10틱/초)
- 지속 발사 (기본 2초)
**특징:**

버튼을 누르는 동안 **끊이지 않고 일정 기간 발사**되며, **단일 객체에 대한 다단 히트를 지원**하는 무기입니다.
### 2. 지속형 레이저 (ContinuousLaser)

```
방어선을 돌파하는 적 무리 제거
적이 밀집된 상황에서 효과적
```
**사용 예시:**

- 다수의 적을 한 번에 처리 가능
- 폭발 이펙트 지원
- 거리 기반 데미지 감쇠 (최대 30%)
- 범위 공격 (기본 반경 2.0)
**특징:**

목표 지점에 도달하거나 적과 충돌 시 **일정 범위 내의 모든 적에게 동시 데미지**를 입히는 무기입니다.
### 1. 폭발 미사일 (ExplosiveMissile)

## 새로 추가된 기능


