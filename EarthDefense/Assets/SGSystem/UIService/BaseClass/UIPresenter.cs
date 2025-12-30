using Cat;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace SG.UI
{
    public enum OpendDetailStep
    {
        None,
        OpenRequest,
        LoadResource,
        Init,
        OpenEventStart,
        OpenEventEnd,
        Complete
    }

    public enum eDisplayHeightValueType
    {
        None,
        FixedSize,
        RatioSize
    }

    public class UIPresenter<T> : IUIPresenter, IDisposable where T : UIView
    {
        public string Id { get; private set; }
        public virtual bool Loaded { get; set; }
        public bool Opened { get; set; }

        protected PlayerLoopTiming _updateLoopTiming = PlayerLoopTiming.PreLateUpdate;

        public bool IsClosed 
        {
            get
            {
                return !Opened;
            }
        }

        public OpendDetailStep OpendDetailStep { get; set; } = OpendDetailStep.None;

        private T _View;
        public T View => _View;

        public object[] Parameters;

        public IUIData UIData { get; protected set; }
        IUIView IUIPresenter.View =>  View;

        public Type ViewType => typeof(T);

        protected bool _isUpdateLoopRunning = false;
        protected CancellationTokenSource _uniTaskUpdateCTS;

        public Action OnCloseAction;
        protected string _option;

        // DESC :: R3를 사용한 외부 클릭 감지를 위한 필드
        private IDisposable _outsideClickDetectionDisposable;
        // DESC :: 외부에서 마우스 다운이 시작되었는지 추적하는 필드
        private bool _isMouseDownStartedOutside;

        public string Option
        {
            get => _option;
        }

        protected UIViewAttribute _uiViewAttribute;
        public UIViewAttribute UiViewAttribute
        {
            get
            {
                if( _uiViewAttribute == null )
                {
                    _uiViewAttribute = typeof( T ).GetCustomAttribute<UIViewAttribute>();
                }
                return _uiViewAttribute;
            }
        }

        public string ViewPrefabName => GetViewPrefabName();
        public virtual void BindView( IUIView view )
        {
            if( view == null )
            {
                throw new ArgumentNullException( nameof( view ), "View is null" );
            }
            _View = view as T;
            if( _View == null )
            {
                throw new InvalidCastException( $"View is not of type {typeof( T )}" );
            }
            if( !_View.IsBuild )
                _View.Build();

            if ( UiViewAttribute.RootHideLoad )
            {
                if( _View.m_MainRoot != null )
                {
                    _View.m_MainRoot.gameObject.SetActive( false );
                }
            }
            if( UiViewAttribute.FollowTarget || UiViewAttribute.FallowTarget )
            {
                View.isFollowTargetTransform = true;
            }
            Init();
            Loaded = true;
        }

        protected void SetUpdateLoopTiming(PlayerLoopTiming timing)
        {
            _updateLoopTiming = timing;
        }

        protected void StartUpdateLoop(Action OnUpdateCallback)
        {
            if (_isUpdateLoopRunning)
            {
                return;
            }

            if (_uniTaskUpdateCTS != null)
            {
                _isUpdateLoopRunning = false;
                _uniTaskUpdateCTS.Cancel();
                _uniTaskUpdateCTS.Dispose();
                _uniTaskUpdateCTS = null;
            }

            // DESC :: View 파괴 시 자동으로 취소되는 토큰 획득
            var destroyToken = View.GetCancellationTokenOnDestroy();
            _uniTaskUpdateCTS = new CancellationTokenSource();
            _isUpdateLoopRunning = true;

            // DESC :: 두 토큰을 조합하여 하나라도 취소되면 UpdateLoop가 중단되도록 설정
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(destroyToken, _uniTaskUpdateCTS.Token).Token;

            UpdateLoopAsync(combinedToken, OnUpdateCallback, _updateLoopTiming).Forget();
        }

        private async UniTask UpdateLoopAsync(CancellationToken token, Action OnUpdateCallback, PlayerLoopTiming timing = PlayerLoopTiming.PreLateUpdate)
        {
            while (!token.IsCancellationRequested)
            {
                OnUpdateCallback?.Invoke(); 
                await UniTask.Yield(timing, token, true);
            }
        }

        protected void StopUpdateLoop()
        {
            if (_uniTaskUpdateCTS != null)
            {
                _uniTaskUpdateCTS.Cancel();
                _uniTaskUpdateCTS.Dispose();
                _uniTaskUpdateCTS = null;
            }

            _isUpdateLoopRunning = false;
        }


        // DESC :: 외부 클릭 감지 중지
        protected void StopOutsideClickDetection()
        {
            _outsideClickDetectionDisposable?.Dispose();
            _outsideClickDetectionDisposable = null;
            _isMouseDownStartedOutside = false;
        }

        // DESC :: 마우스 입력 처리 메서드
        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // DESC :: 마우스 다운이 외부에서 시작되었는지 확인
                _isMouseDownStartedOutside = IsClickOutsideMainRoot();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                // DESC :: 외부에서 다운이 시작되고 외부에서 업이 발생했을 때만 처리
                if (_isMouseDownStartedOutside && IsClickOutsideMainRoot())
                {
                    OnOutsideClick();
                }
                _isMouseDownStartedOutside = false;
            }
        }

        // DESC :: 클릭이 MainRoot 외부인지 확인하는 메서드
        private bool IsClickOutsideMainRoot()
        {
            // DESC :: EventSystem을 사용하여 클릭된 UI 요소 확인
            if (EventSystem.current == null)
            {
                return true;
            }

            var pointerEventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, results);

            bool clickedOnMainRoot = false;
            
            if (View != null && View.m_MainRoot != null)
            {
                if (results.Count > 0)
                {
                    // DESC :: 클릭된 UI 요소가 m_MainRoot 안에 있는지 확인
                    foreach (var result in results)
                    {
                        if (result.gameObject != null)
                        {
                            // DESC :: 클릭된 오브젝트가 m_MainRoot의 자식인지 확인
                            if ((result.gameObject.transform.IsChildOf(View.m_MainRoot)) ||
                                (result.gameObject.transform == View.m_MainRoot))
                            {
                                clickedOnMainRoot = true;
                                break;
                            }
                        }
                    }
                }
            }
            
            return false;
        }

        protected virtual void OnOutsideClick()
        {
            Close();
        }

        // DESC :: 상호작용 가능한 UI 요소인지 확인하는 메서드
        private bool IsInteractableUIElement(GameObject gameObject)
        {
            // DESC :: Button 컴포넌트 확인
            var button = gameObject.GetComponent<UnityEngine.UI.Button>();
            if (button != null && button.interactable)
            {
                return true;
            }
            
            // DESC :: Toggle 컴포넌트 확인
            var toggle = gameObject.GetComponent<UnityEngine.UI.Toggle>();
            if (toggle != null && toggle.interactable)
            {
                return true;
            }
            
            // DESC :: Slider 컴포넌트 확인
            var slider = gameObject.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null && slider.interactable)
            {
                return true;
            }
            
            // DESC :: Dropdown 컴포넌트 확인
            var dropdown = gameObject.GetComponent<UnityEngine.UI.Dropdown>();
            if (dropdown != null && dropdown.interactable)
            {
                return true;
            }
            
            // DESC :: InputField 컴포넌트 확인
            var inputField = gameObject.GetComponent<UnityEngine.UI.InputField>();
            if (inputField != null && inputField.interactable)
            {
                return true;
            }
            
            // DESC :: Scrollbar 컴포넌트 확인
            var scrollbar = gameObject.GetComponent<UnityEngine.UI.Scrollbar>();
            if (scrollbar != null && scrollbar.interactable)
            {
                return true;
            }

            return false;
        }

        protected virtual void OnSaveDraggableRect(RectTransform arg0)
        {
            
        }

        private UIPositionData ConvertRectPositionToJson(RectTransform rectTransform)
        {
            // DESC :: Canvas를 통해 화면 크기 기준 계산
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("Canvas not found for RectTransform position calculation");
                return null;
            }

            RectTransform canvasRectTransform = canvas.transform as RectTransform;
            if (canvasRectTransform == null)
            {
                Debug.LogWarning("Canvas RectTransform not found");
                return null;
            }

            // DESC :: 캔버스 크기 가져오기
            Vector2 canvasSize = canvasRectTransform.sizeDelta;
            if (canvasSize.x == 0 || canvasSize.y == 0)
            {
                // DESC :: Canvas 크기가 0이면 화면 해상도 사용
                canvasSize = new Vector2(Screen.width, Screen.height);
            }

            // DESC :: RectTransform의 현재 위치 (앵커드 포지션 기준)
            Vector2 currentPosition = rectTransform.anchoredPosition;

            // DESC :: 화면 해상도 기준 비율 계산 (0~1 범위)
            float xRatio = (currentPosition.x + canvasSize.x * 0.5f) / canvasSize.x;
            float yRatio = (currentPosition.y + canvasSize.y * 0.5f) / canvasSize.y;

            // DESC :: 비율을 0~1 범위로 클램핑
            xRatio = Mathf.Clamp01(xRatio);
            yRatio = Mathf.Clamp01(yRatio);

            // DESC :: JSON 직렬화를 위한 데이터 객체 생성
            UIPositionData positionData = new UIPositionData
            {
                uiPresenterName = this.GetType().Name,
                xRatio = xRatio,
                yRatio = yRatio,
                absolutePosition = currentPosition,
                canvasSize = canvasSize,
                screenResolution = new Vector2(Screen.width, Screen.height),
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };


            return positionData;
        }

        protected bool RestoreRectPosition(UIPositionData positionData, RectTransform rectTransform)
        {
            if(View.MoveRootRectTransform == null)
            {
                Debug.LogWarning("Root RectTransform is null");
                return false;
            }

            if (positionData == null)
            {
                Debug.LogWarning("Failed to deserialize UI position data");
                return false;
            }

            // DESC :: Canvas 크기 가져오기
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return false;
            }

            RectTransform canvasRectTransform = canvas.transform as RectTransform;
            Vector2 currentCanvasSize = canvasRectTransform?.sizeDelta ?? new Vector2(Screen.width, Screen.height);
            
            if (currentCanvasSize.x == 0 || currentCanvasSize.y == 0)
            {
                currentCanvasSize = new Vector2(Screen.width, Screen.height);
            }

            // DESC :: 저장된 비율로부터 현재 해상도에 맞는 위치 계산
            float xPosition = (positionData.xRatio * currentCanvasSize.x) - (currentCanvasSize.x * 0.5f);
            float yPosition = (positionData.yRatio * currentCanvasSize.y) - (currentCanvasSize.y * 0.5f);

            // DESC :: 위치 적용
            rectTransform.anchoredPosition = new Vector2(xPosition, yPosition);
            
            Debug.Log($"Restored UI position from JSON: {positionData.uiPresenterName} -> ({xPosition:F2}, {yPosition:F2})");
            return true;
        }

        public void CreateAttribute<V>() where V : UIView
        {
            _uiViewAttribute = typeof(V).GetCustomAttribute<UIViewAttribute>();
        }

        public virtual string GetViewPrefabName()
        {
            return UiViewAttribute.ViewResourceKey;
        }

        // Start is called before the first frame update
        public virtual void Init()
        {
            Debug.Log( "Init UI::" + View.name );
            if( Loaded == false )
                AddStaticEvent();
        }

        private void UpdateViewPositionToFollowWorldObject(Transform worldTarget)
        {
            // DESC :: 메인 카메라 가져오기
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("Main camera not found for world to UI conversion");
                return;
            }

            // DESC :: Canvas 컴포넌트 찾기
            Canvas canvas = View.MoveRootRectTransform.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("Canvas not found for UI position conversion");
                return;
            }

            // DESC :: 3D 월드 좌표를 스크린 좌표로 변환
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldTarget.position);

            // DESC :: 오브젝트가 카메라 뒤에 있는지 확인
            if (screenPosition.z < 0)
            {
                // DESC :: 오브젝트가 카메라 뒤에 있으면 UI를 숨김
                View.gameObject.SetActive(false);
                return;
            }

            // DESC :: UI가 숨겨져 있었다면 다시 보이게 함
            if (!View.gameObject.activeInHierarchy)
            {
                View.gameObject.SetActive(true);
            }

            // DESC :: Canvas 타입에 따른 좌표 변환
            Vector2 canvasPosition;
            RectTransform canvasRectTransform = canvas.transform as RectTransform;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // DESC :: Screen Space - Overlay 모드
                canvasPosition = screenPosition;
            }
            else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // DESC :: Screen Space - Camera 모드
                if (canvas.worldCamera != null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRectTransform, 
                        screenPosition, 
                        canvas.worldCamera, 
                        out canvasPosition);
                }
                else
                {
                    canvasPosition = screenPosition;
                }
            }
            else
            {
                // DESC :: World Space 모드
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform, 
                    screenPosition, 
                    mainCamera, 
                    out canvasPosition);
            }

            // DESC :: MenuOffSet이 있는 경우 추가 오프셋 적용
            if (View.MenuOffSet != Vector2.zero)
            {
                canvasPosition += View.MenuOffSet;
            }

            // DESC :: View의 RectTransform 위치 업데이트
            View.MoveRootRectTransform.anchoredPosition = canvasPosition;
        }

        public virtual void Open(Action<IUIPresenter> onOpen, Action oncloseAction, IUIData data = null, params object[] parameters )
        {
            Parameters = parameters;
            UIData = data;
            OnCloseAction = oncloseAction;

            Open(oncloseAction);
            onOpen?.Invoke(this);
        }

        public virtual void Open(Action oncloseAction)
        {
            OnCloseAction = oncloseAction;
            Open();
        }

        public virtual void Open()
        {
            Debug.Log( "Open UI::" + View.name );
            Opened = true;
            if (View is MonoBehaviour viewComponent)
            {
                viewComponent.gameObject.SetActive(true);
                if( UiViewAttribute.IsAutoTop )
                {
                    //하이어라키 상 항상 Top에 위치 시키기
                    viewComponent.transform.SetAsLastSibling();
                }
            }
            if( UiViewAttribute  != null )
            {
                if( UiViewAttribute.CanvasType == UICanvasType.ContentPopup )
                {
                    UIService.Instance.AddEnabledContentPopupUI( UICanvasType.ContentPopup, View.gameObject );
                }
                if( UiViewAttribute.EscapeEnable )
                {
                    UIService.Instance.PushUIStack( this );
                }
            }
            RunOpenEvent();
        }

        public virtual void SetOption(string option)
        {
            _option = option;
        }

        protected virtual void OnUIEvent(UnityEvent uiEvent )
        {
            
        }

        //생성레벨에서 등록해서 쓰는 이벤트 
        public virtual void AddStaticEvent()
        {
            //이지선다 두가지 방법을 제공하고 있다. 
            /*
            if (View.m_close != null)
                View.AddEvent(View.m_close, Close);
            */

            if( View.m_close != null )
            {
                View.m_close.onClick.AddListener( () =>
                {
                    OnClickCloseButton();
                } );
            }

            if( UiViewAttribute.FocusShowHide )
            {
                View.OnApplicationFocusEvent.AddListener( ( bool isOn ) =>
                {
                    if( isOn == false )
                    {
                        Close();
                    }
                } );
            }    
        }

        protected virtual void OnClickCloseButton()
        {
            Close();
        }

        //오픈할때 등록해서 쓰는 이벤트 
        public virtual void RunOpenEvent()
        {
            /*
            if( UiViewAttribute.GuideType != eGuideType.None )
            {
                if( UIService.Instance.UIGuideManager.GetGuideCompleteInfo( UiViewAttribute.GuideType ) == false )
                {
                    PlayGuide();
                }
            }*/
            PlayOpenEvent_Async().Forget();
        }

        public virtual async UniTaskVoid PlayOpenEvent_Async()
        {
            await UniTask.CompletedTask;
        }

        public virtual void Close()
        {
            StopUpdateLoop();
            StopOutsideClickDetection(); // DESC :: 외부 클릭 및 드래그 감지 중지
            if( UiViewAttribute != null )
            {
                if( UiViewAttribute.CanvasType == UICanvasType.ContentPopup )
                {
                    UIService.Instance.DissableUI( UICanvasType.ContentPopup, View.gameObject );
                }
                if( UiViewAttribute.EscapeEnable )
                {
                    UIService.Instance.CheckAndPop( this );
                }
                if(UiViewAttribute.IsMultiView )
                {
                    UIService.Instance.RemoveMultiView( this );
                }
            }

            Opened = false;
            OnCloseAction?.Invoke();
            OnCloseAction = null;

            // DESC :: UIService의 UIEntity 상태 업데이트
            UIService.Instance.UpdateUIEntityState(this.GetType(), false, false);

            // DESC :: Content 타입이고 현재 프레젠터가 이 프레젠터인 경우 null로 설정
            if (UiViewAttribute != null && UiViewAttribute.CanvasType == UICanvasType.Content)
            {
                UIService.Instance.ClearCurrentPresenter(this);
            }

            View.SetActive(false);
        }

        public virtual void SetShow(bool show)
        {
            View.gameObject.SetActive( show );
        }
        public virtual void Release()
        {
            Dispose();
            View.OnApplicationFocusEvent.RemoveAllListeners(); 
            GameObject.Destroy( View.gameObject );
        }
        public virtual void OnEnable()
        {
            
        }
        
        public virtual void OnDisable()
        {
            
        }

        public virtual void Dispose()
        {
            StopUpdateLoop();
            StopOutsideClickDetection(); // DESC :: Dispose 시 외부 클릭 감지 정리
            _isMouseDownStartedOutside = false; // DESC :: 마우스 다운 상태 초기화
        }
        
        protected virtual void DisposeCTS(CancellationTokenSource cts)
        {
            if (cts != null)
            {
                if (cts.IsCancellationRequested == false)
                {
                    cts.Cancel();
                }
                DisposeItem(cts);
            }
        }

        protected virtual void DisposeItem(IDisposable item)
        {
            if (item != null)
            {
                item.Dispose();
                item = null;
            }
        }

        public virtual void ChangeLanguage()
        {
            
        }

        public void SetFollowTarget( Transform target )
        {
            View.SetTargetTransform( target );
        }

        public void SetFollowPoint( Vector3 pos )
        {
            View.SetTargetPoint( pos );
        }
    }
}
