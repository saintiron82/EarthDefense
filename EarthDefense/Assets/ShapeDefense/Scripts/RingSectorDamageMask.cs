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
        [SerializeField, Min(0.001f)] private float cellHp = 60f;

        [Header("Mode")]
        [Tooltip("켜면 셀은 부분 침식 없이, 누적 데미지가 cellHp 이상이면 즉시 '완전 제거'됩니다.")]
        [SerializeField] private bool discreteCells = true;

        [Tooltip("discreteCells=true일 때, 파괴 직전의 얇은 '파임' 표현을 살짝 줄지(0=없음). ")]
        [SerializeField, Range(0f, 0.25f)] private float preErodeVisual = 0.0f;

        private RingSectorMesh _sector;
        private float[] _damage; // 0..cellHp

        public int AngleCells => angleCells;
        public int RadialCells => radialCells;

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
            angleCells = Mathf.Clamp(angleCells, 1, 64);
            radialCells = Mathf.Clamp(radialCells, 1, 128);
            angleStepDeg = Mathf.Max(0.1f, angleStepDeg);
            radialStep = Mathf.Max(0.01f, radialStep);

            if (_sector == null) _sector = GetComponent<RingSectorMesh>();

            // step 기반이면 현재 섹터 파라미터로 셀 수를 자동 갱신
            AutoComputeCellsFromSteps();

            EnsureArray();

            if (_sector != null)
            {
                _sector.SetDamageMask(this);
            }
        }

        private void OnEnable()
        {
            if (_sector == null) _sector = GetComponent<RingSectorMesh>();
            AutoComputeCellsFromSteps();
            EnsureArray();
            if (_sector != null) _sector.SetDamageMask(this);
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

            ApplyDamage(ToIndex(aIdx, rIdx), amount);
        }

        // 기존 API 유지(각도만): radialIndex=0에 데미지
        public void DamageByAngle(float hitAngleDeg, float amount)
        {
            DamageByAngleRadius(hitAngleDeg, _sector != null ? _sector.InnerRadius : 0f, amount);
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
    }
}
