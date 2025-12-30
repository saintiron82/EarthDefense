using System;

namespace SG.Save
{
    /// <summary>
    /// 세이브 슬롯 메타 정보
    /// 실제 게임 데이터와 독립적인 메타데이터만 포함
    /// </summary>
    [Serializable]
    public class SaveSlotInfo
    {
        /// <summary>슬롯 ID (파일명 기준)</summary>
        public string SlotId;

        /// <summary>유저가 지정한 세이브 이름</summary>
        public string DisplayName;

        /// <summary>저장 시각 (ISO 8601)</summary>
        public string SavedAt;

        /// <summary>플레이 시간 (초)</summary>
        public float PlayTime;

        /// <summary>데이터 버전</summary>
        public int Version;

        /// <summary>게임 레벨 (표시용, 옵션)</summary>
        public int GameLevel;

        /// <summary>썸네일 경로 (옵션)</summary>
        public string ThumbnailPath;

        /// <summary>
        /// 표시용 플레이 시간 포맷
        /// </summary>
        public string GetFormattedPlayTime()
        {
            var ts = TimeSpan.FromSeconds(PlayTime);
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
            if (DateTime.TryParse(SavedAt, out var dt))
            {
                return dt.ToString("yyyy-MM-dd HH:mm");
            }
            return SavedAt;
        }
    }
}