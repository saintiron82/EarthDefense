using UnityEngine;

namespace Polar.Field
{
    /// <summary>
    /// Phase 1 - Step 2: 180개 섹터 데이터를 기반으로 실시간 변형되는 장막 메시 렌더링
    /// HTML destination-out 재현: 밖에서 안으로 조여오는 장막
    /// + 내부 경계선만 밝은 네온 글로우 (셰이더 처리)
    /// </summary>
    [RequireComponent(typeof(PolarFieldController))]
    public class PolarBoundaryRenderer : MonoBehaviour, IPolarView
    {
        [Header("Visual")]
        [SerializeField] private Color boundaryColor = new Color(0f, 0.8f, 0.8f, 0.3f); // 어두운 장막
        [SerializeField] private Material customMaterial;
        
        [Header("Edge Glow (Inner Line)")]
        [SerializeField] private Color edgeGlowColor = new Color(0f, 2f, 2f, 1f); // 밝은 시안
        [SerializeField, Range(0f, 50f)] private float edgeGlowStrength = 20f; // maps to _EdgeIntensity
        [SerializeField, Range(1f, 20f)] private float edgeGlowPower = 10f;     // maps to _EdgePower
        [SerializeField, Range(0.0005f, 0.05f)] private float lineThickness = 0.01f; // UV 범위 기준 초슬림
        
        [Header("Pulse Effect")]
        [SerializeField] private bool enableEdgePulse = true;
        [SerializeField, Range(0.5f, 5f)] private float edgePulseSpeed = 2.5f;
        [SerializeField, Range(0f, 0.5f)] private float edgePulseAmplitude = 0.3f;
        
        [Header("Spatial Configuration")]
        [SerializeField, Min(10f), Tooltip("화면 끝 거리 (HTML maxScreenDist)")]
        private float maxViewDistance = 20f;
        
        [Header("Background Curtain (Optional)")]
        [SerializeField] private bool renderCurtain = false;
        [SerializeField] private Color curtainColor = new Color(0f, 0.5f, 0.5f, 0.1f);
        
        [Header("References")]
        [SerializeField] private PolarFieldController controller;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // 메시 렌더링
        private Mesh _mesh;
        private MeshFilter _mf;
        private MeshRenderer _mr;
        private bool _isInitialized;
        
        // 정점 버퍼 (재사용)
        private Vector3[] _vertices;
        private Vector2[] _uvs;
        private int[] _triangles;
        private Color32[] _colors;
        
        // 펄스 상태
        private float _pulsationTime;
        
        // Config 캐시
        private PolarDataConfig _config;
        
        // MaterialPropertyBlock (성능 최적화)
        private MaterialPropertyBlock _mpb;
        
        // 변경 감지용 캐시
        private float _lastEdgeGlowStrength = -1f;
        private float _lastLineThickness = -1f;
        private float _lastEdgeGlowPower = -1f;
        private Color _lastEdgeGlowColor;
        private Color _lastBoundaryColor;
        private bool _materialPropertiesDirty = true;

        public bool IsViewActive => _isInitialized;

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<PolarFieldController>();
            }
            
