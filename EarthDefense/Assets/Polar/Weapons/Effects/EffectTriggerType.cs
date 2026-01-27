using UnityEngine;

namespace Polar.Weapons.Effects
{
    /// <summary>
    /// Effect 발동 시점 타입
    /// </summary>
    public enum EffectTriggerType
    {
        OnImpact,           // 충돌 시 (벽에 닿을 때)
        OnLaunch,           // 발사 시 (투사체가 생성될 때)
        OnDestroy,          // 소멸 시 (투사체가 사라질 때)
        OnInterval,         // 주기적 (이동 중 일정 간격)
        OnDistance,         // 거리 기반 (특정 거리마다)
        OnTime,             // 시간 기반 (발사 후 N초 뒤)
        OnPenetrate,        // 관통 시 (적을 뚫을 때)
        OnKill,             // 적 처치 시
        OnLowHealth,        // 저체력 시
        OnCharge,           // 충전 완료 시
        Manual              // 수동 (외부 호출)
    }

    /// <summary>
    /// Effect 발동 조건
    /// </summary>
    [System.Serializable]
    public class EffectTriggerCondition
    {
        [Tooltip("발동 시점 타입")]
        public EffectTriggerType triggerType = EffectTriggerType.OnImpact;
        
        [Tooltip("발동 확률 (0~1, 1 = 100%)")]
        [Range(0f, 1f)]
        public float probability = 1f;
        
        [Tooltip("발동 지연 시간 (초)")]
        public float delay;
        
        [Tooltip("주기 (OnInterval 타입용, 초)")]
        public float interval = 1f;
        
        [Tooltip("거리 간격 (OnDistance 타입용)")]
        public float distanceStep = 5f;
        
        [Tooltip("트리거 시간 (OnTime 타입용, 초)")]
        public float triggerTime = 2f;
        
        [Tooltip("최대 발동 횟수 (0 = 무제한)")]
        public int maxTriggerCount = 1;
        
        [Tooltip("쿨다운 (초, 0 = 없음)")]
        public float cooldown;

        /// <summary>
        /// 발동 가능한지 확인
        /// </summary>
        public bool CanTrigger(bool enableDebug = false)
        {
            // 확률 체크
            if (probability < 1f && Random.value > probability)
            {
                if (enableDebug)
                {
                    Debug.Log($"[EffectTrigger] Probability check failed: {probability * 100}%");
                }
                return false;
            }
            
            return true;
        }
    }
}

