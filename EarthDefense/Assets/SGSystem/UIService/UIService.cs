using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using Cysharp.Threading.Tasks;
using SG.LocaleService;

namespace SG.UI
{

    public class UIEntity
    {
        public IUIPresenter presenter { get; set; }
        public Type ViewType { get; set; }
        public IUIView View { get; set; }
        public bool IsLoading { get; set; }
        public bool IsComplete { get; set; }
    }

    public enum UICanvasType
    {
        None,
        Hud,
        Static,
        Common,
        MainMenu,
        Content,
        ContentPopup,
    }

    public class UILayerRootData
    {
        public UICanvasType m_CanvasType;
        public Canvas m_Canvas;
    }

    public class UIStack
    {
        private Stack<IUIEscape> _uIStacks;
        public UIStack()
        {
            _uIStacks = new Stack<IUIEscape>();
        }
        public int Count => _uIStacks.Count;

        public void Push(IUIEscape ui)
        {
            _uIStacks.Push(ui);
        }

        public IUIEscape Pop()
        {
            if (_uIStacks.Count > 0)
                return _uIStacks.Pop();
            return null;
        }

        public IUIEscape Peek()
        {
            if (_uIStacks.Count > 0)
                return _uIStacks.Peek();
            return null;
        }

        public void Clear()
        {
            //_uIStacks queue가 빌때까지 순회하면서 Pop을 시행한다.
            while (_uIStacks.Count > 0)
            {
                var targetUI = Pop();
                if (targetUI != null)
                {
                    targetUI.Close();
                }
            }
        }

        public bool CheckAndPop(IUIEscape ui)
        {
            var topUI = Peek();
            if (topUI == ui)
            {
                Pop();
                return true;
            }
            return false;
        }
    }


    public class UIService : ServiceBase, IDoUpdate
    {
        public static int UIDefaultWidth = 1080;
        public static UIService Instance { get; private set; }

        private IUIPresenter _currentPresenter = null;
        public IUIPresenter CurrentPresenter => _currentPresenter;

        #region Common

        [Header("[Common]")][SerializeField] private EventSystem m_UIEventSystem = null;

        public EventSystem UIEventSystem
        {
            get { return m_UIEventSystem; }
        }

        [SerializeField] private UIRoot _uiRoot = null;
        public UIRoot UIRoot
        {
            get
            {
                if (_uiRoot == null)
                {
                    _uiRoot = GameObject.FindAnyObjectByType<UIRoot>();
                }
                return _uiRoot;
            }
        }
        private readonly Dictionary<Type, UIEntity> _entities = new();
        private readonly Dictionary<string, UIView> _cachedPrefabDictionary = new();

        private Dictionary<UICanvasType, List<GameObject>> _enabledContentPopupUIList = new Dictionary<UICanvasType, List<GameObject>>();

        //Multiple UI를 열었을때 뒤로가기 버튼을 눌렀을때 처리하기위한 스택
        private Dictionary<Type, IMultiUIContainer> _MultiUIContainer = new Dictionary<Type, IMultiUIContainer>();


        private bool _isBlockInput = false;
        private UIStack _uiStack = new UIStack();

        private UIDynamicLoadManager dynamicLoadManager;
        public UIDynamicLoadManager DynamicLoadManager
        {
            get
            {
                if (dynamicLoadManager == null)
                {
                    dynamicLoadManager = GameObject.FindAnyObjectByType<UIDynamicLoadManager>(); // DESC :: FindObjectOfType보다 성능이 개선된 메서드 사용
                }
                return dynamicLoadManager;
            }
        }


        #endregion Common

        public override async UniTask<bool> Init()
        {
            await base.Init();
            Instance = this;
            LocalizeService.SetLcaleChangeCallback(OpendUIPresenterChangeLanguage);
            CreateUIRootData();
            _uiStack.Clear();
            return true;
        }

        public override async UniTask<bool> Prepare()
        {
            await base.Prepare();

            return true;
        }

        public void CloseAllOpneUI(UICanvasType canvasType = UICanvasType.Content)
        {
            foreach (var entity in _entities)
            {
                var presenter = entity.Value.presenter;
                var view = entity.Value.View;
                if (view != null)
                {
                    var attribute = view.GetType().GetCustomAttribute<UIViewAttribute>();
                    if (attribute != null && attribute.CanvasType == canvasType)
                    {
                        if (presenter != null && presenter.Opened)
                            entity.Value.presenter.Close();
                    }
                }
            }
        }


