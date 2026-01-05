# ShapeDefense 핵심 개념 정리

## 1. 프로젝트 요약
- 장르: 탑다운 슈터/디펜스 하이브리드. 원형(링) 맵을 섹터/청크로 나누어 적과 투사체를 처리.
- 목표: 수학 기반 판정(콜라이더 최소화)으로 성능 확보, 셀 단위 침식/파괴를 시각화.
- 주요 자산: 링 메시(RingSectorMesh), 셀 침식 마스크(RingSectorDamageMask), 무기/투사체 시스템, HP/팀 판정.

## 2. 공간/지오메트리
- **Sector**: 360°를 일정 각도로 분할한 방향 슬롯.
- **Chunk(RingSectorMesh)**: 한 섹터 내에서 반지름 구간 A~B를 차지하는 스트립 메시.
- **Cell(RingSectorDamageMask)**: 청크 내부를 각도×반지름 그리드로 쪼갠 데미지 단위.
- **침식(Erosion)**: 셀 누적 피해량 ÷ `cellHp` (0~1). 1이면 메시 일부 제거.
- **이벤트**: `CellDestroyed(int angleIndex)`로 셀 최초 파괴를 알림.

## 3. 데미지/판정 흐름
1) 무기/투사체가 피격 지점을 계산해 `DamageEvent`를 생성.
2) 대상이 **Health**면 HP를 깎고 사망 시 `Died` 이벤트, 필요 시 객체 파괴(`destroyOnDeath`).
3) 대상이 **RingSectorDamageMask**면 셀 인덱스를 계산해 누적 데미지 적용, 메시 업데이트.
4) **DamageableRegistry**는 씬 내 데미지 대상 등록/관리(수학 판정용). 미사용 공간 그리드 API는 단순화 가능.

## 4. Health(HP/팀) 규칙
- 고유 ID: `UniqueId`는 항상 `InstanceID` 자동 할당(인스펙터 숨김).
- 팀키: `teamKey`로 아군/적/중립 구분. 투사체/무기는 발사자 팀을 전달해 아군 오사 방지에 활용.
- 사망 처리: HP≤0 → `Died` 이벤트 → `destroyOnDeath`가 true면 GameObject 파괴.
- 리셋: `ResetHp(newMaxHp)`로 최대/현재 HP 동시 초기화(최소 1).

## 5. 무기/투사체 시스템
- **WeaponController**: 무기 데이터(`WeaponDataTable`)에서 ID로 프리팹 로드 → `BaseWeapon` 초기화(`Initialize(data, owner, team)`). 입력에 따라 자동/수동 발사.
- **BaseWeapon 파생체**: `MachineGunWeapon`, `LaserWeapon`, `BeamProjectile`, `ExplosiveBullet` 등. 발사 모드(`FireMode.Automatic/Manual`) 지원.
- **조준/입력**: `WeaponController.Update()`에서 마우스 좌표를 월드로 변환 후 `UpdateAim`. 공격 입력에 따라 `StartFire/StopFire/Fire` 호출.

## 6. 자원/풀
- **ResourceService**: 번들 기반 리소스 로딩/캐싱.
- **PoolService**: Unity `ObjectPool<T>` 기반 재사용. 풀 설정은 번들/프리셋(`PoolConfig/Preset`)에서 결정.

## 7. 공간 최적화(선택적)
- `DamageableRegistry`는 리스트 + SpatialGrid 기반 API를 갖지만, 현 코드에서 라인/반경 쿼리, 위치 업데이트, 주기적 Prune 등이 미사용. 필요 없으면 순수 리스트 레지스트리로 축소 가능.
- 메시 변형/침식이 잦을 때 콜라이더 재생성 비용이 커서 수학/셀 기반 판정이 유리. 하이브리드가 필요하면 느슨한 AABB/원형 콜라이더만 두고 세밀 판정은 셀로 수행.

## 8. 데이터 흐름(요약)
- 무기 ID → `WeaponDataTable` → 프리팹 로드 → `BaseWeapon.Initialize` → `DamageEvent` 생성 → `Health` 또는 `RingSectorDamageMask`에 적용 → 이벤트(`Damaged`, `Died`, `CellDestroyed`).

## 9. 역할별 포인트
- **기획**: 셀 침식이 시각적 핵심. 팀키로 아군/적 구분. 무기 추가는 데이터 테이블+프리팹+BaseWeapon 확장.
- **프로그래밍**: 그리드 API 사용 여부 정리 후 유지/삭제 결정. 메시 업데이트는 `RingSectorDamageMask`의 `MarkDirtyExternal`로 트리거. HP/팀/이벤트 훅으로 게임 룰 연결.
- **성능**: 다수 오브젝트·잦은 메시 변형 → 콜라이더 재빌드 지양, 수학 판정 유지. 필요 시 SpatialGrid 재활용하여 범위 후보만 좁힌 뒤 세밀 판정.

## 10. TODO 제안(문서/정리)
- 현재 사용 중/미사용 시스템 표 작성(특히 DamageableRegistry 공간 API, 1D 호환 메서드).
- 무기 데이터 사양서: 필드 정의(ID, Name, BundleId, UnlockLevel 등)와 생성 가이드.
- 링/셀 파괴 연출 파이프라인: 입력→피격 계산→Mask 업데이트→메시 리빌드/이펙트 흐름도.
- 성능 프로파일링 가이드: 침식 빈도, 프레임별 리빌드 비용, 풀링 설정 체크리스트.
