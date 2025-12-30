using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SG.UI
{
    public class UICanvas 
    {
        public UICanvasType Type;
        public Canvas Canvas;
        public Transform Root;
    }
    public class UIRoot : MonoBehaviour
    {
        public Canvas StaticUI;
        public Canvas ContentUI;
        public Canvas ContentPopupUI;
        public Canvas HudCanvas;
        public Canvas MainMenu;

        public Canvas InputBlockCanvas;


        public Dictionary<UICanvasType, UICanvas> UIRootCanvasList = new Dictionary<UICanvasType, UICanvas>();

        private void Awake()
        {
            CreateRoot();
        }

        private void CreateRoot()
        {
            GenerateRoot( UICanvasType.Content, ContentUI );
            GenerateRoot( UICanvasType.ContentPopup, ContentPopupUI );
            GenerateRoot( UICanvasType.Hud, HudCanvas );
            GenerateRoot( UICanvasType.MainMenu, MainMenu );
            GenerateRoot( UICanvasType.Static, StaticUI );
        }

        private UICanvas GenerateRoot( UICanvasType type, Canvas canvas)
        {
            if( canvas == null )
                return null;

            Transform rootObject = default;


            rootObject = canvas.transform.Find( "_Root" );
            if( rootObject == null )
            {
                rootObject = new GameObject( $"{type.ToString()}_Root" ).transform;
                rootObject.transform.ExSetParentWithFullStratch( canvas.transform );
            }

            
            var newUICanvas = new UICanvas()
            {
                Type = type,
                Canvas = canvas,
                Root = rootObject.transform,
            };
            UpdateScreenResolution( canvas );
            UIRootCanvasList.Add(type, newUICanvas);
            return newUICanvas;
        }

        private void UpdateScreenResolution( Canvas canvas )
        {
            if( canvas == null )
                return;

            var scaler = canvas.GetComponent<CanvasScaler>();
        }

        public void InputBlockCanvasToggle(bool active )
        {
            if( InputBlockCanvas != null )
            {
                InputBlockCanvas.gameObject.SetActive( active );
            }
        }

        public void SetCamera(UICanvasType uICanvasType, Camera camera )
        {
            var target = GetCanvas(uICanvasType);
            if( target != null)
            {
                target.worldCamera = camera;
            }
        }

        public Canvas GetCanvas(UICanvasType type)
        {
            if (UIRootCanvasList.ContainsKey(type))
                return UIRootCanvasList[type].Canvas;
            return null;
        }

        public Transform GetRoot( UICanvasType type )
        {
            if (UIRootCanvasList.ContainsKey(type))
            {
                return UIRootCanvasList[type].Root;
            }
            else
            {
                return null;
            }
        }


        public void SetActiveTargetCanvas( UICanvasType targetType, bool active)
        {
            var targetCanvas = GetCanvas(targetType);
            if( targetCanvas != null)
            {
                targetCanvas.gameObject.SetActive(active);
            }
        }
    }
}