        public static void OpendUIPresenterChangeLanguage(Locale language)
        {
            foreach (var presenter in Instance._enabledContentPopupUIList)
            {
                foreach (var uiObject in presenter.Value)
                {
                    if (uiObject.TryGetComponent(out ILocalizeChangedCallback localizeCallback))
                    {
                        localizeCallback.OnLocalizeChanged(language);
                    }
                }
            }
        }

        public void ReleaseAllUI()
        {
            foreach (var entity in _entities)
            {
                var presenter = entity.Value.presenter;
                if (presenter != null)
                {
                    presenter.Release();
                }
            }
            _entities.Clear();
        }

        public override void Release()
        {
            Debug.Log("ReleaseSystem : UISystem");
            ReleaseAllUI();
            base.Release();
        }

        public override void Destroy()
        {
            base.Destroy();
        }

        private void CreateUIRootData()
        {
            if (_uiRoot == null)
            {
                _uiRoot = GameObject.FindAnyObjectByType<UIRoot>();
            }
        }

        public void GenerateUIPresenter<P, V>() where P : UIPresenter<V>, new()
            where V : UIView
        {
            var presenterType = typeof(P);
            var viewType = typeof(V);
            if (_entities.ContainsKey(presenterType) == false)
            {
                var newPresenter = new P() as UIPresenter<V>;
                var entity = new UIEntity()
                {
                    presenter = newPresenter,
                    ViewType = viewType,
                };
                _entities.Add(presenterType, entity);
            }
        }

        public void GenerateUIPresenter(Type presenterType)
        {
            if (_entities.ContainsKey(presenterType) == false)
            {
                var newPresenter = Activator.CreateInstance(presenterType) as IUIPresenter;
                var entity = new UIEntity()
                {
                    presenter = newPresenter as IUIPresenter,
                    ViewType = newPresenter.ViewType,
                    IsLoading = false,
                    IsComplete = false,
                };
                _entities.Add(presenterType, entity);
            }
        }

        public UIEntity FindByPresenterType(Type type)
        {
            if (_entities.TryGetValue(type, out var pv))
            {
                return pv;
            }
            else
            {

            }

            return default;
        }

        public T FindByPresenterType<T>() where T : class, IUIPresenter
        {
            var type = typeof(T);
            if (_entities.TryGetValue(type, out var pv))
            {
                return pv.presenter as T;
            }
            else
            {

            }

            return default;
        }

        public void CloseByPresenterType<T>() where T : class, IUIPresenter
        {
            var type = typeof(T);
            if (_entities.TryGetValue(type, out var pv))
            {
                if (pv.presenter != null)
                {
                    pv.presenter.Close();
                }
            }
        }

        public void CloseByPresenterType(Type type)
        {
            if (_entities.TryGetValue(type, out var pv))
            {
                if (pv.presenter != null)
                {
                    pv.presenter.Close();
                }
            }
        }

        public void SetCanvas(UICanvasType uICanvasType, GameObject uiObject)
        {
            if (uiObject != default)
            {
                var targetCanvasRoot = _uiRoot.GetRoot(uICanvasType);
                if (targetCanvasRoot == null)
                {

                }
                else
                {
                    if (targetCanvasRoot.transform != null)
                    {
                        uiObject.transform.ExRectTransformNormalize(targetCanvasRoot.GetComponent<RectTransform>());
                    }
                }
            }
        }


        private void SetCanvas(UIView view)
        {
            Type type = view.GetType();
            var attribute = type.GetCustomAttribute<UIViewAttribute>();
            if (attribute != default)
            {
                var targetCanvas = _uiRoot.GetCanvas(attribute.CanvasType);
                if (targetCanvas == null)
                {

                }
                else
                {
                    if (targetCanvas.transform != null)
                    {
                        view.transform.SetParent(targetCanvas.transform, false);
                        view.transform.ExNormalize();
                    }
                }
            }
        }

        public void DeActiveAllCanavas()
        {
            foreach (var canvas in _uiRoot.UIRootCanvasList)
            {
                canvas.Value.Canvas.gameObject.SetActive(false);
            }
        }
        public void SetEnabledCanvas(UICanvasType uICanvasType, bool enabled)
        {
            var targetCanvas = _uiRoot.GetCanvas(uICanvasType);
            if (targetCanvas != null)
                SetEnabledCanvas(targetCanvas, enabled);
        }
        public void SetEnabledCanvas(Canvas targetCanvas, bool enabled)
        {
            if (targetCanvas != null)
            {
                targetCanvas.gameObject.SetActive(enabled);
            }
        }

