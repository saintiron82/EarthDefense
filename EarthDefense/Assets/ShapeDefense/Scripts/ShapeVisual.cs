using UnityEngine;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 외부 에셋 없이 '쉐이프 게임' 느낌을 내기 위한 최소 시각화.
    /// SpriteRenderer가 있으면 색을 주고, 없으면 Gizmo로 보이게 한다.
    /// </summary>
    public sealed class ShapeVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sr;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private float gizmoRadius = 0.25f;

        private void Reset()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (sr != null)
            {
                sr.color = color;
            }
        }

        private void OnValidate()
        {
            if (sr != null)
            {
                sr.color = color;
            }
        }

        private void OnDrawGizmos()
        {
            if (sr != null) return;

            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, gizmoRadius);
        }
    }
}
