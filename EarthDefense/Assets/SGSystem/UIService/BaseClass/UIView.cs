using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace SG.UI
{
    public class UIView : MonoBehaviour, IUIView
    {
        [Header("Common")]
        public Button m_close;
        public Transform m_MainRoot;
        public Transform m_SpaceUIParent;

        public RectTransform MoveRootRectTransform;
        public Vector2 MenuOffSet;


        protected Canvas _canvas;
        public Canvas Canvas => _canvas;

        protected Dictionary<int, UnityEvent> m_ButtonEventContainer = new Dictionary<int, UnityEvent>();
        protected Dictionary<int, UISlideEvent> m_SlideEventContainer = new Dictionary<int, UISlideEvent>();
        protected Dictionary<int, UIToggleEvent> m_ToggleEventContainer = new Dictionary<int, UIToggleEvent>();
        private UnityEvent<bool> _onApplicationFocusEvent = new UnityEvent<bool>();
        public UnityEvent<bool> OnApplicationFocusEvent
        {
            get { return _onApplicationFocusEvent; }
            
        }

        private bool _isPassAssertCheck = false;
        private bool _isBuild = false;
        public bool IsBuild => _isBuild;
        public GameObject GameObject => gameObject;

        [FormerlySerializedAs("isFallowTargetTransform")]
        public bool isFollowTargetTransform = false;
        private Transform _targetTransform;

        private Vector3 _targetPoint;

        public bool IsPassAssertCheck()
        {
            return _isPassAssertCheck;
        }
        protected RectTransform _rectTransform;
        public RectTransform ViewRectTransform
        {
            get { return _rectTransform; }
        }

        protected virtual void Awake()
        {
            Build();
        }

        protected virtual void OnDisable()
        {

        }

        protected void LateUpdate()
        {
            if( isFollowTargetTransform )
            {
                UpdateInfoViewPosition();
            }
        }

        public void SetCanvas( Canvas canvas )
        {
            _canvas = canvas;
        }

        public void SetTargetTransform( Transform targetTransform )
        {
            _targetTransform = targetTransform;
        }

        public virtual void Build()
        {
            _isBuild = true;
            _rectTransform = GetComponent<RectTransform>();
            Assert();
        }

        public Button CloseButton
        {
            get { return m_close; }
            private set { }
        }

        public virtual bool Assert()
        {
            _isPassAssertCheck = false;
            //검사 코드는 이 아래에 
            //문제가 있으면 _isPassAssertCheck == false
            _isPassAssertCheck = true;
            return true;
        }

        protected virtual void OnApplicationFocus(bool focus)
        {
            _onApplicationFocusEvent?.Invoke( focus );
        }

        public virtual void FitToParent( RectTransform rectTransform )
        {
            transform.ExSetParentWithFullStratch( rectTransform );
        }

        public virtual void Reset() { }

        public void AddEvent(Component eventObject, UnityAction @event)
        {
            AddCompositeButtonEvent(eventObject, @event);
        }

        public void AddEvent(Component eventObject, UnityAction<float> @event)
        {
            AddCompositeSlideEvent(eventObject, @event);
        }

        public void AddEvent(Component eventObject, UnityAction<bool> @event)
        {
            AddCompositeToggleEvent(eventObject, @event);
        }

        public void AddCompositeButtonEvent(Component eventObject, UnityAction @event)
        {
            if( eventObject == null ) return;

            var objectInstanceID = eventObject.GetInstanceID();
            if( !m_ButtonEventContainer.TryGetValue( objectInstanceID, out var buttonEvent ) )
            {
                buttonEvent = new UnityEvent();
                m_ButtonEventContainer[objectInstanceID] = buttonEvent;
            }
            buttonEvent.AddListener( @event );
        }

        public void AddCompositeSlideEvent(Component eventObject, UnityAction<float> @event)
        {
            if( eventObject == null )
                return;

            if (m_SlideEventContainer == null)
            {
                m_SlideEventContainer = new();
            }
            var objectInstanceID = eventObject.GetInstanceID();
            if (m_SlideEventContainer.ContainsKey(objectInstanceID) == false)
            {
                var newEvent = new UISlideEvent();
                newEvent.AddListener(@event);
                m_SlideEventContainer.Add(objectInstanceID, newEvent);
            }
            else
            {
                m_SlideEventContainer[objectInstanceID].AddListener(@event);
            }
        }

        public void AddCompositeToggleEvent(Component eventObject, UnityAction<bool> @event)
        {
            if( eventObject == null || m_ToggleEventContainer == null )
                return;

            var objectInstanceID = eventObject.GetInstanceID();
            if (m_ToggleEventContainer.ContainsKey(objectInstanceID) == false)
            {
                var newEvent = new UIToggleEvent();
                newEvent.AddListener(@event);
                m_ToggleEventContainer.Add(objectInstanceID, newEvent);
            }
            else
            {
                m_ToggleEventContainer[objectInstanceID].AddListener(@event);
            }
        }

        protected void ReceiveButtonEvent(Component eventObject)
        {
            if( eventObject == null || m_ButtonEventContainer == null)
                return;
            var objectInstanceID = eventObject.GetInstanceID();
            if (m_ButtonEventContainer.ContainsKey(objectInstanceID) != false)
            {
                m_ButtonEventContainer[objectInstanceID]?.Invoke();
            }
        }

        protected void ReceiveSlideEvent(Component eventObject, float value)
        {
            if( eventObject == null || m_SlideEventContainer == null )
                return;
            var objectInstanceID = eventObject.GetInstanceID();
            if (m_SlideEventContainer.ContainsKey(objectInstanceID) != false)
            {
                m_SlideEventContainer[objectInstanceID]?.Invoke(value);
            }
        }

        protected void ReceiveToggleEvent(Component eventObject, bool value)
        {
            if( eventObject == null || m_ToggleEventContainer == null )
                return;
            var objectInstanceID = eventObject.GetInstanceID();
            if (m_ToggleEventContainer.ContainsKey(objectInstanceID) != false)
            {
                m_ToggleEventContainer[objectInstanceID]?.Invoke(value);
            }
        }

        private void UpdateInfoViewPosition()
        {
            //현재 이 UI를 targetTransform 위치로 옮긴다.
            if( _canvas == null )
            {
                return;
            }
            RectTransform canvasRect = _canvas.transform as RectTransform;
            if( canvasRect == null )
            {
                return;
            }
            Camera cam = null;
            if( _canvas.renderMode == RenderMode.ScreenSpaceCamera || _canvas.renderMode == RenderMode.WorldSpace )
            {
                cam = _canvas.worldCamera != null ? _canvas.worldCamera : Camera.main;
            }

            Vector3 worldPos = _targetPoint;
            if( _targetTransform )
            {
                worldPos = _targetTransform.position;
            }
            if( _canvas.renderMode == RenderMode.WorldSpace )
            {
                MoveRootRectTransform.position = worldPos;
                return;
            }
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint( cam, worldPos );
            Vector2 localPoint;
            bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                cam,
                out localPoint
            );
            if( !ok )
            {
                return;
            }
            if( MoveRootRectTransform == null )
            {
                return;
            }
            MoveRootRectTransform.anchoredPosition = localPoint;
        }

        #region UIEvent
        public void OnClickClose()
        {
            ReceiveButtonEvent(m_close);
        }

        public void SetTargetPoint( Vector3 pos )
        {
            _targetPoint = pos;
        }
        #endregion
    }

}
