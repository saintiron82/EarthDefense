using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SG.UI
{
    public class HoverEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public UnityEvent OnPointEnterEvent;
        public UnityEvent OnPointExitEvent;
        public void OnPointerEnter( PointerEventData eventData )
        {
            if( OnPointEnterEvent != null )
                OnPointEnterEvent.Invoke(  );
            // ���̶���Ʈ �� ���� �߰�
        }

        public void OnPointerExit( PointerEventData eventData )
        {
            if( OnPointExitEvent != null )
                OnPointExitEvent.Invoke(  );

            EventSystem.current.SetSelectedGameObject( null );
            // ���̶���Ʈ ���� �� ���� �߰�
        }
    }
}
