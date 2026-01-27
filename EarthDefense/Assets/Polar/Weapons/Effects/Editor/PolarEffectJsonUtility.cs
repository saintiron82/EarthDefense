using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Polar.Weapons.Effects.Editor
{
    /// <summary>
    /// Effect JSON 내보내기/가져오기 유틸리티
    /// </summary>
    public static class PolarEffectJsonUtility
    {
        private const string EXPORT_PATH = "Assets/Polar/Data/Effects/Exported/";

        [MenuItem("Assets/Polar Effects/Export Selected to JSON", false, 100)]
        private static void ExportSelectedToJson()
        {
            var selectedEffects = Selection.GetFiltered<PolarEffectBase>(SelectionMode.Assets);
            
            if (selectedEffects.Length == 0)
            {
                EditorUtility.DisplayDialog("Export Effects", "No PolarEffectBase selected!", "OK");
                return;
            }

            if (!Directory.Exists(EXPORT_PATH))
            {
                Directory.CreateDirectory(EXPORT_PATH);
            }

            int exportCount = 0;
            foreach (var effect in selectedEffects)
            {
                string json = effect.ToJson(true);
                string fileName = $"{effect.EffectId}_{effect.name}.json";
                string filePath = Path.Combine(EXPORT_PATH, fileName);
                
                File.WriteAllText(filePath, json, Encoding.UTF8);
                exportCount++;
                
                Debug.Log($"[EffectJsonUtility] Exported: {filePath}");
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export Complete", 
                $"Exported {exportCount} effect(s) to:\n{EXPORT_PATH}", "OK");
        }

        [MenuItem("Assets/Polar Effects/Export Selected to JSON", true)]
        private static bool ValidateExportSelected()
        {
            return Selection.GetFiltered<PolarEffectBase>(SelectionMode.Assets).Length > 0;
        }

        [MenuItem("Assets/Polar Effects/Export All to JSON", false, 101)]
        private static void ExportAllToJson()
        {
            var allEffects = FindAllEffects();
            
            if (allEffects.Count == 0)
            {
                EditorUtility.DisplayDialog("Export All Effects", "No effects found!", "OK");
                return;
            }

            if (!Directory.Exists(EXPORT_PATH))
            {
                Directory.CreateDirectory(EXPORT_PATH);
            }

            int exportCount = 0;
            foreach (var effect in allEffects)
            {
                string json = effect.ToJson(true);
                string fileName = $"{effect.EffectId}_{effect.name}.json";
                string filePath = Path.Combine(EXPORT_PATH, fileName);
                
                File.WriteAllText(filePath, json, Encoding.UTF8);
                exportCount++;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export Complete", 
                $"Exported {exportCount} effect(s) to:\n{EXPORT_PATH}", "OK");
            
            Debug.Log($"[EffectJsonUtility] Exported {exportCount} effects");
        }

        [MenuItem("Assets/Polar Effects/Import from JSON", false, 102)]
        private static void ImportFromJson()
        {
            string filePath = EditorUtility.OpenFilePanel("Import Effect JSON", EXPORT_PATH, "json");
            
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            
            // 효과 타입 감지 (간단한 방법: effectId 접두사로 판단)
            var tempData = JsonUtility.FromJson<EffectIdHolder>(json);
            
            PolarEffectBase effect = null;
            if (tempData.effectId.StartsWith("gravity"))
            {
                effect = ScriptableObject.CreateInstance<PolarGravityFieldEffect>();
            }
            // 추가 효과 타입들...
            
            if (effect == null)
            {
                EditorUtility.DisplayDialog("Import Failed", 
                    $"Unknown effect type: {tempData.effectId}", "OK");
                return;
            }

            effect.FromJson(json);
            
            string assetPath = $"Assets/Polar/Data/Effects/{effect.EffectName}_{effect.EffectId}.asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            
            AssetDatabase.CreateAsset(effect, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = effect;
            
            EditorUtility.DisplayDialog("Import Complete", 
                $"Imported effect:\n{assetPath}", "OK");
            
            Debug.Log($"[EffectJsonUtility] Imported: {assetPath}");
        }

        [MenuItem("Assets/Polar Effects/Bulk Import from Folder", false, 103)]
        private static void BulkImportFromFolder()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select JSON Folder", EXPORT_PATH, "");
            
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            var jsonFiles = Directory.GetFiles(folderPath, "*.json");
            
            if (jsonFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("Bulk Import", "No JSON files found!", "OK");
                return;
            }

            int importCount = 0;
            foreach (var filePath in jsonFiles)
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var tempData = JsonUtility.FromJson<EffectIdHolder>(json);
                
                PolarEffectBase effect = null;
                if (tempData.effectId.StartsWith("gravity"))
                {
                    effect = ScriptableObject.CreateInstance<PolarGravityFieldEffect>();
                }
                
                if (effect != null)
                {
                    effect.FromJson(json);
                    
                    string assetPath = $"Assets/Polar/Data/Effects/{effect.EffectName}_{effect.EffectId}.asset";
                    assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                    
                    AssetDatabase.CreateAsset(effect, assetPath);
                    importCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Bulk Import Complete", 
                $"Imported {importCount} effect(s)", "OK");
            
            Debug.Log($"[EffectJsonUtility] Bulk imported {importCount} effects");
        }

        [MenuItem("Assets/Polar Effects/Open Export Folder", false, 200)]
        private static void OpenExportFolder()
        {
            if (!Directory.Exists(EXPORT_PATH))
            {
                Directory.CreateDirectory(EXPORT_PATH);
            }
            
            EditorUtility.RevealInFinder(EXPORT_PATH);
        }

        private static List<PolarEffectBase> FindAllEffects()
        {
            var effects = new List<PolarEffectBase>();
            var guids = AssetDatabase.FindAssets("t:PolarEffectBase");
            
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var effect = AssetDatabase.LoadAssetAtPath<PolarEffectBase>(path);
                if (effect != null)
                {
                    effects.Add(effect);
                }
            }
            
            return effects;
        }

        [System.Serializable]
        private class EffectIdHolder
        {
            public string effectId;
        }
    }
}