            _mf = gameObject.AddComponent<MeshFilter>();
            _mr = gameObject.AddComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (controller != null)
            {
                InitializeView(controller);
            }
            else
            {
                Debug.LogError("[PolarBoundaryRenderer] PolarFieldController not found!");
            }
        }

        private void Update()
        {
            if (!_isInitialized || controller == null) return;

            // 펄스 시간 업데이트
            if (enableEdgePulse || (_config != null && _config.EnablePulsation))
            {
                _pulsationTime += Time.deltaTime;
            }

            UpdateFromPolarData(controller);
            
            // 변경 감지 후 업데이트 (값이 바뀌었을 때만)
            UpdateMaterialPropertiesIfDirty();
        }
        
        private void OnDestroy()
        {
            CleanupView();
        }
        
        /// <summary>
        /// Inspector 값 변경 시 (에디터 전용)
        /// </summary>
        private void OnValidate()
        {
            _materialPropertiesDirty = true;
            
            // 에디터 모드에서 즉시 반영
            if (!Application.isPlaying && _mr != null)
            {
                UpdateMaterialPropertiesIfDirty();
            }
        }

        #region IPolarView Implementation

        public void InitializeView(PolarFieldController polarController)
        {
            controller = polarController;
            _config = controller.Config;
            
            // 메시 초기화
            _mesh = new Mesh { name = "PolarBoundary" };
            _mesh.MarkDynamic();
            _mf.mesh = _mesh;
            
            // 머티리얼 설정
            SetupMaterial();
            
            // 메시 구조 생성 (토폴로지만 1회)
            BuildMeshTopology();
            
            _isInitialized = true;

            if (showDebugInfo)
            {
                Debug.Log($"[PolarBoundaryRenderer] Initialized: {_vertices.Length} vertices, {_triangles.Length / 3} triangles");
                Debug.Log($"[PolarBoundaryRenderer] Edge Glow: Strength={edgeGlowStrength}, Width={lineThickness}");
            }
        }

        public void UpdateFromPolarData(PolarFieldController polarController)
        {
            if (polarController == null || _vertices == null) return;

            int sectorCount = polarController.SectorCount;
            
            // 엣지 펄스 계산
            float edgePulse = 1.0f;
            if (enableEdgePulse)
            {
                edgePulse = 1.0f + Mathf.Sin(_pulsationTime * edgePulseSpeed) * edgePulseAmplitude;
            }

            // HTML의 for (let i = 0; i <= SECTOR_COUNT; i++) 재현
            for (int i = 0; i <= sectorCount; i++)
            {
                int sectorIndex = i % sectorCount;
                float angle = (i * Mathf.PI * 2f) / sectorCount;
                
                // ✅ HTML destination-out 논리
                float rInner = polarController.GetSectorRadius(sectorIndex); // 가변 (경계선)
                float rOuter = maxViewDistance;                              // 고정 (화면 끝)
                
                // Config 맥동 효과 적용
                if (_config != null && _config.EnablePulsation)
                {
                    float pulsation = CalculatePulsation(sectorIndex, sectorCount);
                    rInner += pulsation;
                    rOuter += pulsation * 0.3f; // 외곽도 약간
                }

                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                
                int vertexIndex = i * 2;
                _vertices[vertexIndex] = new Vector3(cos * rInner, sin * rInner, 0f);      // Inner = 경계선
                _vertices[vertexIndex + 1] = new Vector3(cos * rOuter, sin * rOuter, 0f);  // Outer = 화면 끝
                
                // 버텍스 컬러: Inner는 밝은 글로우, Outer는 어두움
                byte innerGlow = (byte)(255 * edgePulse); // 최전방: 밝음
                byte outerAlpha = 30; // 외곽: 매우 어두움
                
                _colors[vertexIndex] = new Color32(255, 255, 255, innerGlow);
                _colors[vertexIndex + 1] = new Color32(255, 255, 255, outerAlpha);
            }

            // 메시 업데이트
            _mesh.vertices = _vertices;
            _mesh.colors32 = _colors;
            _mesh.RecalculateBounds();
        }

        public void CleanupView()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_mesh);
                else
                    DestroyImmediate(_mesh);
            }
            
            _isInitialized = false;
        }

        #endregion

        /// <summary>
        /// 유기적 맥동 계산 (Config 파라미터)
        /// </summary>
        private float CalculatePulsation(int sectorIndex, int sectorCount)
        {
            if (_config == null) return 0f;

            float phase = (sectorIndex / (float)sectorCount) * _config.PhaseOffset * Mathf.PI * 2f;
            float wave = Mathf.Sin(_pulsationTime * _config.PulsationFrequency * Mathf.PI * 2f + phase);
            float pulsation = wave * _config.PulsationAmplitude;
            
            if (_config.UsePerlinNoise)
            {
                float noiseInput = (_pulsationTime * 0.5f + sectorIndex * 0.1f) * _config.NoiseScale;
                float noise = Mathf.PerlinNoise(noiseInput, sectorIndex * 0.01f) * 2f - 1f;
                pulsation += noise * _config.PulsationAmplitude * 0.5f;
            }
            
            return pulsation;
        }

        /// <summary>
        /// 메시 토폴로지 생성 (1회만)
        /// </summary>
        private void BuildMeshTopology()
        {
            int sectorCount = controller.SectorCount;
            int vertexCount = (sectorCount + 1) * 2;
            
            _vertices = new Vector3[vertexCount];
            _uvs = new Vector2[vertexCount];
            _colors = new Color32[vertexCount];
            
            // UV: Y=0 (내부), Y=1 (외부)
            for (int i = 0; i <= sectorCount; i++)
            {
                float t = (float)i / sectorCount;
                int vi = i * 2;
                
                _uvs[vi] = new Vector2(t, 0f);     // Inner
                _uvs[vi + 1] = new Vector2(t, 1f); // Outer
            }
            
            // 삼각형 (쿼드 스트립)
            int quadCount = sectorCount;
            _triangles = new int[quadCount * 6];
            
            int ti = 0;
            for (int i = 0; i < quadCount; i++)
            {
                int v0 = i * 2;
                int v1 = v0 + 1;
                int v2 = v0 + 2;
                int v3 = v0 + 3;
                
                _triangles[ti++] = v0;
                _triangles[ti++] = v3;
                _triangles[ti++] = v1;
                
                _triangles[ti++] = v0;
                _triangles[ti++] = v2;
                _triangles[ti++] = v3;
            }
            
            // 메시 설정
            _mesh.vertices = _vertices;
            _mesh.uv = _uvs;
            _mesh.colors32 = _colors;
            _mesh.triangles = _triangles;
            _mesh.RecalculateNormals();
        }

        private void SetupMaterial()
        {
            if (customMaterial != null)
            {
                _mr.sharedMaterial = customMaterial;
                _materialPropertiesDirty = true;
                return;
            }
            
            // RingSectorGlow 셰이더 사용
            var glowShader = Shader.Find("ShapeDefense/RingSectorGlow");
            if (glowShader != null)
            {
                var mat = new Material(glowShader);
                
                mat.SetColor("_BaseColor", boundaryColor);
                mat.SetColor("_EdgeColor", edgeGlowColor);
                mat.SetFloat("_EdgeIntensity", edgeGlowStrength);
                mat.SetFloat("_EdgePower", edgeGlowPower);
                mat.SetFloat("_EdgeWidth", lineThickness);
                mat.SetFloat("_EdgeMode", 0f);
                mat.SetFloat("_InvertLine", 0f);


                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                
                if (mat.HasProperty("_Cull"))
                {
                    mat.SetFloat("_Cull", 0f);
                }
                
                _mr.sharedMaterial = mat;
                _materialPropertiesDirty = true;
                
                if (showDebugInfo)
                {
                    Debug.Log("[PolarBoundaryRenderer] Using RingSectorGlow shader");
                }
            }
            else
            {
                // 폴백
                var mat = new Material(Shader.Find("Particles/Additive"));
                mat.color = boundaryColor;
                _mr.sharedMaterial = mat;
                
                Debug.LogWarning("[PolarBoundaryRenderer] RingSectorGlow shader not found");
            }
        }

        /// <summary>
        /// 런타임 머티리얼 프로퍼티 업데이트
        /// </summary>
        private void UpdateMaterialPropertiesIfDirty()
        {
            if (_mr == null || _mr.sharedMaterial == null) return;
            
            // 변경 감지
            bool isDirty = _materialPropertiesDirty ||
                           !Mathf.Approximately(_lastEdgeGlowStrength, edgeGlowStrength) ||
                           !Mathf.Approximately(_lastEdgeGlowPower, edgeGlowPower) ||
                           !Mathf.Approximately(_lastLineThickness, lineThickness) ||
                           _lastEdgeGlowColor != edgeGlowColor ||
                           _lastBoundaryColor != boundaryColor;

            if (!isDirty) return;

            _mr.GetPropertyBlock(_mpb);

            _mpb.SetFloat("_EdgeIntensity", edgeGlowStrength);
            _mpb.SetFloat("_EdgePower", edgeGlowPower);
            _mpb.SetFloat("_EdgeWidth", lineThickness);
            _mpb.SetFloat("_EdgeMode", 0f); // Line_Y
            _mpb.SetFloat("_InvertLine", 0f);
            _mpb.SetColor("_EdgeColor", edgeGlowColor);
            _mpb.SetColor("_BaseColor", boundaryColor);

            _mr.SetPropertyBlock(_mpb);

            _lastEdgeGlowStrength = edgeGlowStrength;
            _lastEdgeGlowPower = edgeGlowPower;
            _lastLineThickness = lineThickness;
            _lastEdgeGlowColor = edgeGlowColor;
            _lastBoundaryColor = boundaryColor;
            _materialPropertiesDirty = false;
        }

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!showDebugInfo || !_isInitialized || controller == null) return;

            Vector3 center = transform.position;
            Gizmos.color = Color.yellow;
            
            // 일부 섹터만 표시
            int sectorCount = controller.SectorCount;
            for (int i = 0; i < sectorCount; i += 20)
            {
                float angle = (i * Mathf.PI * 2f) / sectorCount;
                float radius = controller.GetSectorRadius(i);
                
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                Gizmos.DrawLine(center, center + dir * radius);
            }
        }

        #endregion
    }
}
