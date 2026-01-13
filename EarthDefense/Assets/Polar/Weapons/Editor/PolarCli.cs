using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

namespace Polar.Weapons.Editor
{
    /// <summary>
    /// Polar CLI 엔트리포인트 - batchmode에서 JSON Export/Import 지원
    ///
    /// 사용법:
    /// Unity.exe -batchmode -quit -projectPath "..." -executeMethod Polar.Weapons.Editor.PolarCli.ExportAll -exportDir="./Export"
    /// Unity.exe -batchmode -quit -projectPath "..." -executeMethod Polar.Weapons.Editor.PolarCli.ImportAll -importDir="./Export"
    /// </summary>
    public static class PolarCli
    {
        private const string ExportDirArg = "-exportDir=";
        private const string ImportDirArg = "-importDir=";

        #region CLI Entry Points

        /// <summary>
        /// 모든 무기 및 옵션 프로필을 Export
        /// </summary>
        public static void ExportAll()
        {
            string exportDir = GetArgValue(ExportDirArg);
            if (string.IsNullOrEmpty(exportDir))
            {
                Debug.LogError("[PolarCli] -exportDir argument is required");
                EditorApplication.Exit(1);
                return;
            }

            try
            {
                // 폴더 구조 생성
                string weaponsDir = Path.Combine(exportDir, "Weapons");
                string weaponOptionsDir = Path.Combine(exportDir, "Options", "Weapon");
                string projectileOptionsDir = Path.Combine(exportDir, "Options", "Projectile");

                Directory.CreateDirectory(weaponsDir);
                Directory.CreateDirectory(weaponOptionsDir);
                Directory.CreateDirectory(projectileOptionsDir);

                // Export
                int weaponCount = ExportWeaponsToFolder(weaponsDir);
                int weaponOptionCount = ExportWeaponOptionProfilesToFolder(weaponOptionsDir);
                int projectileOptionCount = ExportProjectileOptionProfilesToFolder(projectileOptionsDir);

                Debug.Log($"[PolarCli] Export complete: {weaponCount} weapons, {weaponOptionCount} weapon options, {projectileOptionCount} projectile options");
                Debug.Log($"[PolarCli] Export directory: {Path.GetFullPath(exportDir)}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PolarCli] Export failed: {e.Message}\n{e.StackTrace}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// 모든 무기 및 옵션 프로필을 Import
        /// Import 순서: 옵션 프로필 -> 무기 (의존성 고려)
        /// </summary>
        public static void ImportAll()
        {
            string importDir = GetArgValue(ImportDirArg);
            if (string.IsNullOrEmpty(importDir))
            {
                Debug.LogError("[PolarCli] -importDir argument is required");
                EditorApplication.Exit(1);
                return;
            }

            if (!Directory.Exists(importDir))
            {
                Debug.LogError($"[PolarCli] Import directory not found: {importDir}");
                EditorApplication.Exit(1);
                return;
            }

            try
            {
                // Import 순서: 옵션 프로필 먼저 (무기가 참조하므로)
                string weaponOptionsDir = Path.Combine(importDir, "Options", "Weapon");
                string projectileOptionsDir = Path.Combine(importDir, "Options", "Projectile");
                string weaponsDir = Path.Combine(importDir, "Weapons");

                int weaponOptionCount = 0;
                int projectileOptionCount = 0;
                int weaponCount = 0;

                if (Directory.Exists(weaponOptionsDir))
                {
                    weaponOptionCount = ImportWeaponOptionProfilesFromFolder(weaponOptionsDir);
                }

                if (Directory.Exists(projectileOptionsDir))
                {
                    projectileOptionCount = ImportProjectileOptionProfilesFromFolder(projectileOptionsDir);
                }

                if (Directory.Exists(weaponsDir))
                {
                    weaponCount = ImportWeaponsFromFolder(weaponsDir);
                }

                // 변경사항 저장
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[PolarCli] Import complete: {weaponCount} weapons, {weaponOptionCount} weapon options, {projectileOptionCount} projectile options");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PolarCli] Import failed: {e.Message}\n{e.StackTrace}");
                EditorApplication.Exit(1);
            }
        }

        #endregion

        #region Export Methods

        /// <summary>
        /// 무기 데이터를 폴더에 Export
        /// </summary>
        public static int ExportWeaponsToFolder(string folderPath)
        {
            Directory.CreateDirectory(folderPath);

            var guids = AssetDatabase.FindAssets("t:PolarWeaponData");
            int count = 0;

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var weaponData = AssetDatabase.LoadAssetAtPath<PolarWeaponData>(assetPath);

                if (weaponData != null)
                {
                    string json = weaponData.ToJson(true);
                    string fileName = !string.IsNullOrEmpty(weaponData.Id) ? weaponData.Id : weaponData.name;
                    string filePath = Path.Combine(folderPath, $"{fileName}.json");

                    File.WriteAllText(filePath, json);
                    count++;
                    Debug.Log($"[PolarCli] Exported weapon: {fileName}");
                }
            }

            return count;
        }

        /// <summary>
        /// 무기 옵션 프로필을 폴더에 Export
        /// </summary>
        public static int ExportWeaponOptionProfilesToFolder(string folderPath)
        {
            Directory.CreateDirectory(folderPath);

            var guids = AssetDatabase.FindAssets("t:PolarWeaponOptionProfile");
            int count = 0;

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<PolarWeaponOptionProfile>(assetPath);

                if (profile != null)
                {
                    string json = profile.ToJson(true);
                    string fileName = !string.IsNullOrEmpty(profile.Id) ? profile.Id : profile.name;
                    string filePath = Path.Combine(folderPath, $"{fileName}.json");

                    File.WriteAllText(filePath, json);
                    count++;
                    Debug.Log($"[PolarCli] Exported weapon option profile: {fileName}");
                }
            }

            return count;
        }

