namespace Polar.Field
{
    /// <summary>
    /// Phase 1 - Step 2: 극좌표 필드 데이터를 시각화하는 뷰 인터페이스
    /// PolarFieldController의 sectorRadii 데이터를 메시/비주얼로 렌더링합니다.
    /// </summary>
    public interface IPolarView
    {
        /// <summary>
        /// PolarFieldController로부터 데이터를 받아 시각화를 업데이트합니다.
        /// </summary>
        /// <param name="controller">데이터 소스 컨트롤러</param>
        void UpdateFromPolarData(PolarFieldController controller);

        /// <summary>
        /// 뷰 초기화 (메시 생성, 머티리얼 설정 등)
        /// </summary>
        void InitializeView(PolarFieldController controller);

        /// <summary>
        /// 뷰 정리 (메시 해제 등)
        /// </summary>
        void CleanupView();

        /// <summary>
        /// 뷰가 활성화되어 있는지 여부
        /// </summary>
        bool IsViewActive { get; }
    }
}
