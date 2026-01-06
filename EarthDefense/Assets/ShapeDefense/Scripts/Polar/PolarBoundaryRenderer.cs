using UnityEngine;

namespace ShapeDefense.Scripts.Polar
{
    /// <summary>
    /// Phase 1 - Step 2 (수정): 180개 섹터 데이터를 정점 1:1 매핑
    /// HTML 프로토타입의 ctx.lineTo 로직을 Mesh.vertices로 직접 변환
    /// 
    /// 핵심 논리: "밖에서 안으로 조여오는 장막" (HTML destination-out 재현)
    /// + 유기적 맥동 효과 (Organic Pulsation) - Config에서 설정 로드
    /// </summary>
    [RequireComponent(typeof(PolarFieldController))]
    public class PolarBoundaryRenderer : MonoBehaviour, IPolarView
    {
        [Header("Visual")]
        [SerializeField] private Color boundaryColor = new Color(0f, 1f, 1f, 1.0f);
        [SerializeField] private Material customMaterial;
        
        [Header("Spatial Configuration")]
        [SerializeField, Min(10f), Tooltip("화면 끝 거리 (HTML의 maxScreenDist)")]
        private float maxViewDistance = 20f;
        
        [Header("References")]
        [SerializeField] private PolarFieldController controller;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // 단일 메시 (HTML의 Canvas 대응)
        private Mesh _mesh;
        private MeshFilter _mf;
        private MeshRenderer _mr;
        private bool _isInitialized;

        // 정점 버퍼 (재사용)
        private Vector3[] _vertices;
        private Vector2[] _uvs;
        private int[] _triangles;
        private Color32[] _colors;
        
        // 맥동 상태
        private float _pulsationTime;
        
        // Config 캐시 (성능)
        private PolarDataConfig _config;

        public bool IsViewActive => _isInitialized;

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<PolarFieldController>();
            }
            
            _mf = gameObject.AddComponent<MeshFilter>();
            _mr = gameObject.AddComponent<MeshRenderer>();
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

            // 맥동 시간 업데이트 (Config에서 활성화 여부 확인)
            if (_config != null && _config.EnablePulsation)
            {
                _pulsationTime += Time.deltaTime;
            }

