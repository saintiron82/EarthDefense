using UnityEngine;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 플레이어 코어(중앙)에 붙이는 '쉘 링' 시각화/기준 반지름.
    /// 
    /// - 현재 단계에서는 '보이는 링'을 만드는 데 집중.
    /// - RingSectorMesh를 재사용해 360도 링을 그린다.
    /// - reachRadius(=섹터가 닿았다고 판단하는 반지름)와 동일한 값을 쓰면 감각이 맞는다.
    /// </summary>
    public sealed class ShellRing : MonoBehaviour
    {
        [Header("Geometry")]
        [SerializeField, Min(0f)] private float radius = 1.2f;
        [SerializeField, Min(0.001f)] private float thickness = 0.15f;
        [SerializeField, Range(8, 256)] private int segments = 96;

        [Header("Rendering")]
        [SerializeField] private Color color = Color.white;
        [Tooltip("원형 링을 그릴 MeshRenderer가 필요합니다. 비워두면 자식으로 자동 생성합니다.")]
        [SerializeField] private RingSectorMesh ringMesh;

        public float Radius => radius;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Reset()
        {
            EnsureRing();
        }

        private void Awake()
        {
            EnsureRing();
            Apply();
        }

        private void OnValidate()
        {
            thickness = Mathf.Max(0.001f, thickness);
            segments = Mathf.Clamp(segments, 8, 256);
            EnsureRing();
            Apply();
        }

        private void EnsureRing()
        {
            if (ringMesh != null) return;

            // 자식 오브젝트로 생성해 코어 위치를 깔끔하게 유지
            var child = transform.Find("ShellRingMesh");
            if (child == null)
            {
                var go = new GameObject("ShellRingMesh");
                go.transform.SetParent(transform, false);
                child = go.transform;
            }

            ringMesh = child.GetComponent<RingSectorMesh>();
            if (ringMesh == null) ringMesh = child.gameObject.AddComponent<RingSectorMesh>();

            // 쉘 링은 파괴 마스크가 필요 없으므로 마스크 컴포넌트는 붙이지 않는다.
        }

        private void Apply()
        {
            if (ringMesh == null) return;

            // 360도 링
            ringMesh.StartAngleDeg = 0f;
            ringMesh.ArcAngleDeg = 360f;
            ringMesh.InnerRadius = radius;
            ringMesh.Thickness = thickness;

            // 색은 RingSectorMesh 내부 필드라 직접 세팅 API가 없어서,
            // 현재는 MeshRenderer 머티리얼 컬러를 직접 변경하는 방식으로 처리.
            if (ringMesh.TryGetComponent<MeshRenderer>(out var mr) && mr.sharedMaterial != null)
            {
                var mat = mr.sharedMaterial;
                if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, color);
                else if (mat.HasProperty(ColorId)) mat.SetColor(ColorId, color);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
