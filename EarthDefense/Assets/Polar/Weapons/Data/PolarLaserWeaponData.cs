﻿using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 레이저 무기 전용 데이터 (Drill 타입)
    /// - 느낌: 좁은 곳을 꾸준히 지지는 용접기
    /// - 전술: 약점 집중 타격, 장시간 유지
    /// - ⭐ 빔 폭에 비례한 다중 섹터 타격 지원
    /// </summary>
    [CreateAssetMenu(fileName = "PolarLaserWeaponData", menuName = "EarthDefense/Polar/Weapon Data/Laser", order = 10)]
    public class PolarLaserWeaponData : PolarWeaponData
    {
        [Header("Laser Specific")]
        [Tooltip("빔 확장 속도")]
        [SerializeField, Min(1f)] private float extendSpeed = 50f;

        [Tooltip("빔 수축 속도")]
        [SerializeField, Min(1f)] private float retractSpeed = 70f;

        [Tooltip("빔 최대 길이")]
        [SerializeField, Min(1f)] private float maxLength = 50f;

        [Tooltip("빔 두께 (시각적 + 타격 범위). 두꺼울수록 더 많은 섹터 동시 타격")]
        [SerializeField, Min(0.01f)] private float beamWidth = 0.1f;

        [Tooltip("빔 색상")]
        [SerializeField] private Color beamColor = Color.cyan;

        [Tooltip("지속 시간 (0 = 무한)")]
        [SerializeField, Min(0f)] private float duration = 2f;

        public float ExtendSpeed => extendSpeed;
        public float RetractSpeed => retractSpeed;
        public float MaxLength => maxLength;
        public float BeamWidth => beamWidth;
        public Color BeamColor => beamColor;
        public float Duration => duration;

        public override string ToJson(bool prettyPrint = true)
        {
            var data = new LaserWeaponDataJson
            {
                // Base
                baseData = base.ToJson(false),
                // Laser Specific
                extendSpeed = this.extendSpeed,
                retractSpeed = this.retractSpeed,
                maxLength = this.maxLength,
                beamWidth = this.beamWidth,
                beamColor = new[] { beamColor.r, beamColor.g, beamColor.b, beamColor.a },
                duration = this.duration
            };
            
            return JsonUtility.ToJson(data, prettyPrint);
        }

        public override void FromJson(string json)
        {
            var data = JsonUtility.FromJson<LaserWeaponDataJson>(json);
            
            // Base
            base.FromJson(data.baseData);
            
            // Laser Specific
            this.extendSpeed = data.extendSpeed;
            this.retractSpeed = data.retractSpeed;
            this.maxLength = data.maxLength;
            this.beamWidth = data.beamWidth;
            if (data.beamColor != null && data.beamColor.Length == 4)
            {
                this.beamColor = new Color(data.beamColor[0], data.beamColor[1], data.beamColor[2], data.beamColor[3]);
            }
            this.duration = data.duration;
        }

        [System.Serializable]
        private class LaserWeaponDataJson
        {
            public string baseData;
            public float extendSpeed;
            public float retractSpeed;
            public float maxLength;
            public float beamWidth;
            public float[] beamColor;
            public float duration;
        }
    }
}
