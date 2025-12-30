using System;
using UnityEngine;
using SG;

namespace SG.Save
{
    /// <summary>
    /// 모든 저장 가능한 데이터의 베이스 클래스
    /// 메타데이터(버전, 저장 시간, 파일명 등)를 표준화
    /// </summary>
    [Serializable]
    public abstract class SaveData : ISaveable
    {
        // ==================== 메타데이터 (모든 세이브 데이터 공통) ====================
        
        /// <summary>데이터 버전 (호환성 체크용)</summary>
        [SerializeField] protected int version = 1;
        
        /// <summary>고유 슬롯 ID</summary>
        public string SaveId { get; set; }
        
        /// <summary>유저가 지정한 세이브 파일 이름</summary>
        public string SaveFileName;
        
        /// <summary>마지막 저장 시각 (ISO 8601)</summary>
        public string LastSaveTime;
        
        /// <summary>총 플레이 시간 (초)</summary>
        public float TotalPlayTime;
        
        /// <summary>게임 레벨 (표시용, 옵션)</summary>
        public int GameLevel;

        // ==================== ISaveable 구현 ====================
        
        public virtual int Version => version;

        /// <summary>
        /// 직렬화 (서브클래스에서 오버라이드 가능)
        /// </summary>
        public virtual string Serialize()
        {
            // 저장 전 메타데이터 자동 갱신
            LastSaveTime = DateTime.Now.ToString("o");
            
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// 역직렬화 (서브클래스에서 오버라이드 가능)
        /// </summary>
        public virtual void Deserialize(string data)
        {
            JsonUtility.FromJsonOverwrite(data, this);
        }

        // ==================== 저장/로드 헬퍼 (템플릿 메서드) ====================

        /// <summary>
        /// 이 SaveData를 저장 (편의 메서드)
        /// </summary>
        /// <param name="slotId">슬롯 ID (null이면 자동 생성)</param>
        /// <param name="displayName">표시 이름 (null이면 자동 생성)</param>
        public void Save(string slotId = null, string displayName = null)
        {
            var saveService = GetSaveService();
            if (saveService == null)
            {
                return;
            }

            SaveTo(saveService, slotId, displayName);
        }

        /// <summary>
        /// SaveService를 명시적으로 받아 저장 (권장)
        /// </summary>
        /// <param name="saveService">SaveService 인스턴스</param>
        /// <param name="slotId">슬롯 ID</param>
        /// <param name="displayName">표시 이름</param>
        public void SaveTo(SaveService saveService, string slotId = null, string displayName = null)
        {
            if (saveService == null)
            {
                Debug.LogError("[SaveData] SaveService is null");
                return;
            }

            // 슬롯 ID 결정
            if (string.IsNullOrEmpty(slotId))
            {
                slotId = SaveId;
            }
            if (string.IsNullOrEmpty(slotId))
            {
                slotId = saveService.GenerateAutoSaveSlotId();
            }

            // 표시 이름 결정
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = SaveFileName;
            }
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = GenerateDefaultDisplayName();
            }

            // 메타데이터 설정
            SaveId = slotId;
            SaveFileName = displayName;
            
            // 서브클래스가 추가 메타데이터 설정 가능
            OnBeforeSave();

            // SaveService를 통해 저장
            saveService.Save(slotId, this, displayName, TotalPlayTime, GameLevel);
            
            OnAfterSave();
        }

        // ==================== 훅 메서드 (서브클래스가 오버라이드 가능) ====================

        /// <summary>
        /// 저장 직전 호출 (추가 메타데이터 설정 가능)
        /// </summary>
        protected virtual void OnBeforeSave()
        {
        }

        /// <summary>
        /// 저장 직후 호출 (로깅, 알림 등)
        /// </summary>
        protected virtual void OnAfterSave()
        {
        }

        /// <summary>
        /// 로드 직후 호출 (데이터 복원 로직)
        /// </summary>
        protected virtual void OnAfterLoad()
        {
        }

        /// <summary>
        /// 기본 표시 이름 생성 (서브클래스가 오버라이드 가능)
        /// </summary>
        protected virtual string GenerateDefaultDisplayName()
        {
            return $"Save {DateTime.Now:yyyy-MM-dd HH:mm}";
        }

        // ==================== 유틸리티 ====================

        /// <summary>
        /// SaveService 가져오기 (헬퍼)
        /// </summary>
        protected SaveService GetSaveService()
        {
            var saveService = App.Instance?.ServiceHome?.GetService<SaveService>();
            if (saveService == null)
            {
                Debug.LogError($"[{GetType().Name}] SaveService not available");
            }
            return saveService;
        }

        /// <summary>
        /// 표시용 플레이 시간 포맷
        /// </summary>
        public string GetFormattedPlayTime()
        {
            var ts = TimeSpan.FromSeconds(TotalPlayTime);
            if (ts.TotalHours >= 1)
            {
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            }
            return $"{ts.Minutes}m {ts.Seconds}s";
        }

        /// <summary>
        /// 저장 시각 표시용
        /// </summary>
        public string GetFormattedSaveTime()
        {
            if (DateTime.TryParse(LastSaveTime, out var dt))
            {
                return dt.ToString("yyyy-MM-dd HH:mm");
            }
            return LastSaveTime;
        }

        // ==================== 정적 로드 메서드 (타입 안전) ====================

        /// <summary>
        /// SaveData 로드 (제네릭 헬퍼)
        /// </summary>
        /// <typeparam name="T">SaveData 서브클래스</typeparam>
        /// <param name="saveService">SaveService 인스턴스</param>
        /// <param name="slotId">슬롯 ID</param>
        /// <returns>로드된 데이터 (실패 시 null)</returns>
        public static T LoadFrom<T>(SaveService saveService, string slotId) where T : SaveData, new()
        {
            if (saveService == null)
            {
                Debug.LogError($"[SaveData] SaveService is null");
                return null;
            }

            var data = saveService.Load<T>(slotId);
            data?.OnAfterLoad();
            return data;
        }

    }
}