        private void SetCanvas(UIViewAttribute attribute, UIView view, bool normalize = true)
        {
            if (attribute != default)
            {
                var targetCanvas = _uiRoot.GetRoot(attribute.CanvasType);
                if (targetCanvas == null)
                {

                }
                else
                {
                    var canvas = targetCanvas.GetComponentInParent<Canvas>();
                    view.SetCanvas(canvas);
                    if (targetCanvas.transform != null)
                    {
                        view.transform.SetParent(targetCanvas.transform, false);
                        if (normalize)
                            view.transform.ExNormalize();


                        if (targetCanvas.transform != null)
                        {
                            view.FitToParent(targetCanvas.GetComponent<RectTransform>());

                        }
                    }
                }
            }
        }
        private void SetCanvas(UIViewAttribute attribute, GameObject view, bool normalize = true)
        {
            if (attribute != default)
            {
                var targetCanvas = _uiRoot.GetRoot(attribute.CanvasType);
                if (targetCanvas == null)
                {

                }
                else
                {
                    if (targetCanvas.transform != null)
                    {
                        view.transform.SetParent(targetCanvas.transform, false);
                        if (normalize)
                            view.transform.ExNormalize();
                    }
                }
            }
        }

        public bool IsOpen<T>() where T : class, IUIPresenter
        {
            var targetPresenter = FindByPresenterType<T>();
            if (targetPresenter != null)
            {
                return targetPresenter.Opened;
            }
            return false;
        }

        public static void InputBlockCanvasToggle(bool active)
        {
            if (Instance != null && Instance.UIRoot != null)
            {
                Instance.UIRoot.InputBlockCanvasToggle(active);
            }
        }

        public static void OpenUI<T>() where T : class, IUIPresenter, new()
        {
            // DESC :: 이미 열려있는 UI인지 체크하고 열려있다면 닫기
            if (Instance.IsOpen<T>())
            {
                Instance.Close<T>();
                return;
            }

            Instance.Open_Async<T>().Forget();
        }

        public static T GetUI<T>() where T : class, IUIPresenter, new()
        {
            return Instance.FindByPresenterType<T>();
        }

        public async UniTask<T> Open_Async<T>(Action<IUIPresenter> openAction = null, Action closeAction = null, IUIData uIData = null, bool currentPresenterClose = true) where T : class, IUIPresenter, new()
        {
            Debug.Log($"Open_Async : {typeof(T).Name}");
            if (_currentPresenter != null && currentPresenterClose)
            {
                if (_currentPresenter is T)
                {
                    _currentPresenter.Close();
                    _currentPresenter = null;

                    return null;
                }
                else
                {
                    _currentPresenter.Close();
                    _currentPresenter = null;
                }
            }

            if (FindByPresenterType(typeof(T)) is var entity && entity != null &&
                entity.presenter is var presenter != default)
            {
                if (entity.IsComplete == false && entity.IsLoading)
                {
                    await UniTask.WaitUntil(() => entity.IsComplete);
                    return entity.presenter as T;
                }

                var viewComponent = default(IUIView);
                var uiView = default(UIView);
                if (presenter.View != default)
                {
                    viewComponent = presenter.View;
                    uiView = viewComponent as UIView;
                }
                else
                {
                    entity.IsLoading = true;
                    var viewType = entity.ViewType;
                    var attribute = presenter.UiViewAttribute;
                    var viewPrefabName = attribute.ViewResourceKey;

                    // DESC :: View 로드 전에 미리 비활성화 상태로 생성 준비
                    viewComponent = await LoadView_Async(viewPrefabName, attribute.InsInstantiate);
                    if (viewComponent == null)
                    {
                        Debug.LogError($"Open_Async - UIView--{viewPrefabName}-- is null");
                        return null;
                    }

                    uiView = viewComponent as UIView;

                    // DESC :: 깜빡임 방지를 위해 View를 먼저 비활성화
                    if (uiView != null)
                    {
                        uiView.gameObject.SetActive(false);
                    }

                    SetCanvas(attribute, uiView);

                    if (viewComponent != null)
                    {
                        presenter.BindView(viewComponent);
                        entity.presenter = presenter;
                        entity.ViewType = viewType;
                    }

                    // DESC :: 레이아웃 계산 완료를 위해 한 프레임 대기
                    await UniTask.NextFrame();

                    // DESC :: Canvas 레이아웃 강제 갱신
                    if (uiView != null)
                    {
                        var canvas = uiView.GetComponentInParent<Canvas>();
                        if (canvas != null)
                        {
                            Canvas.ForceUpdateCanvases();
                        }
                        uiView.transform.GetComponent<RectTransform>()?.ForceUpdateRectTransforms();
                    }
                }

                await UniTask.WaitUntil(() => presenter.Loaded);
                entity.IsLoading = false;

                // DESC :: 모든 준비가 완료된 후 Open 호출 (이때 SetActive(true) 실행)
                presenter.Open(openAction, closeAction, uIData);
                presenter.Opened = true;
                entity.IsComplete = true;
                if (presenter.UiViewAttribute.CanvasType == UICanvasType.Content)
                {
                    _currentPresenter = presenter;
                }
                return presenter as T;
            }
            else
            {
                var presenterType = typeof(T);
                GenerateUIPresenter(presenterType);
                var result = await Open_Async<T>(openAction, closeAction, uIData, currentPresenterClose);
                if (result.UiViewAttribute.CanvasType == UICanvasType.Content)
                {
                    _currentPresenter = result;
                }
                return result;
            }
        }

