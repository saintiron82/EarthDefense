using UnityEngine;

namespace Script.SystemCore.Resource
{
    [System.Serializable]
    public class ResourceEntry
    {
        [Tooltip("내부 식별 ID")]
        public string id;

        [Tooltip("Addressables 주소 키")]
        public string addressableKey;

        [Tooltip("리소스 타입")]
        public ResourceType type;

        [Tooltip("직접 참조 (Fallback)")]
        public UnityEngine.Object directAsset;

        [Tooltip("캐시 그룹 (레벨/카테고리별 관리)")]
        public string cacheGroup;

        [Header("Pool Settings (Optional)")]
        [Tooltip("이 리소스만의 풀 설정. null이면 번들 기본값 사용")]
        public PoolConfigPreset poolPresetOverride;
    }
}
