# Polar CLI JSON 편집 기능 추가 계획

## 목표
- 무기/투사체/옵션 프로필(공용 프리셋)을 Unity CLI(batchmode)에서 JSON으로 Export/Import하여 외부에서 수정 가능하게 한다.
- 기존 `PolarWeaponData`의 `ToJson/FromJson` 구조는 유지하면서, 옵션 프로필 참조까지 CLI에서 연결 가능하게 한다.

## 범위
- 대상 데이터
  - `PolarWeaponData` + 파생 데이터(`PolarLaserWeaponData`, `PolarMachinegunWeaponData`, `PolarMissileWeaponData`)
  - `PolarWeaponOptionProfile`
  - `PolarProjectileOptionProfile`
- 실행 방식
  - `Unity.exe -batchmode -quit -projectPath ... -executeMethod ... -args ...`

---

## 1) 옵션 프로필 JSON 지원 추가
- `PolarWeaponOptionProfile`
  - `id` 필드 추가(문자열)
  - `ToJson/FromJson` 추가(또는 전용 Utility)
- `PolarProjectileOptionProfile`
  - `id` 필드 추가(문자열)
  - `ToJson/FromJson` 추가(또는 전용 Utility)
- 컬러 직렬화 방식: `float[4] (r,g,b,a)` 유지

산출물
- 옵션 프로필도 무기처럼 JSON으로 뽑고/넣을 수 있음

---

## 2) 무기 JSON에 옵션 프로필 참조 포함
- `PolarWeaponData` JSON에 선택 필드 추가
  - `optionProfileId` (없으면 기존 개별 값 사용)
- `PolarMachinegunWeaponData` JSON에 선택 필드 추가
  - `projectileOptionProfileId`
- `PolarMissileWeaponData` JSON에 선택 필드 추가
  - `projectileOptionProfileId`
- Import 시 동작
  - `id -> 옵션 프로필 asset` 매핑 후 참조 연결
  - 매핑 실패 시 경고 로그 + 참조 유지/해제 정책 결정(기본: 유지)

산출물
- 옵션 조합이 JSON에서 가능해짐(프로필 id로)

---

## 3) 배치 Import/Export 헬퍼 구현
- 폴더 단위 Export
  - `Weapons/`: 무기 JSON 일괄 저장
  - `Options/Weapon/`: 무기 옵션 프로필 JSON 일괄 저장
  - `Options/Projectile/`: 투사체 옵션 프로필 JSON 일괄 저장
- 폴더 단위 Import
  - JSON 파일들을 순회하며 id 기반으로 대상 asset을 찾아 업데이트
- asset 탐색 방식
  - 우선 `PolarWeaponDataTable`이 있으면 그 목록을 사용
  - 없으면 `AssetDatabase.FindAssets("t:PolarWeaponData")` 등으로 검색

산출물
- 대량 밸런싱 작업이 JSON 폴더로 가능

---

## 4) CLI 실행 엔트리포인트(에디터 스크립트) 추가
- 새 파일(에디터 전용): 예) `Assets/Polar/Weapons/Editor/PolarCli.cs`
- 정적 메서드 제공
  - `ExportAllWeaponsToFolder`
  - `ImportAllWeaponsFromFolder`
  - `ExportAllOptionProfilesToFolder`
  - `ImportAllOptionProfilesFromFolder`
- 인자 파싱
  - `Environment.GetCommandLineArgs()`에서 `-exportDir=...` `-importDir=...` 등 파싱
- 변경사항 저장
  - `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets` + `AssetDatabase.Refresh`

산출물
- Unity Editor 없이도 batchmode로 JSON 수정 반영 가능

---

## 5) 문서화
- 이 문서를 기준으로 `Docs`에 CLI 사용법(실행 예시/폴더 구조/주의사항/id 규칙) 추가

---

## 6) 검증
- `Assembly-CSharp-Editor` 컴파일 확인(에디터 스크립트 조건부)
- `Assembly-CSharp` 컴파일 확인(런타임 코드 영향 없음)
- 최소 시나리오
  - Export → JSON 수정 → Import → 에셋 값 변경 확인