        /// <summary>
        /// 투사체 옵션 프로필을 폴더에 Export
        /// </summary>
        public static int ExportProjectileOptionProfilesToFolder(string folderPath)
        {
            Directory.CreateDirectory(folderPath);

            var guids = AssetDatabase.FindAssets("t:PolarProjectileOptionProfile");
            int count = 0;

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<PolarProjectileOptionProfile>(assetPath);

                if (profile != null)
                {
                    string json = profile.ToJson(true);
                    string fileName = !string.IsNullOrEmpty(profile.Id) ? profile.Id : profile.name;
                    string filePath = Path.Combine(folderPath, $"{fileName}.json");

                    File.WriteAllText(filePath, json);
                    count++;
                    Debug.Log($"[PolarCli] Exported projectile option profile: {fileName}");
                }
            }

            return count;
        }

        #endregion

        #region Import Methods

        /// <summary>
        /// 폴더에서 무기 데이터를 Import
        /// </summary>
        public static int ImportWeaponsFromFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return 0;

            // id -> asset 매핑 생성
            var weaponMap = BuildWeaponIdMap();
            var jsonFiles = Directory.GetFiles(folderPath, "*.json");
            int count = 0;

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    string weaponId = ExtractIdFromJson(json);

                    if (string.IsNullOrEmpty(weaponId))
                    {
                        Debug.LogWarning($"[PolarCli] No id found in: {filePath}");
                        continue;
                    }

