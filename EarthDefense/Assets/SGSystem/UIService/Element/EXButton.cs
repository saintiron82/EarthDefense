using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
namespace SG.UI
{
    public class EXButton : Button
    {
        private bool _isPointerDown = false;
        private bool _isPointerEnter = false;
        private float _isDownEventTimeSlice = 0.1f; // DESC :: PointerDown 이벤트가 연속으로 발생하지 않도록 하는 시간 간격
        private float _lastPointerDownTime = 0f;
        
        [SerializeField] private float _pressThreshold = 0.25f; // DESC :: Press 상태로 인식하기 위한 최소 시간
        private float _currentPressTime = 0f; // DESC :: 현재 Press 상태 지속 시간
        private bool _isPressTriggered = false; // DESC :: Press 이벤트가 이미 발생했는지 확인

        [SerializeField] UnityEvent _onPointerDown = new UnityEvent(); 
        public UnityEvent onDown
        {
            get { return _onPointerDown; }
            set { _onPointerDown = value; }
        }

        [SerializeField] UnityEvent _onPointerUp = new UnityEvent();
        public UnityEvent onUp
        {
            get { return _onPointerUp; }
            set { _onPointerUp = value; }
        }

        [SerializeField] UnityEvent _onPointerExit = new UnityEvent();
        public UnityEvent onExit
        {
            get { return _onPointerExit; }
            set { _onPointerExit = value; } 
        }

        [SerializeField] UnityEvent _onEnter = new UnityEvent();
        public UnityEvent onEnter
        {
            get { return _onEnter; }
            set { _onEnter = value; }
        }

        [SerializeField] UnityEvent _onPress = new UnityEvent();
        public UnityEvent onPress
        {
            get { return _onPress; }
            set { _onPress = value; }
        }
        
        [SerializeField] private Image _targetImage;
        public Image TargetImage
        {
            get
            {
                if( _targetImage == null )
                {
                    _targetImage = GetComponent<Image>();
                }
                return _targetImage;
            }
        }
        [SerializeField] private TextMeshProUGUI _targetText;
        public TextMeshProUGUI TargetText
        {
            get
            {
                if(_targetText == null )
                {
                    if(gameObject.TryGetComponent(out TextMeshProUGUI tmp))
                    {
                        _targetText = tmp;
                    }
                }
                return _targetText;
            }
        }
        [SerializeField] private UIColorGuide _uIColorGuide;
        public UIColorGuide UIColorGuide
        {
            get
            {
                if( _uIColorGuide == null )
                {
                    if(gameObject.TryGetComponent(out UIColorGuide uIColorGuide))
                    {
                        _uIColorGuide = uIColorGuide;
                    }
                }
                return _uIColorGuide;
            }
        }


        [SerializeField] private ImageSwitcher _imageSwitcher;

        public ImageSwitcher ImageSwitcher
        {
            get
            {
                if( _imageSwitcher == null )
                {
                    _imageSwitcher = GetComponent<ImageSwitcher>();
                    if( _imageSwitcher ==null )
                    {
                        _imageSwitcher = GetComponentInChildren<ImageSwitcher>( true );
                    }
                    if( _imageSwitcher != null )
                        _imageSwitcher.targetImage = TargetImage;
                }
                return _imageSwitcher;
            }
        }

        /// <summary>
        /// Press 상태로 인식하기 위한 최소 시간을 설정합니다.
        /// </summary>
        public float PressThreshold
        {
            get { return _pressThreshold; }
            set { _pressThreshold = value; }
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            // EXButton 컴포넌트가 추가될 때 기본 Transition을 None으로 설정
            transition = Transition.None;
        }
#endif

        public void ChangeColor( string key )
        {
            if( UIColorGuide == null )
            {
                Debug.LogError( "Not Found UIColorGuide" );
                return;
            }
            UIColorGuide.ChangeColor( key );
        }

        public void SwitchImage( string imageName )
        {
            ImageSwitcher.SwitchImage( imageName );
        }

        public void SetText( string buttonText )
        {
            if( TargetText == null )
            {
                return;
            }
            _targetText.SetText(buttonText);
        }