        public async UniTask<T> Open_Async<T>(Action<IUIPresenter> openAction = null, Action closeAction = null, IUIData uIData = null, params object[] parameters) where T : class, IUIPresenter, new()
        {
            Debug.Log($"Open_Async : {typeof(T).Name}");

            if (FindByPresenterType(typeof(T)) is var entity && entity != null &&
                entity.presenter is var presenter != default)
            {
                if (entity.IsComplete == false && entity.IsLoading)
                {
                    await UniTask.WaitUntil(() => entity.IsComplete);
                    return entity.presenter as T;
                }

                var viewComponent = default(IUIView);
                var uiView = default(UIView);
                if (presenter.View != default)
                {
                    viewComponent = presenter.View;
                    uiView = viewComponent as UIView;
                }
                else
                {
                    entity.IsLoading = true;
                    var viewType = entity.ViewType;
                    var attribute = presenter.UiViewAttribute;
                    var viewPrefabName = attribute.ViewResourceKey;
                    viewComponent = await LoadView_Async(viewPrefabName, attribute.InsInstantiate);
                    if (viewComponent == null)
                    {
                        Debug.LogError($"Open_Async - UIView--{viewPrefabName}-- is null");
                        return null;
                    }
                    uiView = viewComponent as UIView;
                    SetCanvas(attribute, uiView);
                    if (viewComponent != null)
                    {
                        presenter.BindView(viewComponent);
                        entity.presenter = presenter;
                        entity.ViewType = viewType;
                    }

                }

                await UniTask.WaitUntil(() => presenter.Loaded);
                entity.IsLoading = false;
                presenter.Open(openAction, closeAction, uIData, parameters);
                entity.IsComplete = true;
                return presenter as T;
            }
            else
            {
                var presenterType = typeof(T);
                GenerateUIPresenter(presenterType);
                var result = await Open_Async<T>(openAction, closeAction, uIData, parameters);
                return result;
            }
        }
        public void Close(UIView view)
        {
            if (view != default)
            {
                view.gameObject.SetActive(false);
            }
        }

        public bool Close<T>() where T : class, IUIPresenter
        {
            var type = typeof(T);
            if (FindByPresenterType(typeof(T)) is var entity && entity != null &&
                entity.presenter is var presenter != default)
            {
                if (presenter.View != default)
                {
                    presenter.Close();
                    return true;
                }
            }

            return false;
        }

        public static void CloseUI<T>() where T : class, IUIPresenter
        {
            Instance.Close<T>();
        }

        public async UniTask<UIView> LoadView_Async(string viewName, bool Instantiate = true)
        {
            if (_cachedPrefabDictionary.TryGetValue(viewName, out var prefab))
            {
                return Instantiate ? GameObject.Instantiate(prefab) : prefab;
            }

            var targetObject = await DynamicLoadManager.UIViewDynamicLoad(viewName);
            if (targetObject != null)
            {
                _cachedPrefabDictionary[viewName] = targetObject;
                return Instantiate ? GameObject.Instantiate(targetObject) : targetObject;
            }

            Debug.LogError($"LoadView_Async - UIView {viewName} not found");
            return null;
        }


