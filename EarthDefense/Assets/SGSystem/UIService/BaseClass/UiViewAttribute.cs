using SG.UI;
using System;
namespace SG.UI
{
    public enum EUIFocusState
    {
        None,
        Modal,
        Fullscreen
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UIViewAttribute : Attribute
    {
        public UIViewAttribute(string viewResourceKey,
            UICanvasType canvasType = UICanvasType.None,
            bool escapeEnable = false,
            bool rootHideLoad = false,
            bool isMultiView = false,
            int showMaxCount = -1,
            bool isAutoTop = true,
            bool focusShowHide = false,
            bool followTarget = false )
        {
            ViewResourceKey = viewResourceKey;
            CanvasType = canvasType;
            EscapeEnable = escapeEnable;
            RootHideLoad = rootHideLoad;
            IsMultiView = isMultiView;
            IsAutoTop = isAutoTop;
            ShowMaxCount = showMaxCount;
            FocusShowHide = focusShowHide;
            // Follow로 정정. 하위 호환을 위해 둘 다 동기화
            FollowTarget = followTarget;
            FallowTarget = FollowTarget;
        }

        // Ui Attach될 레이어 위치 설정
        public UICanvasType CanvasType;
        // 로드할 프리팹 이름 설정(Assets/Resources/UI/Prefabs에 있는 파일명으로 로드)
        public string ViewResourceKey;
        public bool InsInstantiate = true;
        public bool EscapeEnable;
        public bool RootHideLoad = false;
        public bool IsMultiView = false;
        public bool IsAutoTop = false;
        public int ShowMaxCount = -1;
        public bool FocusShowHide = false;
        // 정식 명칭
        public bool FollowTarget = false;
        // 기존 하위호환 필드(오탈자 유지). 추후 제거 가능
        public bool FallowTarget = false;
    }
}
