using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using SG;
using SG.UI;

[CustomEditor( typeof( UILoadingInfoAsset ) )]
public class UILoadingInfoAssetEditor : Editor
{
    private string searchQuery = ""; // 검색어 저장용 변수
    private List<UILoadingInfoAssetData> filteredAssetDataList = new List<UILoadingInfoAssetData>(); // Asset Data 검색 결과 리스트
    private List<UILoadingInfoPrefabData> filteredPrefabDataList = new List<UILoadingInfoPrefabData>(); // Prefab Data 검색 결과 리스트
    private UILoadingInfoAssetData selectedAssetData = null; // 선택된 Asset Data 저장용 변수
    private UILoadingInfoPrefabData selectedPrefabData = null; // 선택된 Prefab Data 저장용 변수

    public override void OnInspectorGUI()
    {
        UILoadingInfoAsset asset = (UILoadingInfoAsset)target;

        // 검색 필드
        EditorGUILayout.LabelField( "Search Data", EditorStyles.boldLabel );
        searchQuery = EditorGUILayout.TextField( "Search", searchQuery );

        // Asset Data 필터링
        if( !string.IsNullOrEmpty( searchQuery ) )
        {
            filteredAssetDataList = asset.uiLoadingInfoAssetDataList
                .Where( data => data.keyName.ToLower().Contains( searchQuery.ToLower() ) )
                .ToList();
        }
        else
        {
            filteredAssetDataList = asset.uiLoadingInfoAssetDataList;
        }

        // Prefab Data 필터링
        if( !string.IsNullOrEmpty( searchQuery ) )
        {
            filteredPrefabDataList = asset.uILoadingInfoPrefabDatas
                .Where( data => data.keyName.ToLower().Contains( searchQuery.ToLower() ) )
                .ToList();
        }
        else
        {
            filteredPrefabDataList = asset.uILoadingInfoPrefabDatas;
        }

        // Asset Data 리스트 표시 및 추가/삭제 기능
        EditorGUILayout.LabelField( "Asset Data Results", EditorStyles.boldLabel );
        foreach( var data in filteredAssetDataList )
        {
            EditorGUILayout.BeginHorizontal();

            // 선택된 데이터는 색상 강조
            GUI.backgroundColor = ( data == selectedAssetData ) ? Color.cyan : Color.white;

            if( GUILayout.Button( data.keyName ) )
            {
                selectedAssetData = data; // 사용자가 클릭한 데이터를 선택
                selectedPrefabData = null; // Prefab Data 선택 해제
            }

            // 삭제 버튼
            if( GUILayout.Button( "Delete", GUILayout.Width( 60 ) ) )
            {
                asset.uiLoadingInfoAssetDataList.Remove( data );
                selectedAssetData = null; // 삭제 후 선택 해제
                break; // 리스트가 변경되었으므로 루프를 종료
            }

            EditorGUILayout.EndHorizontal();
        }

        // Asset Data 추가 버튼
        if( GUILayout.Button( "Add New Asset Data" ) )
        {
            var newData = new UILoadingInfoAssetData { keyName = "New Asset Data" };
            asset.uiLoadingInfoAssetDataList.Add( newData );
            selectedAssetData = newData; // 새로 추가된 데이터를 선택
        }

        // Prefab Data 리스트 표시 및 추가/삭제 기능
        EditorGUILayout.LabelField( "Prefab Data Results", EditorStyles.boldLabel );
        foreach( var data in filteredPrefabDataList )
        {
            EditorGUILayout.BeginHorizontal();

            // 선택된 데이터는 색상 강조
            GUI.backgroundColor = ( data == selectedPrefabData ) ? Color.cyan : Color.white;

            if( GUILayout.Button( data.keyName ) )
            {
                selectedPrefabData = data; // 사용자가 클릭한 데이터를 선택
                selectedAssetData = null; // Asset Data 선택 해제
            }

            // 삭제 버튼
            if( GUILayout.Button( "Delete", GUILayout.Width( 60 ) ) )
            {
                asset.uILoadingInfoPrefabDatas.Remove( data );
                selectedPrefabData = null; // 삭제 후 선택 해제
                break; // 리스트가 변경되었으므로 루프를 종료
            }

            EditorGUILayout.EndHorizontal();
        }

        // Prefab Data 추가 버튼
        if( GUILayout.Button( "Add New Prefab Data" ) )
        {
            var newPrefabData = new UILoadingInfoPrefabData { keyName = "New Prefab Data" };
            asset.uILoadingInfoPrefabDatas.Add( newPrefabData );
            selectedPrefabData = newPrefabData; // 새로 추가된 데이터를 선택
        }

        // 선택된 Asset Data 표시 및 수정 가능하게 하기
        if( selectedAssetData != null )
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( "Selected Asset Data", EditorStyles.boldLabel );

            selectedAssetData.keyName = EditorGUILayout.TextField( "Key Name", selectedAssetData.keyName );
            selectedAssetData.isAutoLoad = EditorGUILayout.Toggle( "Is Auto Load", selectedAssetData.isAutoLoad );
            selectedAssetData.uICanvasSelectType = (UICanvasSelectType)EditorGUILayout.EnumPopup( "Canvas Select Type", selectedAssetData.uICanvasSelectType );
            selectedAssetData.uICanvasType = (UICanvasType)EditorGUILayout.EnumPopup( "Canvas Type", selectedAssetData.uICanvasType );
            selectedAssetData.assetPath = EditorGUILayout.TextField( "Asset Path", selectedAssetData.assetPath );

            EditorGUILayout.Space();
        }

        // 선택된 Prefab Data 표시 및 수정 가능하게 하기
        if( selectedPrefabData != null )
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( "Selected Prefab Data", EditorStyles.boldLabel );

            selectedPrefabData.keyName = EditorGUILayout.TextField( "Key Name", selectedPrefabData.keyName );
            selectedPrefabData.isAutoLoad = EditorGUILayout.Toggle( "Is Auto Load", selectedPrefabData.isAutoLoad );
            selectedPrefabData.uICanvasSelectType = (UICanvasSelectType)EditorGUILayout.EnumPopup( "Canvas Select Type", selectedPrefabData.uICanvasSelectType );
            selectedPrefabData.uICanvasType = (UICanvasType)EditorGUILayout.EnumPopup( "Canvas Type", selectedPrefabData.uICanvasType );
            selectedPrefabData.prefab = (GameObject)EditorGUILayout.ObjectField( "Prefab", selectedPrefabData.prefab, typeof( GameObject ), false );

            EditorGUILayout.Space();
        }

        // 변경 사항이 있는 경우 저장
        if( GUI.changed )
        {
            EditorUtility.SetDirty( asset );
        }
    }
}
