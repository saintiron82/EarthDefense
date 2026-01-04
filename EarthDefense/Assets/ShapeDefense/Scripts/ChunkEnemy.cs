using UnityEngine;
using Script.SystemCore.Pool;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 조각(Chunk) 1개의 게임플레이 상태를 관리.
    /// 
    /// 용어(문서 기준):
    /// - 섹터(Sector): 360도를 일정 각도로 나눈 방향 슬롯(slot)
    /// - 조각(Chunk): 한 섹터(slot) 안에서 반지름 A~B를 차지하는 적 스트립 1개(=본 컴포넌트)
    /// - 셀(Cell): 조각 내부 데미지 단위(RingSectorDamageMask)
    /// 
    /// 동작:
    /// - 링 전체 이동은 `SectorSpawner`가 `radiusOffset`으로 처리
    /// - 렌더링은 `RingSectorMesh`가 담당
    /// - 셀 파괴/피해는 `RingSectorDamageMask`가 담당
    /// </summary>
    public sealed class ChunkEnemy : MonoBehaviour, IPoolable
    {
        // IPoolable - 풀에서 자동 주입
        public string PoolBundleId { get; set; }

        [Header("Refs")]
        [SerializeField] private Transform center;
        [SerializeField] private RingSectorMesh sectorMesh;
        [SerializeField] private RingSectorDamageMask damageMask;

        [Header("Move")]
        [Tooltip("조각(Chunk)의 기준 바깥 반지름(스폰 시 결정).")]
        [SerializeField, Min(0f)] private float baseOuterRadius = 6f;

        [Tooltip("조각(Chunk) 두께(월드). 스폰 시 결정됩니다.")]
        [SerializeField, Min(0.0001f)] private float thickness = 1f;

        [Tooltip("반지름이 줄어드는 속도(초당 월드 유닛).")]
        [SerializeField, Min(0f)] private float radialSpeed = 0.75f;

        [SerializeField, Min(0f)] private float reachRadius = 1.2f;

        public float InnerRadius => Mathf.Max(0f, baseOuterRadius - Thickness);
        public float OuterRadius => baseOuterRadius;
        public float Thickness => Mathf.Max(0.0001f, thickness);

        public int SectorIndex { get; private set; }
        public RingSectorDamageMask DamageMask => damageMask;
        public float StartAngleDeg => sectorMesh != null ? sectorMesh.StartAngleDeg : 0f;
        public float ArcAngleDeg => sectorMesh != null ? sectorMesh.ArcAngleDeg : 0f;

        private float _nextContactTime;
        private PlayerCore _core;

        [Header("Contact Damage")]
        [SerializeField] private bool dealContactDamage;
        [SerializeField, Min(0f)] private float contactDamage = 2f;
        [SerializeField, Min(0.01f)] private float contactCooldown = 0.25f;

        private void Reset()
        {
            sectorMesh = GetComponent<RingSectorMesh>();
            damageMask = GetComponent<RingSectorDamageMask>();
        }

        private void Awake()
        {
            if (sectorMesh == null) sectorMesh = GetComponent<RingSectorMesh>();
            if (damageMask == null) damageMask = GetComponent<RingSectorDamageMask>();
        }

        public void Configure(
            Transform centerTransform,
            float thetaCenterDeg,
            float arcDeg,
            float baseInnerR,
            float baseThickness,
            float speed,
            float reachR,
            PlayerCore core,
            Color color,
            int sectorIndex)
        {
            center = centerTransform;
            SectorIndex = sectorIndex;

            thickness = Mathf.Max(0.0001f, baseThickness);
            baseOuterRadius = Mathf.Max(0f, baseInnerR) + thickness;

            radialSpeed = Mathf.Max(0f, speed);
            reachRadius = reachR;
            _core = core;

            if (sectorMesh != null)
            {
                sectorMesh.StartAngleDeg = thetaCenterDeg - arcDeg * 0.5f;
                sectorMesh.ArcAngleDeg = arcDeg;
                sectorMesh.Thickness = Thickness;
                sectorMesh.InnerRadius = Mathf.Max(0f, baseOuterRadius - Thickness);
                sectorMesh.RadiusOffset = 0f;

                // 색 적용(버텍스 컬러 + per-renderer 컬러)
                sectorMesh.SetVertexColor(color);
            }

            // 메시가 중심 기준으로 생성되므로, 섹터 오브젝트는 center 위치에 붙여둔다.
            if (center != null) transform.position = center.position;
        }

        /// <summary>
        /// (레거시) 전역 이동/조임 값 주입 - 더 이상 사용하지 않습니다.
        /// </summary>
        public void SetGlobalOffsets(float radiusOffset, float squeeze)
        {
            _ = radiusOffset;
            _ = squeeze;
        }

        private void Update()
        {
            if (center == null) return;

            // 각 Chunk가 자체적으로 반지름을 줄인다.
            if (radialSpeed > 0f)
            {
                baseOuterRadius = Mathf.Max(0f, baseOuterRadius - radialSpeed * Time.deltaTime);

                if (sectorMesh != null)
                {
                    sectorMesh.Thickness = Thickness;
                    sectorMesh.InnerRadius = Mathf.Max(0f, baseOuterRadius - Thickness);
                }
                
                // 청크의 위치가 변경되었으므로 DamageableRegistry에 업데이트 알림
                // (현재는 피봇이 고정이지만 향후 이동할 수 있으므로 대비)
                if (damageMask != null)
                {
                    DamageableRegistry.UpdateChunkPosition(damageMask, transform.position);
                }
            }

            // 도달 판정: inner가 reach에 가까워지면
            if (InnerRadius <= reachRadius)
            {
                HandleReach();
            }
        }

        private void HandleReach()
        {
            if (!dealContactDamage) return;
            if (_core == null || _core.Health == null) return;
            if (Time.time < _nextContactTime) return;

            _nextContactTime = Time.time + contactCooldown;

            var pos = (Vector2)transform.position;
            var dir = ((Vector2)_core.transform.position - pos).normalized;
            _core.Health.TakeDamage(new DamageEvent(contactDamage, pos, dir, gameObject));

            // 코어에 도달한 청크는 더 이상 유지할 이유가 없으므로 풀로 반환(또는 파괴)
            Despawn();
        }

        private void Despawn()
        {
            var pool = PoolService.Instance;
            if (pool != null)
            {
                pool.Return(this);
                return;
            }

            Destroy(gameObject);
        }

        public void OnSpawnFromPool()
        {
            // 풀에서 재사용될 때 레퍼런스/타이머 초기화
            _nextContactTime = 0f;
        }

        public void OnReturnToPool()
        {
            // center가 null이면 Update에서 바로 return 되기 쉬우므로, 안전하게 비움
            center = null;

            // 섹터 스택에서 제거
            SectorManager.Instance?.UnregisterChunk(SectorIndex, this);
        }
    }
}
