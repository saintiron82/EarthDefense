using UnityEngine;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// "중앙을 제외하고 화면 전체가 적으로 둘러싸이는" 느낌을 만들기 위한 계산 유틸.
    /// 
    /// 핵심:
    /// - 각도 폭(호의 각, arc)은 동일해도 반지름 r이 커지면 아크 길이(=r*theta)가 커지고,
    ///   링 섹터(두께 t)의 면적도 커집니다.
    /// - 따라서 "호(각도 폭)는 고정" + "반지름/두께 변화"만으로 자연스럽게 면적 차이가 구현됩니다.
    /// </summary>
    public static class SectorGridSolver
    {
        /// <summary>
        /// Orthographic 카메라에서 월드 유닛당 픽셀(PPU 유사값)을 계산합니다.
        /// </summary>
        public static float PixelsPerWorldUnit(Camera cam)
        {
            if (cam == null) return 0f;
            if (!cam.orthographic) return 0f;

            // 화면 높이(픽셀) / 월드 높이(orthographicSize*2)
            var worldHeight = cam.orthographicSize * 2f;
            if (worldHeight <= 0.0001f) return 0f;
            return Screen.height / worldHeight;
        }

        /// <summary>
        /// 목표 픽셀 두께를 유지하도록, 현재 픽셀/월드 비율에서 필요한 월드 두께를 계산합니다.
        /// (Orthographic 카메라에서는 r(거리)에 따라 스케일이 바뀌지 않으므로 단순 환산)
        /// </summary>
        public static float WorldThicknessForPixelThickness(Camera cam, float pixelThickness)
        {
            var ppu = PixelsPerWorldUnit(cam);
            if (ppu <= 0.0001f) return 0f;
            return pixelThickness / ppu;
        }

        /// <summary>
        /// 반지름 r에서 각도 폭(thetaRad)을 가질 때 아크 길이를 구합니다. (길이 = r * theta)
        /// </summary>
        public static float ArcLength(float radius, float thetaRad)
        {
            return Mathf.Max(0f, radius) * Mathf.Max(0f, thetaRad);
        }

        /// <summary>
        /// '링 섹터(annular sector)'의 면적을 구합니다.
        /// - inner 반지름 rInner
        /// - 두께 t
        /// - 각도 폭 thetaRad (라디안)
        /// 
        /// 면적 = 0.5 * theta * (rOuter^2 - rInner^2)
        /// </summary>
        public static float AnnularSectorArea(float rInner, float thickness, float thetaRad)
        {
            rInner = Mathf.Max(0f, rInner);
            var rOuter = Mathf.Max(rInner, rInner + Mathf.Max(0f, thickness));
            thetaRad = Mathf.Max(0f, thetaRad);
            return 0.5f * thetaRad * (rOuter * rOuter - rInner * rInner);
        }

        /// <summary>
        /// 같은 각도 셀 수(angleCells)로 나눌 때, 특정 반지름에서 셀 하나가 차지하는 아크 길이(선분 길이).
        /// (thetaRad / angleCells를 각 셀의 각폭으로 보고, 길이 = r * (thetaRad/angleCells))
        /// </summary>
        public static float CellArcLength(float radius, float thetaRad, int angleCells)
        {
            angleCells = Mathf.Max(1, angleCells);
            return ArcLength(radius, thetaRad / angleCells);
        }
    }
}