        public GameObject LoadStaticView(string viewName, bool Instantiate = true)
        {
            var targetInfo = dynamicLoadManager.GetStaticUIObject(viewName);
            if (targetInfo != null)
            {
                var targetObject = targetInfo.prefab;
                if (Instantiate && targetObject != default)
                {
                    return GameObject.Instantiate(targetObject);
                }
                else
                {
                    return targetObject;
                }
            }
            Debug.LogError($"{viewName} UI Not Find int StaticGroup");
            return null;
        }

        public async UniTask<T> CreateDynamicPresenter_Async<T>(Transform InstantiateParent, bool InsInstantiate, Action<IUIPresenter> openAction = null) where T : class, IUIPresenter, new()
        {
            await UniTask.Yield();
            var presenterType = typeof(T);
            var presenter = Activator.CreateInstance(presenterType) as IUIPresenter;
            var viewType = presenter.ViewType;
            var viewPrefabName = presenter.ViewPrefabName;

            var attribute = viewType.GetCustomAttribute<UIViewAttribute>();

            var viewComponent = await LoadView_Async(viewPrefabName, InsInstantiate);
            if (viewComponent == null)
            {
                Debug.LogError($"CreateDynamicPresenter_Async  - UIView --[ {viewPrefabName} ] is null");
                return null;
            }

            viewComponent.transform.SetParent(InstantiateParent);
            viewComponent.transform.ExNormalize();

            if (viewComponent.TryGetComponent(out RectTransform rt))
            {
                // DESC :: Stretch일때
                if (rt.anchorMax == Vector2.one && rt.anchorMin == Vector2.zero)
                {
                    rt.sizeDelta = Vector2.zero;
                }
            }

            var uiView = viewComponent;
            if (viewComponent != null)
            {
                presenter.BindView(viewComponent);
            }
            openAction?.Invoke(presenter);
            return presenter as T;
        }

        public static T CreateDynamicPresenter<T>(Transform instantiateParent, bool insInstantiate) where T : class, IUIPresenter, new()
        {
            return Instance.CreateDynamicPresenterSync<T>(instantiateParent, insInstantiate);
        }

        public T CreateDynamicPresenterSync<T>(Transform instantiateParent, bool insInstantiate) where T : class, IUIPresenter, new()
        {
            var presenterType = typeof(T);
            var presenter = Activator.CreateInstance(presenterType) as IUIPresenter;
            var viewType = presenter.ViewType;
            var viewPrefabName = presenter.ViewPrefabName;

            if (_entities.ContainsKey(presenterType) == false)
            {
                var entity = new UIEntity()
                {
                    presenter = presenter,
                    ViewType = viewType,
                };
                _entities.Add(presenterType, entity);
            }

            var viewGameObject = LoadStaticView(viewPrefabName, insInstantiate);
            if (viewGameObject == null)
            {
                Debug.LogError($"CreateDynamicPresenterSync - UIView --[ {viewPrefabName} ] is null");
                return null;
            }

            var uiViewComponent = viewGameObject.GetComponent(viewType) as IUIView;
            if (uiViewComponent != null)
            {
                viewGameObject.transform.SetParent(instantiateParent);
                viewGameObject.transform.ExNormalize();

                if (viewGameObject.TryGetComponent(out RectTransform rt))
                {
                    // DESC :: Stretch일때
                    if (rt.anchorMax == Vector2.one && rt.anchorMin == Vector2.zero)
                    {
                        rt.sizeDelta = Vector2.zero;
                    }
                }
                uiViewComponent.GameObject.SetActive(false); // DESC :: 디폴트는 꺼진상태로
                presenter.BindView(uiViewComponent);
            }


            return presenter as T;
        }

        #region Stack

        public void PushUIStack(IUIEscape ui)
        {
            _uiStack.Push(ui);
        }

        public int GetUIStackCount()
        {
            return _uiStack.Count;
        }
        public IUIEscape PopUIStack(bool autoClose)
        {
            var popui = _uiStack.Peek();
            if (popui != null && autoClose)
            {
                popui.Close();
                if (popui.IsClosed == true && _uiStack.Peek() == popui)
                {
                    _uiStack.Pop();
                }
            }

            return popui;
        }

