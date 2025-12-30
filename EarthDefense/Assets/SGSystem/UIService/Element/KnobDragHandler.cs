using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace SG.UI 
{
    [RequireComponent( typeof( RectTransform ) )]
    public class KnobDragHandler : MonoBehaviour,
                                    IPointerDownHandler,
                                    IDragHandler,
                                    IEndDragHandler,          // �� �巡�� ���� ����
                                    IPointerEnterHandler,
                                    IPointerExitHandler
    {
        public UnityEvent<float> OnAngleChanged;     // 0 ~ 360��
        public UnityEvent OnPointerEnterEvent;
        public UnityEvent OnPointerExitEvent;

        [SerializeField] private float sensitivity = 0.3f;

        float _initialMouseY;
        float _initialAngle;
        float _currentAngle;

        bool _isDragging;    // �巡�� �� �÷���
        bool _exitPending;   // �巡�� �� Exit ����

        public float Angle
        {
            get => _currentAngle;
            set
            {
                _currentAngle = Mathf.Clamp( value, 0f, 360f );
                OnAngleChanged?.Invoke( _currentAngle );
            }
        }

        public void SetCurrentAngle( float angle )
        {
            _currentAngle = angle;
        }

        /* �������������������������������������������������� Pointer & Drag �������������������������������������������������� */

        public void OnPointerDown( PointerEventData eventData )
        {
            _initialMouseY = eventData.position.y;
            _initialAngle = _currentAngle;

            _isDragging = true;
            _exitPending = false;     // �� �巡�װ� ���۵Ǹ� �ʱ�ȭ
        }

        public void OnDrag( PointerEventData eventData )
        {
            float deltaY = eventData.position.y - _initialMouseY;
            float angleDelta = deltaY * sensitivity;
            Angle = _initialAngle + angleDelta;
        }

        public void OnEndDrag( PointerEventData eventData )
        {
            _isDragging = false;

            // �巡�� �߿� Exit �� �߻��ߴٸ� ���⼭ �� �� ó��
            if( _exitPending )
            {
                _exitPending = false;
                HandlePointerExit();
            }
        }

        /* �������������������������������������������������� Hover �������������������������������������������������� */

        public void OnPointerEnter( PointerEventData eventData )
        {
            HandlePointerEnter();
            _exitPending = false;   // �ٽ� ������ Exit ���� �÷��� ����
        }

        public void OnPointerExit( PointerEventData eventData )
        {
            if( _isDragging )
            {
                // �巡�� ���̸� ��� ó������ �ʰ� ����
                _exitPending = true;
                return;
            }
            HandlePointerExit();
        }

        /* �������������������������������������������������� Helpers �������������������������������������������������� */

        void HandlePointerEnter() => OnPointerEnterEvent?.Invoke();
        void HandlePointerExit() => OnPointerExitEvent?.Invoke();
    }

}