            UpdateFromPolarData(controller);
        }

        private void OnDestroy()
        {
            CleanupView();
        }

        #region IPolarView Implementation

        public void InitializeView(PolarFieldController polarController)
        {
            controller = polarController;
            
            // Config 캐시
            _config = controller.Config; // PolarFieldController에서 Config 접근
            
            // 메시 초기화
            _mesh = new Mesh { name = "PolarBoundary" };
            _mesh.MarkDynamic();
            _mf.mesh = _mesh;
            
            // 머티리얼 설정
            SetupMaterial();
            
            // 메시 구조 생성 (토폴로지만 1회 생성)
            BuildMeshTopology();
            
            _isInitialized = true;

            if (showDebugInfo)
            {
                Debug.Log($"[PolarBoundaryRenderer] Initialized: {_vertices.Length} vertices, {_triangles.Length / 3} triangles");
                if (_config != null && _config.EnablePulsation)
                {
                    Debug.Log($"[PolarBoundaryRenderer] Pulsation: Amp={_config.PulsationAmplitude:F3}, Freq={_config.PulsationFrequency:F2}Hz");
                }
            }
        }

        public void UpdateFromPolarData(PolarFieldController polarController)
        {
            if (polarController == null || _vertices == null) return;

            int sectorCount = polarController.SectorCount;

            // HTML의 for (let i = 0; i <= SECTOR_COUNT; i++) 루프 재현
            for (int i = 0; i <= sectorCount; i++)
            {
                // 순환 처리 (마지막 정점 = 첫 정점)
                int sectorIndex = i % sectorCount;
                float angle = (i * Mathf.PI * 2f) / sectorCount;
                
                // ✅ 논리 반전: HTML destination-out 재현
                float rInner = polarController.GetSectorRadius(sectorIndex); // 가변 (경계선)
                float rOuter = maxViewDistance;                              // 고정 (화면 끝)
                
                // 🌊 맥동 효과 추가 (Config에서 설정 로드)
                if (_config != null && _config.EnablePulsation)
                {
                    float pulsation = CalculatePulsation(sectorIndex, sectorCount);
                    rInner += pulsation;
                    
                    // 외곽도 약간 맥동 (덜 강하게)
                    rOuter += pulsation * 0.3f;
                }

                // 정점 위치 직접 업데이트 (HTML의 ctx.lineTo)
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                
                int vertexIndex = i * 2;
                _vertices[vertexIndex] = new Vector3(cos * rInner, sin * rInner, 0f);      // Inner = 경계선 (움직임)
                _vertices[vertexIndex + 1] = new Vector3(cos * rOuter, sin * rOuter, 0f);  // Outer = 화면 끝 (고정)
            }

            // 메시 업데이트 (정점만 변경, 토폴로지는 불변)
            _mesh.vertices = _vertices;
            _mesh.RecalculateBounds();
        }

        /// <summary>
        /// 유기적 맥동 계산 (Config에서 파라미터 로드)
        /// </summary>
        private float CalculatePulsation(int sectorIndex, int sectorCount)
        {
            if (_config == null) return 0f;

            // 기본 사인파 맥동
            float phase = (sectorIndex / (float)sectorCount) * _config.PhaseOffset * Mathf.PI * 2f;
            float wave = Mathf.Sin(_pulsationTime * _config.PulsationFrequency * Mathf.PI * 2f + phase);
            
            float pulsation = wave * _config.PulsationAmplitude;
            
            // 노이즈 추가 (불규칙한 생명감)
            if (_config.UsePerlinNoise)
            {
                float noiseInput = (_pulsationTime * 0.5f + sectorIndex * 0.1f) * _config.NoiseScale;
                float noise = Mathf.PerlinNoise(noiseInput, sectorIndex * 0.01f) * 2f - 1f;
                pulsation += noise * _config.PulsationAmplitude * 0.5f;
            }
            
            return pulsation;
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
        /// 메시 토폴로지 생성 (1회만 실행)
        /// HTML의 "선으로 그리기" 대신 삼각형 스트립으로 변환
        /// </summary>
        private void BuildMeshTopology()
        {
            int sectorCount = controller.SectorCount;
            
            // 정점 개수: (sectorCount + 1) * 2 (순환을 위해 +1)
            int vertexCount = (sectorCount + 1) * 2;
            _vertices = new Vector3[vertexCount];
            _uvs = new Vector2[vertexCount];
            _colors = new Color32[vertexCount];
            
            // UV 및 색상 초기화
            Color32 c = boundaryColor;
            for (int i = 0; i <= sectorCount; i++)
            {
                float t = (float)i / sectorCount;
                int vi = i * 2;
                
                _uvs[vi] = new Vector2(t, 0f);     // Inner
                _uvs[vi + 1] = new Vector2(t, 1f); // Outer
                
                _colors[vi] = c;
                _colors[vi + 1] = c;
            }
            
            // 삼각형 생성: 쿼드 스트립
            int quadCount = sectorCount;
            int triangleCount = quadCount * 2 * 3;
            _triangles = new int[triangleCount];
            
            int ti = 0;
            for (int i = 0; i < quadCount; i++)
            {
                int v0 = i * 2;
                int v1 = v0 + 1;
                int v2 = v0 + 2;
                int v3 = v0 + 3;
                
                // Triangle 1
                _triangles[ti++] = v0;
                _triangles[ti++] = v3;
                _triangles[ti++] = v1;
                
                // Triangle 2
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
                return;
            }
            
            // RingSectorGlow 셰이더 사용
            var neonShader = Shader.Find("ShapeDefense/RingSectorGlow");
            if (neonShader != null)
            {
                var mat = new Material(neonShader);
                mat.SetColor("_BaseColor", boundaryColor);
                mat.SetColor("_EmissionColor", Color.cyan);
                mat.SetFloat("_EmissionStrength", 1.5f);
                
                // 양면 렌더링
                if (mat.HasProperty("_Cull"))
                {
                    mat.SetFloat("_Cull", 0f); // Cull Off
                }
                
                _mr.sharedMaterial = mat;
            }
            else
            {
                // 폴백: 기본 Unlit
                var mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = boundaryColor;
                _mr.sharedMaterial = mat;
            }
        }

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!showDebugInfo || !_isInitialized || controller == null) return;

            Vector3 center = transform.position;
            Gizmos.color = Color.yellow;
            
            // 섹터 경계 표시
            int sectorCount = controller.SectorCount;
            for (int i = 0; i < sectorCount; i += 10) // 10개마다
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