        public IUIEscape PeekUIStack()
        {
            return _uiStack.Peek();
        }

        public bool CheckAndPop(IUIEscape ui)
        {
            return _uiStack.CheckAndPop(ui);
        }

        public void ClearUIStack()
        {
            _uiStack.Clear();
        }

        public void BlockInput(bool block)
        {
            _isBlockInput = block;
        }

        public void AddEnabledContentPopupUI(UICanvasType canvasType, GameObject uiObject)
        {
            if (_enabledContentPopupUIList.ContainsKey(canvasType) == false)
            {
                _enabledContentPopupUIList.Add(canvasType, new List<GameObject>());
            }
            var uiList = _enabledContentPopupUIList[canvasType];
            if (uiList.Contains(uiObject) == false)
            {
                uiList.Add(uiObject);
            }
            UpdateCurrentCamera(canvasType, uiList);
        }

        public void DissableUI(UICanvasType canvasType, GameObject uiObject)
        {
            if (_enabledContentPopupUIList.ContainsKey(canvasType))
            {
                var targetList = _enabledContentPopupUIList[canvasType];
                if (_enabledContentPopupUIList[canvasType].Contains(uiObject))
                {
                    _enabledContentPopupUIList[canvasType].Remove(uiObject);
                    UpdateCurrentCamera(canvasType, targetList);
                }
            }
        }

        private void UpdateCurrentCamera(UICanvasType canvasType, List<GameObject> uiList)
        {
            var enabledUI = false;
            foreach (var ui in uiList)
            {
                if (ui != null && ui.activeSelf)
                {
                    enabledUI = true;
                    break;
                }
            }

            _uiRoot.SetActiveTargetCanvas(canvasType, enabledUI);

            if (enabledUI)
                EnableUICanvas(canvasType);
            else
                DisableUICanvas(canvasType);
        }

        public void EnableUIRoot(UICanvasType canvasType, bool enabled)
        {
            if (_uiRoot == null)
                return;

            var root = _uiRoot.GetRoot(canvasType);
            if (root != null)
            {
                root.gameObject.SetActive(enabled);
            }
        }

        public void EnableUICanvas(UICanvasType canvasType)
        {
            var targetCanvas = _uiRoot.GetCanvas(canvasType);
            if (targetCanvas != null)
            {
                targetCanvas.gameObject.SetActive(true);
            }
        }

        public void DisableUICanvas(UICanvasType canvasType)
        {
            var targetCanvas = _uiRoot.GetCanvas(canvasType);
            if (targetCanvas != null)
            {
                targetCanvas.gameObject.SetActive(false);
            }
        }

        public void UpdateUIEntityState(Type presenterType, bool isComplete, bool isLoading)
        {
            // DESC :: UIEntity의 상태를 업데이트하는 헬퍼 메서드
            if (_entities.TryGetValue(presenterType, out var entity))
            {
                entity.IsComplete = isComplete;
                entity.IsLoading = isLoading;
            }
        }

        public void ClearCurrentPresenter(IUIPresenter presenter)
        {
            // DESC :: 현재 프레젠터가 닫히는 프레젠터와 같은 경우에만 null로 설정
            if (_currentPresenter == presenter)
            {
                _currentPresenter = null;
            }
        }

        public void DoUpdate()
        {
        }

        private MultiUIContainer<T> GetContainerForType<T>() where T : class, IUIPresenter, new()
        {
            var type = typeof(T);
            if (!_MultiUIContainer.TryGetValue(type, out var baseContainer))
            {
                var container = new MultiUIContainer<T>();
                container.Init(() => CreateMultiPresenter_Async<T>());
                _MultiUIContainer[type] = container;
                return container;
            }
            return baseContainer as MultiUIContainer<T>;
        }

        public bool IsOpenedMultiUI<T>(string id) where T : class, IUIPresenter, new()
        {
            var type = typeof(T);
            var isOpened = false;
            if (_MultiUIContainer.TryGetValue(type, out var container))
            {
                var multiContainer = container as MultiUIContainer<T>;
                if (multiContainer != null)
                {
                    var multiView = multiContainer.Get(id);
                    if (multiView != null)
                    {
                        isOpened = multiView.Opened;
                    }
                    else
                    {
                        Debug.LogWarning($"IsOpenedMultiUI - MultiUIContainer for {type.Name} does not contain id: {id}");
                    }
                }
                else
                {
                    Debug.LogError($"IsOpenedMultiUI - MultiUIContainer for {type.Name} is not of type MultiUIContainer<{typeof(T).Name}>");
                }
            }

            return isOpened;
        }

