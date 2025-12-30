using Cat;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

namespace SG.UI
{
    public interface IUIData
    {

    }
    public interface IUIView
    {
        public bool IsBuild { get; }
        public void Build();
        public GameObject GameObject { get; }
        public bool Assert();
    }
    public interface IUIEscape
    {
        public bool IsClosed { get; }
        public void Close();
    }

    public interface IMonoUpdateUIView
    {
        public void Update();

        public void AddUpdateAction( Action action );
        public void RemoveUpdateAction( Action action );
    }

    public interface ILocalizeChangedCallback
    {
        /// <summary>/// 로컬라이즈가 변경되었을때 호출되는 콜백/// </summary>
        public void OnLocalizeChanged(Locale locale);
    }

    public interface IUIPresenter : IUIEscape
    {
        public string Id { get; }
        public bool Loaded { get; set; }
        public bool Opened { get; set; }
        public IUIView View { get; }
        public IUIData UIData { get; }
        public Type ViewType { get; }
        public string ViewPrefabName { get; }
        public UIViewAttribute UiViewAttribute { get; }

        public void Init();
        public void BindView(IUIView view);

        /// <summary>
        /// VIEW 와 항상 발생하게 되는 이벤트
        /// </summary>
        public void AddStaticEvent();

        /// <summary>
        /// View를 오픈할때마다 다시 설정해야 하는 이벤트
        /// </summary>
        public void RunOpenEvent();
        public void Open();
        public void Open(Action<IUIPresenter> onOpen, Action onClose, IUIData data, params object[] parameters );
        public void Release();
        public void SetOption(string option);
    }

    public class UICurrencyUtil
    {
        
    }
    

    public interface IUIGroupElement
    {
        public void OnSelect();
        public void OnDeselect();
        public void Select();   //스스로 선택된 경우
        public void Deselect(); //스스로 선택을 취소한 경우
    }

    public interface iGroupElement
    {
        void SetEmpty();
        void SetSelect();
        void SetDeselect();
    }

    public enum UIEventType
    {
        Click,
        Select,
        Deselect,
        Slide,
        Toggle,
        Enter,
        Exit,
    }

    public interface iUIEventSender
    {
        public iUIEventReceiver EventReceiver { get; }

        public void Init( iUIEventReceiver receiver );
        public void RefreshCellView();
    }

    public interface iUIEventReceiver
    {
        public void OnReceiveUIEvent( iUIEventSender child, UIEventType uIEventType);
    }

    public class UICompanentEvent : UnityEvent<Component> { }
    public class UISlideEvent : UnityEvent<float> { }
    public class UISlideComnpanentEvent : UnityEvent<Component, float> { }
    public class UIToggleEvent : UnityEvent<bool> { }
    public class UIToggleComnpanenEvent : UnityEvent<Component, bool> { }
}
