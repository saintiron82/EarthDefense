using UnityEngine;

namespace ShapeDefense.Scripts.Debug
{
    /// <summary>
    /// 화면에 안 보일 때 원인을 빠르게 좁히기 위한 디버그 오버레이.
    /// - MeshFilter/MeshRenderer 상태
    /// - vertex/triangle 수
    /// - bounds
    /// - 카메라에서 보이는지(프러스텀 테스트)
    /// 
    /// 사용:
    /// - 문제 있는 Enemy(혹은 RingSectorMesh가 붙은 오브젝트)에 추가
    /// - Play 모드에서 인스펙터/콘솔 확인
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RingSectorDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool logEverySecond = true;
        [SerializeField] private bool drawBoundsGizmo = true;

        private MeshFilter _mf;
        private MeshRenderer _mr;
        private float _nextLog;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorIdLegacy = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _mf = GetComponent<MeshFilter>();
            _mr = GetComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (!logEverySecond) return;
            if (Time.unscaledTime < _nextLog) return;
            _nextLog = Time.unscaledTime + 1f;

            var mesh = _mf != null ? _mf.sharedMesh : null;
            var cam = Camera.main;

            int v = 0;
            int t = 0;
            Bounds b = default;
            bool hasBounds = false;

            if (mesh != null)
            {
                v = mesh.vertexCount;
                t = mesh.triangles != null ? mesh.triangles.Length / 3 : 0;
                b = mesh.bounds;
                hasBounds = true;
            }

            bool rendererEnabled = _mr != null && _mr.enabled;
            bool hasMat = _mr != null && _mr.sharedMaterial != null;
            string shaderName = hasMat ? _mr.sharedMaterial.shader != null ? _mr.sharedMaterial.shader.name : "<null shader>" : "<no material>";

            // bounds는 로컬이므로 월드로 변환(대략)
            Bounds worldBounds = default;
            bool inFrustum = false;
            if (hasBounds)
            {
                worldBounds = TransformBounds(transform, b);
                if (cam != null)
                {
                    var planes = GeometryUtility.CalculateFrustumPlanes(cam);
                    inFrustum = GeometryUtility.TestPlanesAABB(planes, worldBounds);
                }
            }

            Color? sharedMatColor = null;
            Color? mpbColor = null;

            if (_mr != null && _mr.sharedMaterial != null)
            {
                var mat = _mr.sharedMaterial;
                if (mat.HasProperty(BaseColorId)) sharedMatColor = mat.GetColor(BaseColorId);
                else if (mat.HasProperty(ColorIdLegacy)) sharedMatColor = mat.GetColor(ColorIdLegacy);

                _mr.GetPropertyBlock(_mpb);
                // PropertyBlock은 "설정 안됨" 상태를 직접 구분하기 어려워서,
                // 둘 다 꺼내보고 기본값(Color.clear)면 로그로만 참고.
                if (mat.HasProperty(BaseColorId)) mpbColor = _mpb.GetColor(BaseColorId);
                else if (mat.HasProperty(ColorIdLegacy)) mpbColor = _mpb.GetColor(ColorIdLegacy);
            }

            UnityEngine.Debug.Log(
                $"[RingSectorDebug] {name} " +
                $"RendererEnabled={rendererEnabled} HasMaterial={hasMat} Shader={shaderName} " +
                $"Verts={v} Tris={t} InFrustum={inFrustum} " +
                $"SharedMatColor={(sharedMatColor.HasValue ? sharedMatColor.Value.ToString() : "<n/a>")} " +
                $"MPBColor={(mpbColor.HasValue ? mpbColor.Value.ToString() : "<n/a>")} " +
                $"WorldBounds={(hasBounds ? worldBounds.ToString() : "<no mesh>")}",
                this);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawBoundsGizmo) return;
            var mf = _mf != null ? _mf : GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            var b = TransformBounds(transform, mf.sharedMesh.bounds);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(b.center, b.size);
        }

        private static Bounds TransformBounds(Transform t, Bounds local)
        {
            // AABB를 월드로 안전하게 변환(정확히는 OBB 필요하지만 디버그엔 충분)
            var center = t.TransformPoint(local.center);

            var ext = local.extents;
            var axisX = t.TransformVector(ext.x, 0, 0);
            var axisY = t.TransformVector(0, ext.y, 0);
            var axisZ = t.TransformVector(0, 0, ext.z);

            ext.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            ext.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            ext.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds(center, ext * 2f);
        }
    }
}
