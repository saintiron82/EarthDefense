using System.Collections.Generic;
using UnityEngine;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// HTML 원본의 chunkStacks/nextR 개념을 유니티로 옮긴 최소 스포너.
    /// - 중심을 기준으로 각도 슬롯(chunkCount)을 만들고
    /// - 각 슬롯마다 링 섹터 조각을 스택처럼 바깥쪽으로 계속 쌓아 채운다.
    /// - 조각들은 시간이 지나면 중심을 향해 접근하며, 슬롯의 가장 바깥쪽이 일정 거리 이하가 되면 새 조각을 추가한다.
    /// </summary>
    public sealed class SectorSpawner : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform center;
        [SerializeField] private PlayerCore playerCore;
        [SerializeField] private SectorEnemy sectorPrefab;

        [Header("Layout")]
        [SerializeField, Min(1)] private int chunkCount = 18;
        [SerializeField, Min(1)] private int initialPerChunk = 10;
        [SerializeField, Min(0f)] private float startRadius = 4.0f;
        [SerializeField, Min(0.01f)] private float thickness = 1.0f;
        [SerializeField] private bool randomThickness;
        [SerializeField, Min(0.01f)] private float minThickness = 0.6f;
        [SerializeField, Min(0.01f)] private float maxThickness = 1.6f;

        [Header("Flow")]
        [Tooltip("진행값(시간). 값이 증가할수록 링이 안쪽으로 이동합니다(반지름 감소).")]
        [SerializeField] private float squeezeProgress;

        [Tooltip("진행 속도(초당).")]
        [SerializeField, Min(0f)] private float squeezeSpeed = 0.75f;

        [Tooltip("링 전체가 안쪽으로 이동하는 속도(초당 월드 유닛).")]
        [SerializeField, Min(0f)] private float radiusInwardSpeed = 0.75f;

        [Tooltip("현재 반지름 오프셋(디버그). 음수일수록 링이 중심 쪽으로 이동합니다.")]
        [SerializeField] private float radiusOffset;

        [Tooltip("반지름 오프셋 하한. 링이 너무 안쪽으로 들어가 0 근처에서 붕괴되는 걸 방지합니다.")]
        [SerializeField] private float minRadiusOffset = -50f;

        [Tooltip("켜면 반지름 오프셋을 매 프레임 갱신해 링이 안쪽으로 이동합니다. 문제 진단 시 끄면 고정됩니다.")]
        [SerializeField] private bool animateRadiusOffset = true;

        [Tooltip("각 슬롯의 바깥쪽을 이 반지름까지 항상 채웁니다(월드 기준). 카메라 화면 밖까지 채워두면 빈틈이 안 보입니다.")]
        [SerializeField, Min(0f)] private float ensureFilledUntilRadius = 24.0f;

        [Header("Enemy")]
        [SerializeField, Min(0f)] private float radialSpeed = 0.6f;
        [SerializeField, Min(0f)] private float reachRadius = 1.2f;

        [Header("Cells")]
        [SerializeField, Range(1, 64)] private int angleCells = 5;
        [SerializeField, Min(0.001f)] private float cellHp = 60f;

        [Header("Debug")]
        [SerializeField] private bool autoSetupOnStart = true;

        [Header("Shell Sync")]
        [Tooltip("center(플레이어 코어)에 ShellRing이 있으면, 그 반지름으로 reachRadius를 자동 동기화합니다.")]
        [SerializeField] private bool syncReachRadiusFromShell = true;

        [Header("Pixel Thickness")]
        [Tooltip("켜면 thickness를 월드값으로 고정하지 않고, 화면(픽셀) 기준 두께를 유지하도록 자동 계산합니다(Orthographic 카메라 전용).")]
        [SerializeField] private bool keepPixelThickness = true;

        [Tooltip("keepPixelThickness=true일 때 목표 픽셀 두께")]
        [SerializeField, Min(1f)] private float targetPixelThickness = 28f;

        private readonly List<SectorEnemy>[] _stacks = new List<SectorEnemy>[512];
        private float[] _nextR = new float[512];

        private void Reset()
        {
            if (center == null) center = transform;
        }

        private void Start()
        {
            if (syncReachRadiusFromShell && center != null)
            {
                var shell = center.GetComponent<ShellRing>();
                if (shell != null)
                {
                    reachRadius = shell.Radius;
                }
            }

            // ensureFilledUntilRadius를 카메라 기준으로 자동 세팅(옵션)
            AutoTuneFillRadiusIfPossible();

            if (!autoSetupOnStart) return;
            Setup();

            // 시작 시점에는 항상 0에서 출발(순간적으로 보였다가 바로 사라지는 상황 방지)
            radiusOffset = 0f;
        }

        private void AutoTuneFillRadiusIfPossible()
        {
            // Scene/Game 뷰에 보이는 빈틈을 줄이기 위해 Camera.main의 ortho size를 참고할 수 있으면 사용.
            // (없거나 perspective면 사용 안 함)
            var cam = Camera.main;
            if (cam == null) return;
            if (!cam.orthographic) return;

            // 화면의 코너까지 거리 = sqrt(halfWidth^2 + halfHeight^2)
            var halfH = cam.orthographicSize;
            var halfW = halfH * cam.aspect;
            var corner = Mathf.Sqrt(halfW * halfW + halfH * halfH);

            // 여유분: 링 두께/스크롤을 감안해 약간 더 크게
            var margin = 6f;
            ensureFilledUntilRadius = Mathf.Max(ensureFilledUntilRadius, corner + margin);
        }

        [ContextMenu("Setup")]
        public void Setup()
        {
            if (sectorPrefab == null || center == null)
            {
                UnityEngine.Debug.LogWarning("SectorSpawner: missing refs (sectorPrefab/center)", this);
                return;
            }

            chunkCount = Mathf.Clamp(chunkCount, 1, 512);

            // 기존 스폰된 자식 정리(간단)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            for (int i = 0; i < chunkCount; i++)
            {
                _stacks[i] = new List<SectorEnemy>(initialPerChunk + 8);

                // 전역 스크롤(음수) 기준에서도 startRadius 바깥부터 채우려면 base는 startRadius - globalScroll
                _nextR[i] = startRadius;

                // 처음부터 ensureFilledUntilRadius까지 '연속 밀착'되도록 채우기
                FillSlotToRadius(i);
            }
        }

        private void Update()
        {
            if (sectorPrefab == null || center == null) return;

            if (animateRadiusOffset)
            {
                radiusOffset -= radiusInwardSpeed * Time.deltaTime;
                if (radiusOffset < minRadiusOffset) radiusOffset = minRadiusOffset;
            }

            for (int i = 0; i < chunkCount; i++)
            {
                var stack = _stacks[i];
                if (stack == null)
                {
                    _stacks[i] = new List<SectorEnemy>();
                    _nextR[i] = startRadius;
                    stack = _stacks[i];
                }

                for (int s = 0; s < stack.Count; s++)
                {
                    if (stack[s] == null) continue;
                    stack[s].SetGlobalOffsets(radiusOffset, 0f);
                }

                for (int s = stack.Count - 1; s >= 0; s--)
                {
                    if (stack[s] != null) continue;
                    stack.RemoveAt(s);
                }

                FillSlotToRadius(i, radiusOffset);
            }
        }

        private void FillSlotToRadius(int slotIndex, float radiusOffset)
        {
            // 현재 base outer를 '월드' 목표로 맞추려면 offset을 고려해야 한다.
            var targetBaseOuter = ensureFilledUntilRadius - radiusOffset;
            var guard = 0;
            while (_nextR[slotIndex] < targetBaseOuter && guard++ < 4096)
            {
                AddNewChunk(slotIndex);
            }
        }

        private void FillSlotToRadius(int slotIndex)
        {
            // Setup에서 호출되는 레거시 경로(초기 offset=0)
            FillSlotToRadius(slotIndex, 0f);
        }

        private float CurrentThickness()
        {
            if (!keepPixelThickness) return randomThickness ? Random.Range(minThickness, maxThickness) : thickness;

            var cam = Camera.main;
            if (cam == null || !cam.orthographic)
            {
                // 카메라를 못 찾으면 폴백
                return randomThickness ? Random.Range(minThickness, maxThickness) : thickness;
            }

            var worldT = SectorGridSolver.WorldThicknessForPixelThickness(cam, targetPixelThickness);
            if (worldT > 0.0001f) return worldT;
            return randomThickness ? Random.Range(minThickness, maxThickness) : thickness;
        }

        private void AddNewChunk(int slotIndex)
        {
            var arc = 360f / chunkCount;
            var thetaCenterDeg = slotIndex * arc + arc * 0.5f;

            var t = CurrentThickness();

            var baseInnerR = _nextR[slotIndex];

            var enemy = Instantiate(sectorPrefab, center.position, Quaternion.identity, transform);

            // 조각별 랜덤 색(원하면 cycle 기반으로 교체)
            var col = Color.HSVToRGB(Random.value, 0.85f, 0.85f);

            enemy.Configure(
                center,
                thetaCenterDeg,
                arc,
                baseInnerR,
                t,
                radialSpeed,
                reachRadius,
                playerCore,
                cellHp,
                angleCells,
                col);

            // VertexColor 적용(머티리얼 공유 유지)
            var rsm = enemy.GetComponent<RingSectorMesh>();
            if (rsm != null) rsm.SetVertexColor(col);

            // 새로 생성된 조각은 다음 Update에서 SetGlobalOffsets로 동기화됨
            _stacks[slotIndex].Add(enemy);
            _nextR[slotIndex] += t;
        }

        private void OnDrawGizmosSelected()
        {
            if (center == null) return;
            Gizmos.color = new Color(1f, 0.2f, 0.6f, 0.2f);
            Gizmos.DrawWireSphere(center.position, ensureFilledUntilRadius);
        }
    }
}
