using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace SG.UI
{
    public class UIColorGuide : UIBehaviour
    {
        
        [Serializable]
        public class UIColorGuideData
        {
            public string key;
            public Color value;
        }

        [FormerlySerializedAs( "Lock Color" )]
        [SerializeField]
        private List<UIColorGuideData> colorList = new List<UIColorGuideData>();

        [SerializeField]
        private Image targetImage;
        [SerializeField]
        private Text targetText;
        [SerializeField]
        private TextMeshProUGUI targetTextMeshPro;

        protected override void Awake()
        {
            base.Awake();
            targetImage = GetComponent<Image>();
            targetText = GetComponent<Text>();
            targetTextMeshPro = GetComponent<TextMeshProUGUI>();
        }

        public void ChangeColor( string key, bool isWithChild = false )
        {
            var colorData = colorList.Find( x => x.key == key );
            if( colorData == null )
            {
                Debug.LogError( "Not Found Color Key : " + key );
                return;
            }
            ChangeColor( colorData.value, isWithChild );
        }

        public void ChangeColor( Color colorValue, bool isWithChild = false )
        {
            var color = colorValue;

            if( targetImage != null )
                targetImage.color = color;
            if( targetText != null )
                targetText.color = color;
            if( targetTextMeshPro != null )
                targetTextMeshPro.color = color;

            if( isWithChild )
            {
                var childUIColorGuide = GetComponentsInChildren<UIColorGuide>();
                foreach( var child in childUIColorGuide )
                {
                    child.ChangeColor( color, isWithChild );
                }
            }
        }

        public void ChangeColorEnable()
        {
           ChangeColor( "Enable" );
        }
        public void ChangeColorDisable()
        {
            ChangeColor( "Disable" );
        }
        public void ChangeColorLock()
        {
            ChangeColor( "Lock" );
        }
    }
}