        public async UniTask<T> GetMultiUI_Async<T>(string id, bool open) where T : class, IUIPresenter, new()
        {
            var container = GetContainerForType<T>();
            bool isPossibleOpen = false;
            if (container != null)
            {
                var presenter = container.Get(id);
                if (presenter == null)
                {
                    presenter = await container.GetNewPresenter();
                    var typeAttribute = presenter.UiViewAttribute;
                    if (typeAttribute.ShowMaxCount == -1)
                    {
                        isPossibleOpen = true;
                    }
                    else
                    {
                        isPossibleOpen = container.Count < typeAttribute.ShowMaxCount;
                    }

                    if (isPossibleOpen == true)
                    {
                        if (presenter == null)
                        {
                            presenter = await CreateMultiPresenter_Async<T>();
                        }
                    }
                }
                if (presenter != null)
                {
                    var typeAttribute = presenter.UiViewAttribute;
                    if (typeAttribute.ShowMaxCount == -1) // DESC :: -1 이면 멀티뷰를 무제한 열수있음
                    {
                        isPossibleOpen = true;
                    }
                    else
                    {
                        isPossibleOpen = container.Count < typeAttribute.ShowMaxCount;
                    }

                    if ((isPossibleOpen == true) && (open && !presenter.Opened))
                    {
                        container.Add(id, presenter);
                        presenter.Open(null,
                            onClose: () => { Debug.Log("Close!!!!!"); container.Remove(id); }
                            , null, null);
                    }
                    else
                    {
                        presenter.Close();
                    }
                    return presenter as T;
                }
                else
                {
                    Debug.LogError($"GetMultiUI_Async - Failed to get presenter for {typeof(T).Name}");
                }
            }
            return default(T);
        }

        private async UniTask<T> AddMultiUI_Async<T>() where T : class, IUIPresenter, new()
        {
            var container = GetContainerForType<T>();
            if (container != null)
            {
                var presenter = await CreateMultiPresenter_Async<T>();
                if (presenter != null)
                {
                    var id = Guid.NewGuid().ToString();
                    container.Add(id, presenter);
                    return presenter;
                }
                else
                {
                    Debug.LogError($"AddMultiUI_Async - Failed to create presenter for {typeof(T).Name}");
                }
            }
            return null;
        }

        private async UniTask<T> CreateMultiPresenter_Async<T>() where T : class, IUIPresenter, new()
        {
            var presenterType = typeof(T);

            var presenter = Activator.CreateInstance(presenterType) as IUIPresenter;

            var viewComponent = default(IUIView);
            var uiView = default(UIView);
            if (presenter.View != default)
            {
                viewComponent = presenter.View;
                uiView = viewComponent as UIView;
            }
            else
            {
                var viewType = presenter.ViewType;
                var attribute = presenter.UiViewAttribute;
                var viewPrefabName = attribute.ViewResourceKey;
                viewComponent = await LoadView_Async(viewPrefabName, attribute.InsInstantiate);
                if (viewComponent == null)
                {
                    Debug.LogError($"Open_Async - UIView--{viewPrefabName}-- is null");
                    return null;
                }
                uiView = viewComponent as UIView;
                SetCanvas(attribute, uiView);
                if (viewComponent != null)
                {
                    presenter.BindView(viewComponent);
                }
            }
            return presenter as T;
        }

        public void ReturnPool(Type T, IUIPresenter returnPresenter)
        {

        }

        public void RemoveMultiView<T>(T uIPresenter) where T : class, IUIPresenter, new()
        {
            var type = typeof(T);

            var container = GetContainerForType<T>();
            if (!_MultiUIContainer.TryGetValue(type, out var baseContainer))
            {
                var multiContainer = container as MultiUIContainer<T>;
                if (multiContainer != null)
                {
                    multiContainer.Remove(uIPresenter.Id);
                }
                else
                {
                    Debug.LogError($"RemoveMultiView - MultiUIContainer for {type.Name} is not of type MultiUIContainer<{typeof(T).Name}>");
                }
            }
            else
            {
                Debug.LogWarning($"RemoveMultiView - MultiUIContainer for {type.Name} does not exist");
            }
        }

        public void ActiveCurrentMenuController()
        {

        }

