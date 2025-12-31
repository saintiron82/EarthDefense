using UnityEngine;

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
    public sealed class ChunkEnemy : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform center;
        [SerializeField] private RingSectorMesh sectorMesh;
        [SerializeField] private RingSectorDamageMask damageMask;

        [Header("Move")]
        [Tooltip("조각(Chunk)의 기준 바깥 반지름(스폰 시 결정). 링은 SectorSpawner의 radiusOffset으로 이동합니다.")]
        [SerializeField, Min(0f)] private float baseOuterRadius = 6f;

        [Tooltip("조각(Chunk) 두께(월드). 스폰 시 결정되며 현재 로직에서는 전역 squeeze로 변경하지 않습니다.")]
        [SerializeField, Min(0.0001f)] private float thickness = 1f;

        [SerializeField, Min(0f)] private float reachRadius = 1.2f;

        private float _radiusOffset;

        public float InnerRadius => Mathf.Max(0f, baseOuterRadius - Thickness) + _radiusOffset;
        public float OuterRadius => baseOuterRadius + _radiusOffset;
        public float Thickness => Mathf.Max(0.0001f, thickness);

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
            float cellHp,
            int angleCells,
            Color color)
        {
            center = centerTransform;

            // baseInnerR는 슬롯 스택에서 바깥쪽으로 쌓아올릴 때 사용하는 기준 반지름
            // baseOuterRadius = baseInner + thickness
            baseOuterRadius = baseInnerR + baseThickness;
            thickness = baseThickness;

            // speed는 반지름 이동을 스포너가 전역으로 처리하므로 미사용(호환 파라미터)
            _ = speed;

            reachRadius = reachR;
            _core = core;

            if (sectorMesh != null)
            {
                sectorMesh.StartAngleDeg = thetaCenterDeg - arcDeg * 0.5f;
                sectorMesh.ArcAngleDeg = arcDeg;
                sectorMesh.Thickness = Thickness;
                sectorMesh.InnerRadius = Mathf.Max(0f, baseOuterRadius - Thickness);
                sectorMesh.RadiusOffset = 0f;
            }

            if (damageMask != null)
            {
                // 최소 스폰 버전: DamageMask의 cellHp/angleCells는 프리팹 인스펙터 값 사용.
                // (런타임 설정이 필요하면 RingSectorDamageMask에 Configure API를 추가하면 됨)
                _ = cellHp;
                _ = angleCells;
            }

            // 메시가 중심 기준으로 생성되므로, 섹터 오브젝트는 center 위치에 붙여둔다.
            if (center != null) transform.position = center.position;
        }

        /// <summary>
        /// 전역 이동/조임 값을 주입.
        /// - radiusOffset: 링 전체 반지름 이동(스포너가 관리)
        /// - squeeze: 레거시 호환 파라미터(현재는 사용하지 않음)
        /// </summary>
        public void SetGlobalOffsets(float radiusOffset, float squeeze)
        {
            // squeeze는 더 이상 사용하지 않음(두께 고정).
            _ = squeeze;

            _radiusOffset = radiusOffset;

            if (sectorMesh != null)
            {
                sectorMesh.RadiusOffset = _radiusOffset;
                sectorMesh.Thickness = Thickness;
                sectorMesh.InnerRadius = Mathf.Max(0f, baseOuterRadius - Thickness);
            }
        }

        private void Update()
        {
            if (center == null) return;

            // 항상 중심에 위치(메시가 중심 기준)
            if (transform.position != center.position) transform.position = center.position;

            // 도달 판정: 링이 거의 다 조여서 inner가 reach에 가까워지면
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
        }
    }
}
