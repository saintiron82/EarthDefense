using UnityEngine;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 링 섹터(부채꼴 조각) 1개의 게임플레이 상태를 관리.
    /// - 중심을 향해 반지름 방향으로 접근
    /// - 도달 반지름에 닿으면 (옵션) 플레이어 코어에 접촉 피해
    /// - 렌더링은 RingSectorMesh가 담당
    /// - 셀 파괴/피해는 RingSectorDamageMask가 담당
    /// </summary>
    public sealed class SectorEnemy : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform center;
        [SerializeField] private RingSectorMesh sectorMesh;
        [SerializeField] private RingSectorDamageMask damageMask;

        [Header("Move")]
        [Tooltip("섹터의 고정 바깥 반지름(스폰 시 결정). 조여오는 연출은 innerRadius를 바깥으로 밀어 thickness를 줄여서 구현합니다.")]
        [SerializeField, Min(0f)] private float baseOuterRadius = 6f;

        [Tooltip("현재 두께(월드). 전역 squeeze 값에 따라 감소합니다.")]
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
        /// 전역 squeeze(조임) 값을 주입. 값이 커질수록 Thickness가 줄어들어 링이 얇아집니다.
        /// </summary>
        public void SetGlobalScroll(float globalScroll)
        {
            // 호환용: 기존 코드는 globalScroll(음수)을 radiusOffset으로만 쓰거나 squeeze로만 썼음.
            // 현재는 '둘 다' 필요하므로, 여기선 radiusOffset으로 해석하고 squeeze는 0으로 둔다.
            SetGlobalOffsets(globalScroll, 0f);
        }

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
