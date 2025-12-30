using SG.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SG
{
    public enum UICanvasSelectType
    {
        None,
        Custom,
        Value,
    }

    [Serializable]
    public class UILoadingInfo
    {
        public string keyName;
        public bool isAutoLoad = false;
        public UICanvasSelectType uICanvasSelectType = UICanvasSelectType.Custom;
        public UICanvasType uICanvasType = UICanvasType.None;
    }

    [Serializable]
    public class UILoadingInfoAssetData : UILoadingInfo
    {
        public string assetPath;
    }

    [Serializable]
    public class UILoadingInfoPrefabData : UILoadingInfo
    {
        public GameObject prefab;
    }
}
