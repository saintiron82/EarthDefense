using UnityEngine;
using Polar.Field;
using System;

namespace Polar.Weapons.Effects
{
    /// <summary>
    /// 중력장 효과 - 충돌 시 해당 영역의 적 이동 속도 감소
    /// </summary>
    [CreateAssetMenu(fileName = "GravityFieldEffect", menuName = "EarthDefense/Polar/Effects/Gravity Field", order = 1)]
    public class PolarGravityFieldEffect : PolarEffectBase
    {
        [Header("Gravity Field Settings")]
        [SerializeField, Tooltip("중력장 영향 반경 (섹터 개수)")]
        [Range(1, 30)]
        private int fieldRadius = 10;
        
        [SerializeField, Tooltip("적 이동 속도 배율 (0.2 = 80% 둔화)")]
        [Range(0.0f, 1.0f)]
        private float speedMultiplier = 0.2f;
        
        [SerializeField, Tooltip("중력장 지속 시간 (초)")]
        [Range(1f, 30f)]
        private float duration = 5f;
        
        [SerializeField, Tooltip("가우시안 감쇠 사용")]
        private bool useGaussianFalloff = true;
        
        [Header("Visual")]
        [SerializeField] private Color fieldColor = new Color(0.5f, 0.8f, 1f, 0.5f);

        public override void OnImpact(IPolarField field, int sectorIndex, Vector2 position, PolarWeaponData weaponData)
        {
            var fieldController = field as PolarFieldController;
            if (fieldController == null)
            {
                Debug.LogWarning("[GravityFieldEffect] Field is not PolarFieldController!");
                return;
            }

            ApplyGravityField(fieldController, sectorIndex);
        }

        private void ApplyGravityField(PolarFieldController field, int centerSectorIndex)
        {
            int radius = fieldRadius;
            float multiplier = speedMultiplier;
            
            // 중앙 섹터
            field.SetSectorSpeedMultiplier(centerSectorIndex, multiplier);
            
            // 주변 섹터
            for (int offset = 1; offset <= radius; offset++)
            {
                float influence = multiplier;
                
                if (useGaussianFalloff)
                {
                    // 가우시안 감쇠
                    float sigma = radius / 3f;
                    float gaussian = Mathf.Exp(-(offset * offset) / (2f * sigma * sigma));
                    influence = Mathf.Lerp(1f, multiplier, gaussian);
                }
                else
                {
                    // 선형 감쇠
                    float t = 1f - ((float)offset / radius);
                    influence = Mathf.Lerp(1f, multiplier, t);
                }
                
                int leftIndex = (centerSectorIndex - offset + field.SectorCount) % field.SectorCount;
                int rightIndex = (centerSectorIndex + offset) % field.SectorCount;
                
                field.SetSectorSpeedMultiplier(leftIndex, influence);
                field.SetSectorSpeedMultiplier(rightIndex, influence);
            }
            
            Debug.Log($"[GravityFieldEffect] Applied gravity field at sector {centerSectorIndex}, radius {radius}");
        }
        
        public override string ToJson(bool prettyPrint = true)
        {
            var data = new GravityFieldEffectJson
            {
                effectId = this.effectId,
                effectName = this.effectName,
                triggerCondition = new TriggerConditionJson
                {
                    triggerType = this.triggerCondition.triggerType.ToString(),
                    probability = this.triggerCondition.probability,
                    delay = this.triggerCondition.delay,
                    interval = this.triggerCondition.interval,
                    distanceStep = this.triggerCondition.distanceStep,
                    triggerTime = this.triggerCondition.triggerTime,
                    maxTriggerCount = this.triggerCondition.maxTriggerCount,
                    cooldown = this.triggerCondition.cooldown
                },
                fieldRadius = this.fieldRadius,
                speedMultiplier = this.speedMultiplier,
                duration = this.duration,
                useGaussianFalloff = this.useGaussianFalloff,
                fieldColor = new float[] { fieldColor.r, fieldColor.g, fieldColor.b, fieldColor.a }
            };
            
            return JsonUtility.ToJson(data, prettyPrint);
        }
        
        public override void FromJson(string json)
        {
            var data = JsonUtility.FromJson<GravityFieldEffectJson>(json);
            
            this.effectId = data.effectId;
            this.effectName = data.effectName;
            
            // Trigger Condition 복원
            if (data.triggerCondition != null)
            {
                if (System.Enum.TryParse<EffectTriggerType>(data.triggerCondition.triggerType, out var triggerType))
                {
                    this.triggerCondition.triggerType = triggerType;
                }
                this.triggerCondition.probability = data.triggerCondition.probability;
                this.triggerCondition.delay = data.triggerCondition.delay;
                this.triggerCondition.interval = data.triggerCondition.interval;
                this.triggerCondition.distanceStep = data.triggerCondition.distanceStep;
                this.triggerCondition.triggerTime = data.triggerCondition.triggerTime;
                this.triggerCondition.maxTriggerCount = data.triggerCondition.maxTriggerCount;
                this.triggerCondition.cooldown = data.triggerCondition.cooldown;
            }
            
            this.fieldRadius = data.fieldRadius;
            this.speedMultiplier = data.speedMultiplier;
            this.duration = data.duration;
            this.useGaussianFalloff = data.useGaussianFalloff;
            
            if (data.fieldColor != null && data.fieldColor.Length == 4)
            {
                this.fieldColor = new Color(data.fieldColor[0], data.fieldColor[1], data.fieldColor[2], data.fieldColor[3]);
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        [Serializable]
        private class TriggerConditionJson
        {
            public string triggerType;
            public float probability;
            public float delay;
            public float interval;
            public float distanceStep;
            public float triggerTime;
            public int maxTriggerCount;
            public float cooldown;
        }
        
        [Serializable]
        private class GravityFieldEffectJson
        {
            public string effectId;
            public string effectName;
            public TriggerConditionJson triggerCondition;
            public int fieldRadius;
            public float speedMultiplier;
            public float duration;
            public bool useGaussianFalloff;
            public float[] fieldColor;
        }
    }
}