        #endregion MultiUI
        public async UniTask<T> Open_Async<T>(RectTransform parentTransform, Action<IUIPresenter> openAction = null, Action closeAction = null, IUIData uIData = null, bool currentPresenterClose = true) where T : class, IUIPresenter, new()
        {
            Debug.Log($"Open_Async with parent : {typeof(T).Name}");

            if (_currentPresenter != null && currentPresenterClose)
            {
                _currentPresenter.Close();
                if (_currentPresenter.IsClosed)
                {
                    _currentPresenter = null;
                }
            }

            if (FindByPresenterType(typeof(T)) is var entity && entity != null &&
                entity.presenter is var presenter != default)
            {
                if (entity.IsComplete == false && entity.IsLoading)
                {
                    await UniTask.WaitUntil(() => entity.IsComplete);
                    return entity.presenter as T;
                }

                var viewComponent = default(IUIView);
                var uiView = default(UIView);
                if (presenter.View != default)
                {
                    viewComponent = presenter.View;
                    uiView = viewComponent as UIView;

                    uiView.gameObject.SetActive(false); // DESC :: 부모가 설정되기 전에 비활성화 상태로 설정
                    // DESC :: 기존 View가 있는 경우 부모 재설정
                    if (uiView != null && parentTransform != null)
                    {
                        uiView.transform.SetParent(parentTransform, false);
                        uiView.transform.ExNormalize();
                    }
                }
                else
                {
                    entity.IsLoading = true;
                    var viewType = entity.ViewType;
                    var attribute = presenter.UiViewAttribute;
                    var viewPrefabName = attribute.ViewResourceKey;

                    // DESC :: View 로드 전에 미리 비활성화 상태로 생성 준비
                    viewComponent = await LoadView_Async(viewPrefabName, attribute.InsInstantiate);
                    if (viewComponent == null)
                    {
                        Debug.LogError($"Open_Async - UIView--{viewPrefabName}-- is null");
                        return null;
                    }

                    uiView = viewComponent as UIView;

                    // DESC :: 깜빡임 방지를 위해 View를 먼저 비활성화
                    if (uiView != null)
                    {
                        uiView.gameObject.SetActive(false);
                    }

                    // DESC :: 부모 Transform 설정 (Canvas 설정보다 우선)
                    if (parentTransform != null && uiView != null)
                    {
                        uiView.transform.SetParent(parentTransform, false);
                        uiView.transform.ExNormalize();
                    }
                    else
                    {
                        SetCanvas(attribute, uiView);
                    }

                    if (viewComponent != null)
                    {
                        presenter.BindView(viewComponent);
                        entity.presenter = presenter;
                        entity.ViewType = viewType;
                    }

                    // DESC :: 레이아웃 계산 완료를 위해 한 프레임 대기
                    await UniTask.NextFrame();

                    // DESC :: Canvas 레이아웃 강제 갱신
                    if (uiView != null)
                    {
                        var canvas = uiView.GetComponentInParent<Canvas>();
                        if (canvas != null)
                        {
                            Canvas.ForceUpdateCanvases();
                        }
                        uiView.transform.GetComponent<RectTransform>()?.ForceUpdateRectTransforms();
                    }
                }

                await UniTask.WaitUntil(() => presenter.Loaded);
                entity.IsLoading = false;

                // DESC :: 모든 준비가 완료된 후 Open 호출 (이때 SetActive(true) 실행)
                presenter.Open(openAction, closeAction, uIData);
                entity.IsComplete = true;
                return presenter as T;
            }
            else
            {
                var presenterType = typeof(T);
                GenerateUIPresenter(presenterType);
                var result = await Open_Async<T>(parentTransform, openAction, closeAction, uIData);
                return result;
            }
        }

        public static void ContentCanvasOn(bool isOn)
        {
            if (isOn)
            {
                Instance.EnableUICanvas(UICanvasType.Content);
            }
            else
            {
                Instance.DisableUICanvas(UICanvasType.Content);
            }
        }

        public static Canvas GetCanvs(UICanvasType type)
        {
            return Instance._uiRoot.GetCanvas(type);
        }

        public static bool IsContentCanvasActive()
        {
            var targetCanvas = Instance._uiRoot.GetCanvas(UICanvasType.Content);
            if (targetCanvas != null)
            {
                return targetCanvas.gameObject.activeSelf;
            }
            return false;
        }
    }
}
