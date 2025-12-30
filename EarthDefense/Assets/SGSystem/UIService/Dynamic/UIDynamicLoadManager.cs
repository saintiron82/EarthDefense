using Cysharp.Threading.Tasks;
using SG.UI;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    public class UIDynamicLoadManager : MonoBehaviour
    {

        [Header( "[UI Loading Asset]" )]
        [SerializeField] private UILoadingInfoAsset m_UILoadingAsset = null;
        public UILoadingInfoAsset UILoadingAsset { get { return m_UILoadingAsset; } }

        public class LoadingInfo
        {
            public string keyName;
            public Transform parentTransform;
            public GameObject loadObject;
            public bool isNowLoading;
            public bool isComplete;
            public bool isLoadingFail;
            public UICanvasSelectType uICanvasSelectType = UICanvasSelectType.Custom;
            public UICanvasType uICanvasType = UICanvasType.None;
        }

        private Dictionary<string, LoadingInfo> m_LodingInfos = new Dictionary<string, LoadingInfo>();
        public List<UILoadingInfoPrefabData> LoadingInfoPrefabDatas => m_UILoadingAsset.uILoadingInfoPrefabDatas;
        public List<UILoadingInfoAssetData> LoadingInfoAssetDatas => m_UILoadingAsset.uiLoadingInfoAssetDataList;

        public async UniTask<UIView> UIViewDynamicLoad(string viewName)
        {
            var result = await GetUINew_Async<UIView>(viewName, false);
            if( result.info != null )
            {
                if( result.info.uICanvasSelectType == UICanvasSelectType.Value )
                {
                    UIService.Instance.SetCanvas( result.info.uICanvasType, result.uiResult.gameObject );
                }
            }
            return result.uiResult;
        }

        public UILoadingInfoPrefabData GetStaticUIObject( string viewName )
        {
            var targetInfo = LoadingInfoPrefabDatas.Find( x => x.keyName == viewName );
            return targetInfo;
        }

        public async UniTask<(T uiResult, LoadingInfo info)> GetUINew_Async<T>( string viewName, bool isInstantiate = true ) where T : Component
        {
            var type = typeof( T );

            if( m_LodingInfos.ContainsKey( viewName ) == false )
            {
                var typeName = type.Name;
                var newLoadingInfo = new LoadingInfo()
                {
                    isComplete = false,
                    isNowLoading = true,
                    isLoadingFail = false,
                    loadObject = null,
                };
                m_LodingInfos.Add( viewName, newLoadingInfo );
                
                var assetData = LoadingInfoAssetDatas.Find( x => x.keyName == viewName );
                if( assetData == null )
                {
                    var targetInfo = LoadingInfoPrefabDatas.Find( x => x.keyName == viewName );
                    if( targetInfo == null )
                    {
                        Debug.LogWarning( " Not Find Loading Data --" + typeName );
                        newLoadingInfo.isLoadingFail = true;
                        newLoadingInfo.isNowLoading = false;
                        newLoadingInfo.isComplete = true;
                        newLoadingInfo.loadObject = null;
                        return (null, newLoadingInfo);
                    }
                    else
                    {
                        newLoadingInfo.loadObject = targetInfo.prefab;
                        newLoadingInfo.isLoadingFail = false;
                        newLoadingInfo.isNowLoading = false;
                        newLoadingInfo.isComplete = true;
                        newLoadingInfo.uICanvasSelectType = targetInfo.uICanvasSelectType;
                        newLoadingInfo.uICanvasType = targetInfo.uICanvasType;
                    }
                }
                
                if( isInstantiate )
                {
                    return (InstantiateLoadObject<T>( newLoadingInfo ),newLoadingInfo);
                }
                else
                {
                    return (newLoadingInfo.loadObject.GetComponent<T>(), newLoadingInfo);
                }
            }
            else
            {
                Debug.Log( "GetUI_Async2" + m_LodingInfos.Count );
                var loadingInfo = m_LodingInfos[viewName];
                await UniTask.WaitUntil( () => loadingInfo.isNowLoading == false );
                if( loadingInfo.isLoadingFail || loadingInfo.loadObject == null )
                    return (null, loadingInfo);

                var loadObject = loadingInfo.loadObject.GetComponent<T>();
                if( isInstantiate )
                {
                    return (InstantiateLoadObject<T>( loadingInfo ), loadingInfo);
                }
                else
                {
                    return (loadObject,loadingInfo);
                }
            }
        }

        public async UniTask<T> GetUI_Async<T>(bool isInstantiate = true) where T : Component
        {
            var type = typeof(T);
            var typeName = type.Name;
            var result = await GetUINew_Async<T>( typeName, isInstantiate );

            if( result.info != null )
            {
                if( result.info.uICanvasSelectType == UICanvasSelectType.Value )
                {
                    UIService.Instance.SetCanvas( result.info.uICanvasType, result.uiResult.gameObject );
                }
            }

            result.uiResult.gameObject.SetActive( true );
            return result.uiResult;
        }

        private T InstantiateLoadObject<T>( LoadingInfo loadingInfo) where T : Component
        {
            var useObject = GameObject.Instantiate( loadingInfo.loadObject, transform);
            useObject.gameObject.SetActive(false);

            if( loadingInfo.parentTransform != null )
            {
                useObject.transform.SetParent( loadingInfo.parentTransform );
                useObject.gameObject.SetLayerRecursive( loadingInfo.parentTransform.gameObject.layer );
            }
            var rectTransform = useObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector3.zero;
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.offsetMin = new Vector2(0, 0);
                rectTransform.offsetMax = new Vector2(0, 0);
            }
            useObject.transform.localPosition = Vector3.zero;
            useObject.transform.localScale = Vector3.one;
            

            return useObject.GetComponent<T>();
        }
    }
}
