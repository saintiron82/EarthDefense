using SG.UI;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SG.UI
{

    public class IconButton : EXButton
    {
        public Image Icon;
        public TextMeshProUGUI Text;
        public void SetData( Sprite icon, string text = "" )
        {
            if( Icon != null )
                Icon.sprite = icon;
            if( Text != null )
                Text.text = text;
        }
    }
}