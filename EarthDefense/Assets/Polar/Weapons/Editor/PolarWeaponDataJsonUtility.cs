using UnityEngine;
using UnityEditor;
using System.IO;

namespace Polar.Weapons.Editor
{
    /// <summary>
    /// Polar 무기 데이터 JSON 내보내기/가져오기 유틸리티
    /// </summary>
    public static class PolarWeaponDataJsonUtility
    {
        [MenuItem("Assets/Polar/Export Weapon Data to JSON", false, 1000)]
        private static void ExportToJson()
        {
            var selected = Selection.activeObject as PolarWeaponData;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a PolarWeaponData asset.", "OK");
                return;
            }

            string json = selected.ToJson(true);
            string path = EditorUtility.SaveFilePanel(
                "Export Weapon Data to JSON",
                Application.dataPath,
                selected.WeaponName + ".json",
                "json"
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, json);
                Debug.Log($"[PolarWeaponData] Exported to: {path}\n{json}");
                EditorUtility.DisplayDialog("Success", $"Weapon data exported to:\n{path}", "OK");
            }
        }

        [MenuItem("Assets/Polar/Import Weapon Data from JSON", false, 1001)]
        private static void ImportFromJson()
        {
            var selected = Selection.activeObject as PolarWeaponData;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a PolarWeaponData asset to import into.", "OK");
                return;
            }

            string path = EditorUtility.OpenFilePanel(
                "Import Weapon Data from JSON",
                Application.dataPath,
                "json"
            );

            if (!string.IsNullOrEmpty(path))
            {
                string json = File.ReadAllText(path);
                
                Undo.RecordObject(selected, "Import Weapon Data from JSON");
                selected.FromJson(json);
                EditorUtility.SetDirty(selected);
                
                Debug.Log($"[PolarWeaponData] Imported from: {path}");
                EditorUtility.DisplayDialog("Success", $"Weapon data imported from:\n{path}", "OK");
            }
        }

        [MenuItem("Assets/Polar/Export All Weapons to JSON", false, 1002)]
        private static void ExportAllToJson()
        {
            var guids = AssetDatabase.FindAssets("t:PolarWeaponData");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No PolarWeaponData assets found.", "OK");
                return;
            }

            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder",
                Application.dataPath,
                "WeaponData"
            );

            if (string.IsNullOrEmpty(folderPath)) return;

            int exported = 0;
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var weaponData = AssetDatabase.LoadAssetAtPath<PolarWeaponData>(assetPath);
                
                if (weaponData != null)
                {
                    string json = weaponData.ToJson(true);
                    string fileName = string.IsNullOrEmpty(weaponData.WeaponName) 
                        ? weaponData.name 
                        : weaponData.WeaponName;
                    string filePath = Path.Combine(folderPath, $"{fileName}.json");
                    
                    File.WriteAllText(filePath, json);
                    exported++;
                }
            }

            Debug.Log($"[PolarWeaponData] Exported {exported} weapon data files to: {folderPath}");
            EditorUtility.DisplayDialog("Success", $"Exported {exported} weapon data files to:\n{folderPath}", "OK");
        }

        [MenuItem("Assets/Polar/Export Weapon Data to JSON", true)]
        [MenuItem("Assets/Polar/Import Weapon Data from JSON", true)]
        private static bool ValidateSingleWeaponData()
        {
            return Selection.activeObject is PolarWeaponData;
        }
    }

    /// <summary>
    /// PolarWeaponData Inspector 확장
    /// </summary>
    [CustomEditor(typeof(PolarWeaponData), true)]
    public class PolarWeaponDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("JSON Export/Import", EditorStyles.boldLabel);

            var weaponData = target as PolarWeaponData;

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Export to JSON"))
            {
                string json = weaponData.ToJson(true);
                string path = EditorUtility.SaveFilePanel(
                    "Export Weapon Data",
                    Application.dataPath,
                    weaponData.WeaponName + ".json",
                    "json"
                );

                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllText(path, json);
                    Debug.Log($"Exported: {path}\n{json}");
                }
            }

            if (GUILayout.Button("Import from JSON"))
            {
                string path = EditorUtility.OpenFilePanel(
                    "Import Weapon Data",
                    Application.dataPath,
                    "json"
                );

                if (!string.IsNullOrEmpty(path))
                {
                    string json = File.ReadAllText(path);
                    Undo.RecordObject(weaponData, "Import from JSON");
                    weaponData.FromJson(json);
                    EditorUtility.SetDirty(weaponData);
                    Debug.Log($"Imported: {path}");
                }
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Copy JSON to Clipboard"))
            {
                string json = weaponData.ToJson(true);
                EditorGUIUtility.systemCopyBuffer = json;
                Debug.Log($"JSON copied to clipboard:\n{json}");
            }
        }
    }
}

