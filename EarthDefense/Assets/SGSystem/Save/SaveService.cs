using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace SG.Save
{
    /// <summary>
    /// 범용 저장/로드 서비스
    /// 게임 로직에 완전히 무관한 순수 파일 I/O 레이어
    /// </summary>
    public class SaveService : ServiceBase
    {
        private const string SAVE_FOLDER = "Saves";
        private const string META_SUFFIX = ".meta.json";
        private const string DATA_SUFFIX = ".data.json";
        
        private string _savePath;
        private Dictionary<string, SaveSlotInfo> _cachedSlots;

        protected override void InitInternal()
        {
            base.InitInternal();

            _savePath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            _cachedSlots = new Dictionary<string, SaveSlotInfo>();

            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }

            RefreshSlotCache();
        }

        /// <summary>
        /// 슬롯 캐시 갱신
        /// </summary>
        private void RefreshSlotCache()
        {
            _cachedSlots.Clear();

            var metaFiles = Directory.GetFiles(_savePath, $"*{META_SUFFIX}");
            foreach (var metaFile in metaFiles)
            {
                try
                {
                    var json = File.ReadAllText(metaFile);
                    var info = JsonUtility.FromJson<SaveSlotInfo>(json);
                    if (info != null)
                    {
                        _cachedSlots[info.SlotId] = info;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveService] Failed to load meta: {metaFile}\n{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 데이터 저장 (동기)
        /// </summary>
        /// <param name="slotId">슬롯 ID</param>
        /// <param name="saveable">저장할 데이터</param>
        /// <param name="displayName">표시 이름</param>
        /// <param name="playTime">플레이 시간</param>
        /// <param name="gameLevel">게임 레벨 (옵션)</param>
        public void Save(string slotId, ISaveable saveable, string displayName, float playTime, int gameLevel = 0)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                Debug.LogError("[SaveService] SlotId is empty");
                return;
            }

            if (saveable == null)
            {
                Debug.LogError("[SaveService] Saveable is null");
                return;
            }

            try
            {
                // 데이터 직렬화
                var dataJson = saveable.Serialize();
                var dataPath = GetDataPath(slotId);
                File.WriteAllText(dataPath, dataJson);

                // 메타 정보 생성
                var meta = new SaveSlotInfo
                {
                    SlotId = slotId,
                    DisplayName = displayName,
                    SavedAt = DateTime.Now.ToString("o"),
                    PlayTime = playTime,
                    Version = saveable.Version,
                    GameLevel = gameLevel
                };

                var metaJson = JsonUtility.ToJson(meta, true);
                var metaPath = GetMetaPath(slotId);
                File.WriteAllText(metaPath, metaJson);

                // 캐시 갱신
                _cachedSlots[slotId] = meta;

                Debug.Log($"[SaveService] Saved: {slotId} ({displayName})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Save failed: {slotId}\n{ex}");
            }
        }

        /// <summary>
        /// 데이터 로드 (동기)
        /// </summary>
        /// <typeparam name="T">ISaveable 구현 타입</typeparam>
        /// <param name="slotId">슬롯 ID</param>
        /// <returns>로드된 데이터 (실패 시 null)</returns>
        public T Load<T>(string slotId) where T : ISaveable, new()
        {
            if (string.IsNullOrEmpty(slotId))
            {
                Debug.LogError("[SaveService] SlotId is empty");
                return default;
            }

            var dataPath = GetDataPath(slotId);
            if (!File.Exists(dataPath))
            {
                Debug.LogWarning($"[SaveService] Save file not found: {slotId}");
                return default;
            }

            try
            {
                var dataJson = File.ReadAllText(dataPath);
                var data = new T();
                data.Deserialize(dataJson);

                Debug.Log($"[SaveService] Loaded: {slotId}");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Load failed: {slotId}\n{ex}");
                return default;
            }
        }

        /// <summary>
        /// 세이브 삭제
        /// </summary>
        public void Delete(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                return;
            }

            try
            {
                var dataPath = GetDataPath(slotId);
                var metaPath = GetMetaPath(slotId);

                if (File.Exists(dataPath))
                {
                    File.Delete(dataPath);
                }

                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }

                _cachedSlots.Remove(slotId);

                Debug.Log($"[SaveService] Deleted: {slotId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Delete failed: {slotId}\n{ex}");
            }
        }

        /// <summary>
        /// 모든 슬롯 정보 조회
        /// </summary>
        public List<SaveSlotInfo> GetAllSlots()
        {
            var list = new List<SaveSlotInfo>(_cachedSlots.Values);

            list.Sort((a, b) =>
            {
                var dtA = DateTime.Parse(a.SavedAt);
                var dtB = DateTime.Parse(b.SavedAt);
                return dtB.CompareTo(dtA);
            });

            return list;
        }

        /// <summary>
        /// 특정 슬롯 존재 여부
        /// </summary>
        public bool Exists(string slotId)
        {
            return _cachedSlots.ContainsKey(slotId);
        }

        /// <summary>
        /// 슬롯 메타 정보 조회
        /// </summary>
        public SaveSlotInfo GetSlotInfo(string slotId)
        {
            return _cachedSlots.TryGetValue(slotId, out var info) ? info : null;
        }

        /// <summary>
        /// 자동 저장 슬롯 ID 생성
        /// </summary>
        public string GenerateAutoSaveSlotId()
        {
            return $"autosave_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        private string GetDataPath(string slotId)
        {
            return Path.Combine(_savePath, $"{slotId}{DATA_SUFFIX}");
        }

        private string GetMetaPath(string slotId)
        {
            return Path.Combine(_savePath, $"{slotId}{META_SUFFIX}");
        }

        public string SaveFolderPath => _savePath;
    }
}
