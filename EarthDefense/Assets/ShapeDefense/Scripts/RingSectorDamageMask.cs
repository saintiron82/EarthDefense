using System;
using UnityEngine;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// `RingSectorMesh`(조각/Chunk 메시)에 '셀(Cell) 단위 침식/파괴'를 적용하기 위한 피해 마스크.
    /// 
    /// 용어(문서 기준):
    /// - 섹터(Sector): 360도를 일정 각도로 나눈 방향 슬롯(slot)
    /// - 조각(Chunk): 한 섹터(slot) 안에서 반지름 A~B를 차지하는 스트립 1개(=RingSectorMesh 1개)
    /// - 셀(Cell): 조각 내부 데미지 단위(각도 x 반지름)
    /// 
    /// 컨셉(HTML 원본과 유사):
    /// - 조각(Chunk)의 각도 범위를 `angleCells`로 나누고, 반지름 방향을 `radialCells`로 나눈 2D 셀을 관리한다.
    /// - 셀마다 누적 피해량을 저장하고, 0..1 침식도(erosion)를 계산한다.
    /// - erosion==1이면 해당 셀은 완전히 제거(메시에서 삼각형 제외).
    /// 
    /// 주의:
    /// - 이 컴포넌트는 '표현'에 집중. 정확한 히트 판정은 별도 수학 로직으로 처리 권장.
    /// - 빈번한 재빌드가 부담이면, 일정 프레임 간격/임계값 넘을 때만 갱신하도록 조절하세요.
    /// </summary>
    [RequireComponent(typeof(RingSectorMesh))]
    public sealed class RingSectorDamageMask : MonoBehaviour
    {
        [Header("Cells")]
        [SerializeField, Range(1, 64)] private int angleCells = 5;

        [Tooltip("케이스 2: 각도 셀을 '각도 간격(도)'로 지정하고 싶다면 사용합니다.")]
        [SerializeField] private bool useAngleStepDeg;

        [SerializeField, Min(0.1f)] private float angleStepDeg = 4f;

        [SerializeField, Range(1, 128)] private int radialCells = 1;

        [Tooltip("케이스 2: 반지름 셀을 '반지름 간격(월드)'로 지정하고 싶다면 사용합니다.")]
        [SerializeField] private bool useRadialStep;

        [SerializeField, Min(0.01f)] private float radialStep = 1f;

        [Header("Erosion")]
        [Tooltip("damageDepth 1.0에 도달하기까지 필요한 누적 피해량. (HP 개념)")]
        [SerializeField, Min(0.001f)] private float cellHp = 30f;  // 기본 데미지 5 기준 6발로 파괴

        [Header("Mode")]
        [Tooltip("켜면 셀은 부분 침식 없이, 누적 데미지가 cellHp 이상이면 즉시 '완전 제거'됩니다.")]
        [SerializeField] private bool discreteCells = true;

        [Tooltip("discreteCells=true일 때, 파괴 직전의 얇은 '파임' 표현을 살짝 줄지(0=없음). ")]
        [SerializeField, Range(0f, 0.25f)] private float preErodeVisual;

        [Header("Debug Gizmos")]
        [Tooltip("씬 뷰에서 데미지 받은 셀의 HP를 텍스트로 표시합니다.")]
        [SerializeField] private bool showCellHpGizmos = false;
        
        [Tooltip("HP 표시를 위한 텍스트 크기")]
        [SerializeField] private float gizmoTextSize = 0.2f;
        
        [Tooltip("HP가 0보다 큰 셀만 표시")]
        [SerializeField] private bool onlyShowDamagedCells = true;

        private RingSectorMesh _sector;
        private float[] _damage; // 0..cellHp

        public int AngleCells => angleCells;
        public int RadialCells => radialCells;
        public float CellHp => cellHp; // HP 시각화용

        private int TotalCells => Mathf.Max(1, angleCells) * Mathf.Max(1, radialCells);

        /// <summary>
        /// cellIndex 셀이 '처음' 파괴되는 순간 1회 호출됩니다.
        /// (보상 지급/사운드/이펙트 트리거용)
        /// </summary>
        public event Action<int> CellDestroyed;

        private void Awake()
        {
            _sector = GetComponent<RingSectorMesh>();
            AutoComputeCellsFromSteps();
            EnsureArray();
        }

        private void OnValidate()
        {
            // 에디터에서만: 값 검증 및 클램핑
            angleCells = Mathf.Clamp(angleCells, 1, 64);
            radialCells = Mathf.Clamp(radialCells, 1, 128);
            angleStepDeg = Mathf.Max(0.1f, angleStepDeg);
            radialStep = Mathf.Max(0.01f, radialStep);

            if (!Application.isPlaying)
            {
                if (_sector == null) _sector = GetComponent<RingSectorMesh>();
                AutoComputeCellsFromSteps();
                EnsureArray();
                if (_sector != null) _sector.SetDamageMask(this);
            }
        }

        private void OnEnable()
        {
            // 런타임: 레지스트리 등록만
            if (_sector == null) _sector = GetComponent<RingSectorMesh>();
            if (_sector != null) _sector.SetDamageMask(this);
            
            DamageableRegistry.Register(this);
        }

        private void OnDisable()
        {
            DamageableRegistry.Unregister(this);
        }

        private void AutoComputeCellsFromSteps()
        {
            if (_sector == null) return;

            if (useAngleStepDeg)
            {
                var arc = Mathf.Clamp(_sector.ArcAngleDeg, 0.01f, 360f);
                angleCells = Mathf.Clamp(Mathf.CeilToInt(arc / Mathf.Max(0.1f, angleStepDeg)), 1, 64);
            }

            if (useRadialStep)
            {
                var t = Mathf.Max(0.0001f, _sector.Thickness);
                radialCells = Mathf.Clamp(Mathf.CeilToInt(t / Mathf.Max(0.01f, radialStep)), 1, 128);
            }
        }

        private void EnsureArray()
        {
            var total = TotalCells;
            if (_damage == null || _damage.Length != total)
            {
                var old = _damage;
                _damage = new float[total];

                if (old != null)
                {
                    var n = Mathf.Min(old.Length, _damage.Length);
                    Array.Copy(old, _damage, n);
                }
            }
        }

        private int ToIndex(int angleIndex, int radialIndex)
        {
            angleIndex = Mathf.Clamp(angleIndex, 0, angleCells - 1);
            radialIndex = Mathf.Clamp(radialIndex, 0, radialCells - 1);
            return radialIndex * angleCells + angleIndex;
        }

        /// <summary>
        /// 0..1 (0=무피해, 1=완전 파괴)
        /// </summary>
        public float GetCellErosion01(int angleIndex, int radialIndex)
        {
            if (_damage == null || _damage.Length == 0) return 0f;
            var idx = ToIndex(angleIndex, radialIndex);
            var d01 = Mathf.Clamp01(_damage[idx] / cellHp);

            if (!discreteCells) return d01;
            if (d01 >= 1f) return 1f;
            if (preErodeVisual <= 0f) return 0f;

            var threshold = 1f - preErodeVisual;
            if (d01 <= threshold) return 0f;
            return Mathf.InverseLerp(threshold, 1f, d01);
        }

        // 기존 1D API 호환: radialIndex=0으로 처리
        public float GetCellErosion01(int cellIndex)
        {
            if (radialCells <= 1) return GetCellErosion01(Mathf.Clamp(cellIndex, 0, angleCells - 1), 0);
            // 2D에서는 1D 호출을 'angleIndex'로만 해석
            return GetCellErosion01(Mathf.Clamp(cellIndex, 0, angleCells - 1), 0);
        }

        public bool IsCellDestroyed(int cellIndex)
        {
            if (_damage == null || _damage.Length == 0) return false;
            cellIndex = Mathf.Clamp(cellIndex, 0, _damage.Length - 1);
            return _damage[cellIndex] >= cellHp;
        }

        public float GetCellDamage(int cellIndex)
        {
            if (_damage == null || _damage.Length == 0) return 0f;
            cellIndex = Mathf.Clamp(cellIndex, 0, _damage.Length - 1);
            return _damage[cellIndex];
        }

        public void DamageByAngleRadius(float hitAngleDeg, float hitRadius, float amount)
        {
            if (_sector == null) _sector = GetComponent<RingSectorMesh>();
            if (_sector == null) return;
            EnsureArray();

            hitAngleDeg = Normalize180(hitAngleDeg);

            var start = Normalize180(_sector.StartAngleDeg);
            var arc = Mathf.Clamp(_sector.ArcAngleDeg, 0.01f, 360f);

            // 각도 인덱스
            int aIdx;
            if (arc >= 359.999f)
            {
                aIdx = Mathf.FloorToInt(Mathf.Repeat(hitAngleDeg + 180f, 360f) / 360f * angleCells);
            }
            else
            {
                float rel = DeltaAngleSigned(start, hitAngleDeg);
                if (rel < 0f) rel += 360f;
                if (rel > arc) return;
                var tA = Mathf.Clamp01(rel / arc);
                aIdx = Mathf.FloorToInt(tA * angleCells);
            }
            aIdx = Mathf.Clamp(aIdx, 0, angleCells - 1);

            // 반지름 인덱스: inner..outer를 0..1로 정규화 후 radialCells로 매핑
            var rInner = _sector.InnerRadius;
            var rOuter = _sector.InnerRadius + _sector.Thickness;
            if (rOuter <= rInner) return;
            var tR = Mathf.InverseLerp(rInner, rOuter, hitRadius);
            int rIdx = Mathf.Clamp(Mathf.FloorToInt(tR * radialCells), 0, radialCells - 1);

            _sector.PulseCellGlow(aIdx, rIdx);
            ApplyDamage(ToIndex(aIdx, rIdx), amount);
        }


        /// <summary>
        /// 셀 인덱스로 직접 데미지를 적용합니다. (이미 인덱스를 계산한 경우 사용)
        /// </summary>
        public void DamageByCellIndex(int angleIndex, int radiusIndex, float amount)
        {
            EnsureArray();
            _sector?.PulseCellGlow(angleIndex, radiusIndex);
            ApplyDamage(ToIndex(angleIndex, radiusIndex), amount);
        }

        private void ApplyDamage(int cellIndex, float amount)
        {
            if (_damage == null || _damage.Length == 0) return;
            if (amount <= 0f) return;

            var wasDestroyed = _damage[cellIndex] >= cellHp;
            _damage[cellIndex] = Mathf.Min(cellHp, _damage[cellIndex] + amount);
            var isDestroyed = _damage[cellIndex] >= cellHp;

            if (!wasDestroyed && isDestroyed)
            {
                // 2D 인덱스에서 angle/radial을 되찾아 이벤트로 보고할 수도 있으나,
                // 현재는 angleIndex만 필요해서 기존 이벤트 시그니처 유지.
                var angleIndex = cellIndex % Mathf.Max(1, angleCells);
                CellDestroyed?.Invoke(angleIndex);
            }

            if (_sector != null)
            {
                _sector.MarkDirtyExternal();
            }
        }

        private static float Normalize180(float deg)
        {
            deg = Mathf.Repeat(deg + 180f, 360f) - 180f;
            return deg;
        }

        private static float DeltaAngleSigned(float fromDeg, float toDeg)
        {
            // Unity의 Mathf.DeltaAngle은 -180..180 반환
            return Mathf.DeltaAngle(fromDeg, toDeg);
        }

        /// <summary>
        /// 주어진 월드 히트 위치를 이 섹터의 (angleIndex, radialIndex) 셀로 매핑합니다.
        /// 섹터 영역 밖이면 false를 반환합니다.
        /// </summary>
        public bool TryGetCellFromWorldPoint(Vector2 worldPoint, out int angleIndex, out int radialIndex)
        {
            angleIndex = 0;
            radialIndex = 0;

            if (_sector == null) _sector = GetComponent<RingSectorMesh>();
            if (_sector == null) return false;

            // 1. 피봇(원점) 기준 극좌표 변환
            var pivotPos = (Vector2)_sector.transform.position;
            var offset = worldPoint - pivotPos;
            
            if (offset.sqrMagnitude <= 0.000001f) return false;

            var hitRadius = offset.magnitude;
            var hitAngleDeg = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;

            // 2. 반지름 범위 체크 (메시가 그려진 링 영역)
            var rInner = _sector.InnerRadius;
            var rOuter = _sector.InnerRadius + _sector.Thickness;
            if (hitRadius < rInner || hitRadius > rOuter) return false;

            // 3. 각도 범위 체크
            var sectorStart = _sector.StartAngleDeg;
            var sectorArc = Mathf.Clamp(_sector.ArcAngleDeg, 0.01f, 360f);
            
            // 각도를 -180~180 범위로 정규화
            hitAngleDeg = Mathf.Repeat(hitAngleDeg + 180f, 360f) - 180f;
            sectorStart = Mathf.Repeat(sectorStart + 180f, 360f) - 180f;

            int aIdx;
            if (sectorArc >= 359.999f)
            {
                // 전체 원: 각도를 0~360으로 매핑
                var normalized = Mathf.Repeat(hitAngleDeg + 180f, 360f);
                aIdx = Mathf.FloorToInt(normalized / 360f * angleCells);
            }
            else
            {
                // 부채꼴: 섹터 각도 범위 내인지 체크
                var deltaAngle = Mathf.DeltaAngle(sectorStart, hitAngleDeg);
                if (deltaAngle < 0f) deltaAngle += 360f;
                
                if (deltaAngle > sectorArc) return false;
                
                var t = Mathf.Clamp01(deltaAngle / sectorArc);
                aIdx = Mathf.FloorToInt(t * angleCells);
            }

            // 4. 반지름 셀 인덱스 계산
            var radialT = Mathf.InverseLerp(rInner, rOuter, hitRadius);
            int rIdx = Mathf.FloorToInt(radialT * radialCells);

            // 5. 인덱스 클램핑
            angleIndex = Mathf.Clamp(aIdx, 0, angleCells - 1);
            radialIndex = Mathf.Clamp(rIdx, 0, radialCells - 1);
            return true;
        }

        /// <summary>
        /// 주어진 월드 히트 위치가 '이미 파괴된 셀'에 해당하면 true.
        /// 섹터 밖이면 false(=막힌/없는 셀이므로 따로 처리 필요)로 둡니다.
        /// </summary>
        public bool IsDestroyedAtWorldPoint(Vector2 worldPoint)
        {
            EnsureArray();
            if (!TryGetCellFromWorldPoint(worldPoint, out var aIdx, out var rIdx)) return false;
            return IsCellDestroyed(ToIndex(aIdx, rIdx));
        }

        /// <summary>
        /// 주어진 월드 포인트가 이 조각(섹터) 영역 안에서 '막힌(solid) 부분'이면 true.
        /// 즉, 해당 셀이 아직 파괴되지 않았다면(=구멍이 아니라면) solid 입니다.
        /// 
        /// - 섹터 밖이면 false (이 조각과 무관)
        /// - 파괴된 셀(구멍)이면 false
        /// - 남아있는 셀이면 true
        /// </summary>
        public bool IsSolidAtWorldPoint(Vector2 worldPoint)
        {
            EnsureArray();
            if (!TryGetCellFromWorldPoint(worldPoint, out var aIdx, out var rIdx)) return false;
            return !IsCellDestroyed(ToIndex(aIdx, rIdx));
        }


        /// <summary>
        /// 셀(각도/반지름 인덱스)로부터 고유 ID를 계산합니다. angleIndex/radiusIndex는 유효 범위여야 합니다.
        /// </summary>
        public int GetCellUniqueId(int angleIndex, int radiusIndex)
        {
            // 간단 조합: (루트 InstanceID * 1_000_000) + (radiusIndex * AngleCells) + angleIndex
            var rootId = transform.root.GetInstanceID();
            return rootId * 1_000_000 + radiusIndex * Mathf.Max(1, AngleCells) + angleIndex;
        }

        /// <summary>
        /// 아직 파괴되지 않은 셀이 하나라도 남아있는지 확인합니다.
        /// </summary>
        public bool HasIntactCell()
        {
            if (_damage == null || _damage.Length == 0) return true;
            var total = _damage.Length;
            var hp = cellHp;
            for (int i = 0; i < total; i++)
            {
                if (_damage[i] < hp)
                {
                    return true;
                }
            }
            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showCellHpGizmos) return;
            if (_sector == null) _sector = GetComponent<RingSectorMesh>();
            if (_sector == null || _damage == null) return;

            var pivotPos = transform.position;
            
            // 각 셀의 중심 위치에 HP 표시
            for (int rIdx = 0; rIdx < radialCells; rIdx++)
            {
                for (int aIdx = 0; aIdx < angleCells; aIdx++)
                {
                    int cellIdx = ToIndex(aIdx, rIdx);
                    float dmg = _damage[cellIdx];
                    
                    // onlyShowDamagedCells가 켜져있으면 데미지 받은 셀만 표시
                    if (onlyShowDamagedCells && dmg <= 0.001f) continue;
                    
                    // 셀의 월드 위치 계산
                    var cellPos = GetCellWorldPosition(aIdx, rIdx);
                    
                    // HP 비율에 따라 색상 변경
                    float hpRatio = 1f - Mathf.Clamp01(dmg / cellHp);
                    Color color = Color.Lerp(Color.red, Color.green, hpRatio);
                    
                    // 파괴된 셀은 검정색
                    if (IsCellDestroyed(cellIdx))
                    {
                        color = Color.black;
                    }
                    
                    // 텍스트 그리기
                    UnityEditor.Handles.color = color;
                    var style = new GUIStyle();
                    style.normal.textColor = color;
                    style.fontSize = Mathf.RoundToInt(gizmoTextSize * 100f);
                    style.alignment = TextAnchor.MiddleCenter;
                    
                    string text = IsCellDestroyed(cellIdx) ? "X" : $"{dmg:F0}/{cellHp:F0}";
                    UnityEditor.Handles.Label(cellPos, text, style);
                    
                    // 셀 경계 표시 (선택적)
                    Gizmos.color = new Color(color.r, color.g, color.b, 0.3f);
                    Gizmos.DrawWireSphere(cellPos, 0.1f);
                }
            }
        }
        
        /// <summary>
        /// 셀의 월드 위치를 계산합니다 (시각화용)
        /// </summary>
        private Vector3 GetCellWorldPosition(int angleIndex, int radiusIndex)
        {
            if (_sector == null) return transform.position;
            
            // 셀의 중심 각도 계산
            float sectorStart = _sector.StartAngleDeg;
            float sectorArc = _sector.ArcAngleDeg;
            float cellArcSize = sectorArc / Mathf.Max(1, angleCells);
            float cellAngle = sectorStart + (angleIndex + 0.5f) * cellArcSize;
            
            // 셀의 중심 반지름 계산
            float innerR = _sector.InnerRadius;
            float outerR = innerR + _sector.Thickness;
            float cellRadialSize = _sector.Thickness / Mathf.Max(1, radialCells);
            float cellRadius = innerR + (radiusIndex + 0.5f) * cellRadialSize;
            
            // 극좌표 -> 직교좌표 변환
            float angleRad = cellAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angleRad) * cellRadius,
                Mathf.Sin(angleRad) * cellRadius,
                0f
            );
            
            return transform.position + offset;
        }
#endif
    }
}

