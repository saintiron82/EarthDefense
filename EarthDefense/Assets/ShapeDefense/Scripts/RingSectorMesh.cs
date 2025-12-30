using UnityEngine;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 링 형태의 부채꼴(annular sector) 메시를 런타임/에디터에서 생성해 렌더링한다.
    /// - 고퀄 쉐이프 게임을 위한 기본 빌딩 블록.
    /// - 충돌/피해 판정은 별도(수학 판정)로 두는 것을 권장.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteAlways]
    public sealed class RingSectorMesh : MonoBehaviour
    {
        [Header("Geometry (Degrees)")]
        [SerializeField, Range(-180f, 180f)] private float startAngleDeg = -10f;
        [SerializeField, Range(0.01f, 360f)] private float arcAngleDeg = 20f;

        [Header("Radii")]
        [SerializeField, Min(0f)] private float innerRadius = 2.0f;
        [SerializeField, Min(0.0001f)] private float thickness = 0.5f;

        [Header("Tessellation")]
        [Tooltip("원호를 몇 등분할지. 값이 클수록 원호가 부드럽지만 정점 수가 증가합니다.")]
        [SerializeField, Range(1, 256)] private int segments = 24;

        [Header("Rendering")]
        [SerializeField] private Color color = Color.white;
        [Tooltip("Renderer2D(URP 2D) 환경에서도 보이도록 기본 Unlit 계열 머티리얼을 자동 생성합니다.")]
        [SerializeField] private bool autoCreateMaterial = true;

        [Header("Runtime Offset")]
        [Tooltip("버텍스 반지름에 더해지는 런타임 오프셋. SectorSpawner의 globalScroll을 전달하면 HTML 원본처럼 전체가 같이 당겨집니다.")]
        [SerializeField] private float radiusOffset;

        [Header("Vertex Color")]
        [Tooltip("켜면 Mesh.colors32에 버텍스 컬러를 채웁니다. 조각별 색을 머티리얼 공유 상태로 제어할 때 유용합니다.")]
        [SerializeField] private bool useVertexColor = true;

        [SerializeField] private Color vertexColor = Color.white;

        private MeshFilter _mf;
        private MeshRenderer _mr;
        private Mesh _mesh;
        private int _lastHash;

        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorIdLegacy = Shader.PropertyToID("_Color");

        // 피해 마스크(선택). 없으면 기존처럼 단일 링 섹터로 렌더링.
        private RingSectorDamageMask _damageMask;

        [Header("Rendering Debug")]
        [Tooltip("켜면 머티리얼이 단면 컬링 상태여도 항상 보이도록 Cull Off를 강제합니다(디버그/호환용).")]
        [SerializeField] private bool forceDoubleSided = true;

        [Tooltip("삼각형 winding을 뒤집습니다. 카메라/렌더러 조합에서 뒤집혀 안 보일 때 사용하세요.")]
        [SerializeField] private bool flipWinding;

        private MaterialPropertyBlock _mpb;

        public float StartAngleDeg { get => startAngleDeg; set { startAngleDeg = value; MarkDirty(); } }
        public float ArcAngleDeg { get => arcAngleDeg; set { arcAngleDeg = value; MarkDirty(); } }
        public float InnerRadius { get => innerRadius; set { innerRadius = Mathf.Max(0f, value); MarkDirty(); } }
        public float Thickness { get => thickness; set { thickness = Mathf.Max(0.0001f, value); MarkDirty(); } }
        public float OuterRadius => innerRadius + thickness;

        public float RadiusOffset { get => radiusOffset; set { radiusOffset = value; MarkDirty(); } }

        /// <summary>
        /// 외부에서(예: 피해 시스템) 메시 리빌드가 필요함을 알리기 위한 API.
        /// </summary>
        public void MarkDirtyExternal() => MarkDirty();

        /// <summary>
        /// 선택적으로 피해 마스크를 연결한다. (RingSectorDamageMask가 자동으로 등록)
        /// </summary>
        public void SetDamageMask(RingSectorDamageMask mask)
        {
            _damageMask = mask;
            MarkDirty();
        }

        private void EnsureMpb()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
        }

        public void SetVertexColor(Color c)
        {
            vertexColor = c;
            ApplyVertexColor();
            ApplyPerRendererColor();
        }

        private void Reset()
        {
            _mf = GetComponent<MeshFilter>();
            _mr = GetComponent<MeshRenderer>();
        }

        private void Awake()
        {
            EnsureRefs();
            EnsureMesh();
            EnsureMaterial();
            EnsureMpb();

            // MeshRenderer가 꺼져있거나 머티리얼이 null이면 화면에 아무 것도 나오지 않음
            if (_mr != null) _mr.enabled = true;

            RebuildIfNeeded(force: true);
            ApplyPerRendererColor();
        }

        private void OnEnable()
        {
            EnsureRefs();
            EnsureMesh();
            EnsureMaterial();
            EnsureMpb();
            if (_mr != null) _mr.enabled = true;
            RebuildIfNeeded(force: true);
            ApplyPerRendererColor();
        }

        private void OnValidate()
        {
            thickness = Mathf.Max(0.0001f, thickness);
            arcAngleDeg = Mathf.Clamp(arcAngleDeg, 0.01f, 360f);
            segments = Mathf.Clamp(segments, 1, 256);

            EnsureRefs();
            EnsureMesh();
            EnsureMaterial();
            RebuildIfNeeded(force: true);
        }

        private void Update()
        {
            // ExecuteAlways 환경에서 값이 변경되면 리빌드
            RebuildIfNeeded(force: false);
            ApplyColor();
            ApplyVertexColor();
            ApplyPerRendererColor();
        }

        private void OnDestroy()
        {
            // 에디터/플레이 모두에서 누수 방지
            if (_mesh != null)
            {
                if (Application.isPlaying) Destroy(_mesh);
                else DestroyImmediate(_mesh);
            }
        }

        private void MarkDirty()
        {
            _lastHash = 0;
        }

        private void EnsureRefs()
        {
            if (_mf == null) _mf = GetComponent<MeshFilter>();
            if (_mr == null) _mr = GetComponent<MeshRenderer>();
        }

        private void EnsureMesh()
        {
            if (_mesh != null) return;

            _mesh = new Mesh
            {
                name = "RingSectorMesh_Runtime"
            };
            _mesh.MarkDynamic();

            if (_mf != null) _mf.sharedMesh = _mesh;
        }

        private void EnsureMaterial()
        {
            if (_mr == null) return;
            if (!autoCreateMaterial) return;
            if (_mr.sharedMaterial != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            // 마지막 폴백: Unity가 제공하는 InternalErrorShader라도 잡아서 '핑크'라도 보이게
            if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");

            if (shader == null) return;

            var mat = new Material(shader)
            {
                name = "RingSector_AutoMaterial"
            };

            _mr.sharedMaterial = mat;

            // 생성 직후 색 적용(머티리얼 프로퍼티명이 셰이더마다 달라질 수 있어 ApplyColor에서 다시 처리)
            ApplyColor();
        }

        private void ApplyPerRendererColor()
        {
            if (_mr == null) return;
            var mat = _mr.sharedMaterial;
            if (mat == null) return;

            EnsureMpb();

            // 조각별 색은 vertexColor를 우선 사용 (스포너에서 SetVertexColor로 주입)
            var c = vertexColor;

            _mr.GetPropertyBlock(_mpb);
            if (mat.HasProperty(ColorId)) _mpb.SetColor(ColorId, c);
            if (mat.HasProperty(ColorIdLegacy)) _mpb.SetColor(ColorIdLegacy, c);
            _mr.SetPropertyBlock(_mpb);
        }

        private void ApplyColor()
        {
            if (_mr == null) return;
            var mat = _mr.sharedMaterial;
            if (mat == null) return;

            // 단면 컬링으로 인해 아예 안 보이는 케이스를 빠르게 회피
            if (forceDoubleSided && mat.HasProperty("_Cull"))
            {
                mat.SetFloat("_Cull", 0f); // Off
            }

            // '공유 머티리얼'의 기본색은 color로 유지(전역/디폴트)
            if (mat.HasProperty(ColorId)) mat.SetColor(ColorId, color);
            else if (mat.HasProperty(ColorIdLegacy)) mat.SetColor(ColorIdLegacy, color);
        }

        private void ApplyVertexColor()
        {
            if (!useVertexColor) return;
            if (_mesh == null) return;

            // mesh가 아직 생성 전이면 다음 프레임에 적용
            var vc = (Color32)vertexColor;

            // vertices가 없으면 중단
            var vCount = _mesh.vertexCount;
            if (vCount <= 0) return;

            var cols = _mesh.colors32;
            if (cols == null || cols.Length != vCount)
            {
                cols = new Color32[vCount];
            }

            for (int i = 0; i < vCount; i++) cols[i] = vc;

            _mesh.colors32 = cols;
        }

        private int ComputeHash()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + startAngleDeg.GetHashCode();
                h = h * 31 + arcAngleDeg.GetHashCode();
                h = h * 31 + innerRadius.GetHashCode();
                h = h * 31 + thickness.GetHashCode();
                h = h * 31 + segments;
                h = h * 31 + radiusOffset.GetHashCode();
                return h;
            }
        }

        private void RebuildIfNeeded(bool force)
        {
            if (_mesh == null) return;

            var hash = ComputeHash();
            if (_damageMask != null)
            {
                hash = unchecked(hash * 31 + _damageMask.AngleCells);
                hash = unchecked(hash * 31 + _damageMask.RadialCells);

                // 2D(각도 x 반지름) 전체 셀 상태를 해시에 반영
                for (int r = 0; r < _damageMask.RadialCells; r++)
                {
                    for (int a = 0; a < _damageMask.AngleCells; a++)
                    {
                        hash = unchecked(hash * 31 + _damageMask.GetCellErosion01(a, r).GetHashCode());
                    }
                }
            }

            if (!force && hash == _lastHash) return;
            _lastHash = hash;

            var rInner = innerRadius + radiusOffset;
            var rOuter = innerRadius + thickness + radiusOffset;

            // 2D에서는 반지름이 너무 작거나 뒤집히면 bounds/클리핑 문제로 아예 안 보이는 경우가 많음
            rInner = Mathf.Max(0f, rInner);
            rOuter = Mathf.Max(rInner + 0.01f, rOuter); // 최소 두께 확보

            if (_damageMask == null)
            {
                BuildMesh(_mesh, startAngleDeg, arcAngleDeg, rInner, rOuter, segments, flipWinding);
            }
            else
            {
                BuildMeshWithMask(_mesh, startAngleDeg, arcAngleDeg, rInner, rOuter, segments, _damageMask, flipWinding);
            }
        }

        private static void BuildMesh(Mesh mesh, float startDeg, float arcDeg, float rInner, float rOuter, int seg, bool flip)
        {
            // 세그먼트 기준으로 (seg+1)개의 원호 포인트를 만들고,
            // 각 포인트마다 inner/outer 2개의 버텍스를 둬서 스트립을 만든다.

            var vertexCount = (seg + 1) * 2;
            var triCount = seg * 2; // 쿼드(seg개) -> 삼각형 2개씩

            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var tris = new int[triCount * 3];

            var startRad = startDeg * Mathf.Deg2Rad;
            var arcRad = arcDeg * Mathf.Deg2Rad;

            // UV는 단순히 원호 진행도를 x로, inner/outer를 y로 둔다.
            // 나중에 셰이더에서 그라데이션/노이즈 넣기 좋다.
            for (int i = 0; i <= seg; i++)
            {
                float t = seg == 0 ? 0f : (float)i / seg;
                float a = startRad + arcRad * t;
                float ca = Mathf.Cos(a);
                float sa = Mathf.Sin(a);

                int vi = i * 2;

                vertices[vi + 0] = new Vector3(ca * rInner, sa * rInner, 0f);
                vertices[vi + 1] = new Vector3(ca * rOuter, sa * rOuter, 0f);

                uvs[vi + 0] = new Vector2(t, 0f);
                uvs[vi + 1] = new Vector2(t, 1f);
            }

            int ti = 0;
            for (int i = 0; i < seg; i++)
            {
                int v0 = i * 2;
                int v1 = v0 + 1;
                int v2 = v0 + 2;
                int v3 = v0 + 3;

                // (v0 inner, v1 outer) -> (v2 inner, v3 outer) 사각형
                if (!flip)
                {
                    tris[ti++] = v0; tris[ti++] = v3; tris[ti++] = v1;
                    tris[ti++] = v0; tris[ti++] = v2; tris[ti++] = v3;
                }
                else
                {
                    // winding 반전: (a,b,c) -> (a,c,b)
                    tris[ti++] = v0; tris[ti++] = v1; tris[ti++] = v3;
                    tris[ti++] = v0; tris[ti++] = v3; tris[ti++] = v2;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = tris;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private static void BuildMeshWithMask(
            Mesh mesh,
            float startDeg,
            float arcDeg,
            float rInner,
            float rOuter,
            int seg,
            RingSectorDamageMask mask,
            bool flip)
        {
            // 전략:
            // - 기본 링 섹터를 seg 등분으로 만들되,
            // - 각 세그먼트가 속한 '각도 셀'의 erosion에 따라 inner 반지름을 바깥으로 밀어 침식 표현
            // - erosion==1이면 해당 세그먼트 쿼드를 생성하지 않아 '구멍'처럼 보이게

            seg = Mathf.Max(1, seg);
            var angleCells = Mathf.Max(1, mask.AngleCells);
            var radialCells = Mathf.Max(1, mask.RadialCells);

            // 반지름 방향 스트립 분할 수: radialCells
            // 각도 방향 정점 분할 수: seg
            // vertices: 각 (angle step)마다 (radialCells+1)개의 반지름 정점을 둔다.
            var vertexCols = seg + 1;
            var vertexRows = radialCells + 1;
            var vertexCount = vertexCols * vertexRows;

            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];

            var startRad = startDeg * Mathf.Deg2Rad;
            var arcRad = arcDeg * Mathf.Deg2Rad;

            // 정점 생성
            for (int ai = 0; ai <= seg; ai++)
            {
                float tA = (float)ai / seg;
                float a = startRad + arcRad * tA;
                float ca = Mathf.Cos(a);
                float sa = Mathf.Sin(a);

                for (int ri = 0; ri <= radialCells; ri++)
                {
                    float tR = (float)ri / radialCells;
                    float r = Mathf.Lerp(rInner, rOuter, tR);

                    int vi = ri * vertexCols + ai;
                    vertices[vi] = new Vector3(ca * r, sa * r, 0f);
                    uvs[vi] = new Vector2(tA, tR);
                }
            }

            // tri는 셀 파괴 상태에 따라 일부만 생성
            // 각 quad는 (seg * radialCells)개, quad당 tri 2개
            var tris = new int[seg * radialCells * 2 * 3];
            int ti = 0;

            for (int ai = 0; ai < seg; ai++)
            {
                float tMidA = (ai + 0.5f) / seg;
                int angleCell = Mathf.Clamp(Mathf.FloorToInt(tMidA * angleCells), 0, angleCells - 1);

                for (int ri = 0; ri < radialCells; ri++)
                {
                    int radialCell = ri;

                    float erosion01 = Mathf.Clamp01(mask.GetCellErosion01(angleCell, radialCell));
                    if (erosion01 >= 1f) continue;

                    int v00 = ri * vertexCols + ai;
                    int v10 = ri * vertexCols + (ai + 1);
                    int v01 = (ri + 1) * vertexCols + ai;
                    int v11 = (ri + 1) * vertexCols + (ai + 1);

                    if (!flip)
                    {
                        tris[ti++] = v00; tris[ti++] = v11; tris[ti++] = v10;
                        tris[ti++] = v00; tris[ti++] = v01; tris[ti++] = v11;
                    }
                    else
                    {
                        tris[ti++] = v00; tris[ti++] = v10; tris[ti++] = v11;
                        tris[ti++] = v00; tris[ti++] = v11; tris[ti++] = v01;
                    }
                }
            }

            // tri 잘라내기
            if (ti == 0)
            {
                mesh.Clear();
                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.triangles = System.Array.Empty<int>();
                mesh.RecalculateBounds();
                return;
            }

            var finalTris = new int[ti];
            System.Array.Copy(tris, finalTris, ti);

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = finalTris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            // 마지막에 mesh.vertices/uv/triangles 설정 후 색은 외부에서 ApplyVertexColor로 적용
        }

        private void OnDrawGizmosSelected()
        {
            // 편의: 선택 시 반지름 감각을 보여줌
            Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, innerRadius + radiusOffset);
            Gizmos.DrawWireSphere(transform.position, innerRadius + thickness + radiusOffset);

            if (_mf != null && _mf.sharedMesh != null)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
                Gizmos.DrawWireCube(transform.TransformPoint(_mf.sharedMesh.bounds.center), _mf.sharedMesh.bounds.size);
            }
        }
    }
}