        [SerializeField]
        private LocalizeStringEvent _targetLocalizeString;
        public LocalizeStringEvent TargetLocalizeString
        {
            get
            {
                return _targetLocalizeString;
            }
        }

        public void SetLocalizedText( string localizedKey )
        {
            if( TargetText == null )
            {
                return;
            }

            if( _targetLocalizeString == null)
            {
                TryGetComponent(out LocalizeStringEvent _targetLocalizeString);
                if( _targetLocalizeString == null )
                {
                    _targetLocalizeString = gameObject.AddComponent<LocalizeStringEvent>();
                    _targetLocalizeString.SetTable(ConstrantStrings.Localization);
                }
            }

            _targetLocalizeString.SetEntry(localizedKey);
        }

        public override void OnPointerDown( PointerEventData eventData )
        {
            base.OnPointerDown( eventData );

            if(_isPointerDown == false )
            {
                if( onDown != null )
                {
                    onDown.Invoke();
                }

                _isPointerDown = true;
                _currentPressTime = 0f; // DESC :: Press 시간 초기화
                _isPressTriggered = false; // DESC :: Press 이벤트 트리거 상태 초기화
            }
        }

        public override void OnPointerUp( PointerEventData eventData )
        {
            base.OnPointerUp( eventData );

            if( _isPointerDown == true )
            {
                if( onUp != null )
                {
                    onUp.Invoke();
                }

                _lastPointerDownTime = 0;
                _isPointerDown = false;
                _currentPressTime = 0f; // DESC :: Press 시간 초기화
                _isPressTriggered = false; // DESC :: Press 이벤트 트리거 상태 초기화
            }
        }

        public override void OnPointerEnter( PointerEventData eventData )
        {
            base.OnPointerEnter( eventData );
            if( _isPointerEnter == false )
            {
                _lastPointerDownTime = 0;
                _isPointerEnter = true;
            }
        }

        public override void OnPointerExit( PointerEventData eventData )
        {
            base.OnPointerExit( eventData );

            if( _isPointerEnter == true )
            {
                if( onExit != null )
                {
                    onExit.Invoke();
                }
                _lastPointerDownTime = 0;
                _isPointerEnter = false;
                _isPointerDown = false;
                _currentPressTime = 0f; // DESC :: Press 시간 초기화
                _isPressTriggered = false; // DESC :: Press 이벤트 트리거 상태 초기화
            }
        }

        private void Update()
        {
            PointDownCheckEvent();
            PressCheckEvent(); // DESC :: Press 상태 체크 추가
        }

        private void PointDownCheckEvent()
        {
            if( _isPointerDown == true )
            {
                // DESC :: PointerDown 이벤트가 연속으로 발생하지 않도록 하는 시간 간격
                if( _lastPointerDownTime > _isDownEventTimeSlice )
                {
                    _lastPointerDownTime = 0;
                    if( onDown != null )
                    {
                        onDown.Invoke();
                    }
                }

                _lastPointerDownTime += Time.deltaTime;

                if( _isPointerEnter && !IsInteractable() ) // DESC :: 버튼이 비활성화 되었을 때 PointerExit 이벤트를 발생시킴
                {
                    _isPointerDown = false; // DESC :: PointerExit 이벤트가 발생하면 PointerDown 상태를 해제
                    _isPointerEnter = false;
                    _currentPressTime = 0f; // DESC :: Press 시간 초기화
                    _isPressTriggered = false; // DESC :: Press 이벤트 트리거 상태 초기화

                    if( onExit != null )
                    {
                        onExit.Invoke();
                    }
                }
            }
        }

        private void PressCheckEvent()
        {
            if( _isPointerDown == true && _isPointerEnter == true && IsInteractable() )
            {
                _currentPressTime += Time.deltaTime;

                // DESC :: Press 임계값에 도달했고 아직 Press 이벤트가 발생하지 않았을 때
                if( _currentPressTime >= _pressThreshold && !_isPressTriggered )
                {
                    _isPressTriggered = true;
                    if( _onPress != null )
                    {
                        _onPress.Invoke();
                        _currentPressTime = 0f; // DESC :: Press 이벤트 발생 후 시간 초기화
                        _isPressTriggered = false; // DESC :: Press 이벤트 트리거 상태 초기화
                    }
                }
            }
        }
    }
}
