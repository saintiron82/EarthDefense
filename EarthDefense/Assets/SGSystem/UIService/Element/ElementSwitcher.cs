using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.UI
{
    public class ElementSwitcher : MonoBehaviour
    {
        private string _currenctKey = string.Empty;
        public string CurrentKey
        {
            get { return _currenctKey; }
        }

        [Serializable]
        public class SwichElementData
        {
            public string key;
            public GameObject objectNode;
        }
        [SerializeField] private List<SwichElementData> _switchElements;

        public void ElementSwitch( string key )
        {
            if( _switchElements == null || _switchElements.Count == 0 )
            {
                return;
            }

            foreach( var switchElement in _switchElements ) {
                if( switchElement == null )
                {
                    continue;
                }
                if( switchElement.objectNode == null )
                {
                    continue;
                }
                if( switchElement.key == key )
                {
                    _currenctKey = key;
                    switchElement.objectNode.SetActive( true );
                }
                else
                {
                    switchElement.objectNode.SetActive( false );
                }
            }
        }
    }
}

