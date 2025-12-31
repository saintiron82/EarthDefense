# ShapeDefense 용어 정의 (Sector / Chunk / Cell)

이 문서는 `ShapeDefense`의 링 기반 적 배치/피해 시스템에서 사용하는 용어를 통일하기 위한 기준 문서입니다.

> 용어 매핑 원칙
> - 기획/대화 용어는 `섹터(Sector)` / `조각(Chunk)` / `셀(Cell)`을 사용합니다.
> - 현재 코드에는 레거시(HTML 원본) 영향으로 `chunkCount`처럼 용어가 섞여 있습니다. 아래에 **코드 매핑표**로 정리합니다.

## 1) 섹터 (`Sector`)

- 정의: 전체 원(360도)을 일정 각도로 분할한 **하나의 방향 슬롯(slot)**.
- 범위: 특정 각도 구간(예: 20도 폭) 전체.
- 구현/데이터 관점:
  - 섹터 수(=슬롯 수): `SectorSpawner.chunkCount` (레거시 네이밍)
  - 섹터 인덱스: `slotIndex`
  - 섹터 각폭: `arc = 360f / chunkCount`

## 2) 조각 (`Chunk`)

- 정의: 한 섹터 안에서 **반지름 거리 A~B 사이를 차지하는 하나의 적 군집(링 스트립)**.
- 특징:
  - 섹터 방향(각도)은 유지한 채, **반지름 방향 두께(`thickness`)**를 가짐
  - 스포너가 바깥쪽부터 연속으로 쌓아 올리는 기본 단위
- 구현/데이터 관점:
  - 조각 1개는 보통 `SectorEnemy` 인스턴스 1개에 해당
  - 조각의 반지름 범위(기준 값):
    - inner(base) = `baseInnerR`
    - outer(base) = `baseInnerR + t`
  - 조각 두께 `t`는 `SectorSpawner.CurrentThickness()`에서 결정됨
    - `keepPixelThickness=true`면 카메라/픽셀 기준으로 환산된 월드 두께를 사용
    - `randomThickness=true`면 랜덤 범위를 사용

## 3) 셀 (`Cell`)

- 정의: 조각 내부에서 **데미지/파괴가 적용되는 최소 단위**.
- 분할 방식:
  - 2D 그리드로 생각할 수 있음
    - 각도 방향 셀: `RingSectorDamageMask.angleCells`
    - 반지름 방향 셀: `RingSectorDamageMask.radialCells`
- 구현/데이터 관점:
  - `RingSectorDamageMask`가 셀 상태(피해 누적/파괴/침식도)를 보유
  - 반지름 방향 “셀 한 칸 두께(월드 단위)”는 개념적으로 다음과 같음:
    - `cellThickness = chunkThickness / radialCells`

## 포함 관계(구성 관계)

- 조각은 셀의 군집으로 이루어진다.
- 섹터는 조각으로 이루어진다.

즉:
- `Sector` → `Chunk` → `Cell`

## 코드 매핑(현재 구현 기준)

| 개념(문서) | 현재 코드에서 주로 쓰는 이름/위치 |
|---|---|
| 섹터(Sector, slot) | `SectorSpawner.chunkCount`(섹터 수), `slotIndex`(섹터 인덱스) |
| 조각(Chunk) | `SectorEnemy` 인스턴스 1개, `SectorSpawner.AddNewChunk()`가 생성 |
| 셀(Cell) | `RingSectorDamageMask.angleCells`, `RingSectorDamageMask.radialCells` |

## 권장 표현(코드/대화에서의 명칭)

- 섹터: `Sector` (slot)
- 조각: `Chunk`
- 셀: `Cell`

용어 혼동을 줄이기 위해, 새로 추가하는 코드/주석은 가능하면 위 영문 표기를 병기하는 것을 권장합니다.
