using System;
using UnityEngine;

namespace Polar.Weapons.Effects
{
    /// <summary>
    /// Effect 베이스 클래스 - JSON 직렬화 지원
    /// </summary>
    public abstract class PolarEffectBase : ScriptableObject, IPolarProjectileEffect
    {
        [Header("Effect ID")]
        [SerializeField] protected string effectId;
        [SerializeField] protected string effectName;
        
        [Header("Trigger Condition")]
        [SerializeField] protected EffectTriggerCondition triggerCondition = new EffectTriggerCondition();
        
        public string EffectId => effectId;
        public string EffectName => effectName;
        public EffectTriggerCondition TriggerCondition => triggerCondition;
        
        /// <summary>
        /// 충돌 시 발동
        /// </summary>
        public abstract void OnImpact(IPolarField field, int sectorIndex, Vector2 position, PolarWeaponData weaponData);
        
        /// <summary>
        /// JSON으로 직렬화
        /// </summary>
        public abstract string ToJson(bool prettyPrint = true);
        
        /// <summary>
        /// JSON에서 역직렬화
        /// </summary>
        public abstract void FromJson(string json);
    }
}

