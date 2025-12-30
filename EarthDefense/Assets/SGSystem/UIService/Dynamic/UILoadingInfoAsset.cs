using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    [CreateAssetMenu( fileName = "UILoadingInfoAsset", menuName = "UI/UI Resource Loading Asset" )]
    public class UILoadingInfoAsset : ScriptableObject
    {
        public List<UILoadingInfoAssetData> uiLoadingInfoAssetDataList = new List<UILoadingInfoAssetData>();
        public List<UILoadingInfoPrefabData> uILoadingInfoPrefabDatas = new List<UILoadingInfoPrefabData>();
    }
}