                    if (weaponMap.TryGetValue(weaponId, out var weaponData))
                    {
                        Undo.RecordObject(weaponData, "Import Weapon from JSON");
                        weaponData.FromJson(json);
                        EditorUtility.SetDirty(weaponData);
                        count++;
                        Debug.Log($"[PolarCli] Imported weapon: {weaponId}");
                    }
                    else
                    {
                        Debug.LogWarning($"[PolarCli] Weapon not found for id: {weaponId}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PolarCli] Failed to import {filePath}: {e.Message}");
                }
            }

            return count;
        }

        /// <summary>
        /// 폴더에서 무기 옵션 프로필을 Import
        /// </summary>
        public static int ImportWeaponOptionProfilesFromFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return 0;

            var profileMap = BuildWeaponOptionProfileIdMap();
            var jsonFiles = Directory.GetFiles(folderPath, "*.json");
            int count = 0;

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    string profileId = ExtractIdFromJson(json);

                    if (string.IsNullOrEmpty(profileId))
                    {
                        Debug.LogWarning($"[PolarCli] No id found in: {filePath}");
                        continue;
                    }

                    if (profileMap.TryGetValue(profileId, out var profile))
                    {
                        Undo.RecordObject(profile, "Import Weapon Option Profile from JSON");
                        profile.FromJson(json);
                        EditorUtility.SetDirty(profile);
                        count++;
                        Debug.Log($"[PolarCli] Imported weapon option profile: {profileId}");
                    }
                    else
                    {
                        Debug.LogWarning($"[PolarCli] Weapon option profile not found for id: {profileId}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PolarCli] Failed to import {filePath}: {e.Message}");
                }
            }

            return count;
        }

        /// <summary>
        /// 폴더에서 투사체 옵션 프로필을 Import
        /// </summary>
        public static int ImportProjectileOptionProfilesFromFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return 0;

            var profileMap = BuildProjectileOptionProfileIdMap();
            var jsonFiles = Directory.GetFiles(folderPath, "*.json");
            int count = 0;

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    string profileId = ExtractIdFromJson(json);

                    if (string.IsNullOrEmpty(profileId))
                    {
                        Debug.LogWarning($"[PolarCli] No id found in: {filePath}");
                        continue;
                    }

                    if (profileMap.TryGetValue(profileId, out var profile))
                    {
                        Undo.RecordObject(profile, "Import Projectile Option Profile from JSON");
                        profile.FromJson(json);
                        EditorUtility.SetDirty(profile);
                        count++;
                        Debug.Log($"[PolarCli] Imported projectile option profile: {profileId}");
                    }
                    else
                    {
                        Debug.LogWarning($"[PolarCli] Projectile option profile not found for id: {profileId}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PolarCli] Failed to import {filePath}: {e.Message}");
                }
            }

            return count;
        }

        #endregion

        #region Helper Methods

        private static string GetArgValue(string argPrefix)
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.StartsWith(argPrefix))
                {
                    return arg.Substring(argPrefix.Length).Trim('"');
                }
            }
            return null;
        }

        private static string ExtractIdFromJson(string json)
        {
            // 간단한 JSON 파싱 (JsonUtility 사용)
            var idObj = JsonUtility.FromJson<IdContainer>(json);
            return idObj?.id;
        }

        [Serializable]
        private class IdContainer
        {
            public string id;
        }

        private static Dictionary<string, PolarWeaponData> BuildWeaponIdMap()
        {
            var map = new Dictionary<string, PolarWeaponData>();
            var guids = AssetDatabase.FindAssets("t:PolarWeaponData");

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var weaponData = AssetDatabase.LoadAssetAtPath<PolarWeaponData>(assetPath);

                if (weaponData != null && !string.IsNullOrEmpty(weaponData.Id))
                {
                    if (!map.ContainsKey(weaponData.Id))
                    {
                        map[weaponData.Id] = weaponData;
                    }
                    else
                    {
                        Debug.LogWarning($"[PolarCli] Duplicate weapon id: {weaponData.Id}");
                    }
                }
            }

            return map;
        }

        private static Dictionary<string, PolarWeaponOptionProfile> BuildWeaponOptionProfileIdMap()
        {
            var map = new Dictionary<string, PolarWeaponOptionProfile>();
            var guids = AssetDatabase.FindAssets("t:PolarWeaponOptionProfile");

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<PolarWeaponOptionProfile>(assetPath);

                if (profile != null && !string.IsNullOrEmpty(profile.Id))
                {
                    if (!map.ContainsKey(profile.Id))
                    {
                        map[profile.Id] = profile;
                    }
                    else
                    {
                        Debug.LogWarning($"[PolarCli] Duplicate weapon option profile id: {profile.Id}");
                    }
                }
            }

            return map;
        }

        private static Dictionary<string, PolarProjectileOptionProfile> BuildProjectileOptionProfileIdMap()
        {
            var map = new Dictionary<string, PolarProjectileOptionProfile>();
            var guids = AssetDatabase.FindAssets("t:PolarProjectileOptionProfile");

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<PolarProjectileOptionProfile>(assetPath);

                if (profile != null && !string.IsNullOrEmpty(profile.Id))
                {
                    if (!map.ContainsKey(profile.Id))
                    {
                        map[profile.Id] = profile;
                    }
                    else
                    {
                        Debug.LogWarning($"[PolarCli] Duplicate projectile option profile id: {profile.Id}");
                    }
                }
            }

            return map;
        }

        #endregion

        #region Editor Menu (Optional)

        [MenuItem("Polar/CLI/Export All to Folder...", false, 100)]
        private static void MenuExportAll()
        {
            string folderPath = EditorUtility.SaveFolderPanel("Select Export Folder", Application.dataPath, "Export");
            if (string.IsNullOrEmpty(folderPath)) return;

            string weaponsDir = Path.Combine(folderPath, "Weapons");
            string weaponOptionsDir = Path.Combine(folderPath, "Options", "Weapon");
            string projectileOptionsDir = Path.Combine(folderPath, "Options", "Projectile");

            Directory.CreateDirectory(weaponsDir);
            Directory.CreateDirectory(weaponOptionsDir);
            Directory.CreateDirectory(projectileOptionsDir);

            int weaponCount = ExportWeaponsToFolder(weaponsDir);
            int weaponOptionCount = ExportWeaponOptionProfilesToFolder(weaponOptionsDir);
            int projectileOptionCount = ExportProjectileOptionProfilesToFolder(projectileOptionsDir);

            EditorUtility.DisplayDialog("Export Complete",
                $"Exported:\n- {weaponCount} weapons\n- {weaponOptionCount} weapon options\n- {projectileOptionCount} projectile options\n\nTo: {folderPath}",
                "OK");
        }

        [MenuItem("Polar/CLI/Import All from Folder...", false, 101)]
        private static void MenuImportAll()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Import Folder", Application.dataPath, "Export");
            if (string.IsNullOrEmpty(folderPath)) return;

            string weaponOptionsDir = Path.Combine(folderPath, "Options", "Weapon");
            string projectileOptionsDir = Path.Combine(folderPath, "Options", "Projectile");
            string weaponsDir = Path.Combine(folderPath, "Weapons");

            int weaponOptionCount = 0;
            int projectileOptionCount = 0;
            int weaponCount = 0;

            if (Directory.Exists(weaponOptionsDir))
            {
                weaponOptionCount = ImportWeaponOptionProfilesFromFolder(weaponOptionsDir);
            }

            if (Directory.Exists(projectileOptionsDir))
            {
                projectileOptionCount = ImportProjectileOptionProfilesFromFolder(projectileOptionsDir);
            }

            if (Directory.Exists(weaponsDir))
            {
                weaponCount = ImportWeaponsFromFolder(weaponsDir);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Import Complete",
                $"Imported:\n- {weaponCount} weapons\n- {weaponOptionCount} weapon options\n- {projectileOptionCount} projectile options\n\nFrom: {folderPath}",
                "OK");
        }

        #endregion
    }
}
