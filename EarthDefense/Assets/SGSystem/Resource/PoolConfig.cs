using UnityEngine;

namespace Script.SystemCore.Resource
{
    [System.Serializable]
    public class PoolConfig
    {
        [Tooltip("풀링 활성화 여부")]
        public bool enabled;

        [Tooltip("사전 생성 개수")]
        [Range(0, 100)]
        public int preloadCount;

        [Tooltip("최대 풀 크기 (0 = 무제한)")]
        [Range(0, 500)]
        public int maxPoolSize;

        [Tooltip("풀 소진 시 자동 확장")]
        public bool autoExpand = true;

        [Tooltip("초과분 주기적 정리")]
        public bool cullExcess;

        [Tooltip("정리 주기 (초)")]
        [Range(10f, 300f)]
        public float cullInterval = 60f;

        public PoolConfig()
        {
            enabled = false;
            preloadCount = 0;
            maxPoolSize = 0;
            autoExpand = true;
            cullExcess = false;
            cullInterval = 60f;
        }
    }
}
