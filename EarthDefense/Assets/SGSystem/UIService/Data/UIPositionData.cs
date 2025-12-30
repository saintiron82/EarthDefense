using UnityEngine;

namespace SG.UI
{
    /// <summary>
    /// UI 위치 정보를 저장하기 위한 직렬화 가능한 데이터 클래스
    /// </summary>
    [System.Serializable]
    public class UIPositionData
    {
        [SerializeField] public string uiPresenterName;                // DESC :: UI 식별자
        [SerializeField] public float xRatio;                 // DESC :: X축 화면 비율 (0~1)
        [SerializeField] public float yRatio;                 // DESC :: Y축 화면 비율 (0~1)
        [SerializeField] public Vector2 absolutePosition;     // DESC :: 절대 위치 (백업용)
        [SerializeField] public Vector2 canvasSize;           // DESC :: 저장 당시 캔버스 크기
        [SerializeField] public Vector2 screenResolution;     // DESC :: 저장 당시 화면 해상도
        [SerializeField] public string timestamp;             // DESC :: 저장 시간
    }
}