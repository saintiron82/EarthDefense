﻿using UnityEngine;

namespace Polar.Weapons
{
    /// <summary>
    /// 반사 모드
    /// </summary>
    public enum LaserReflectMode
    {
        /// <summary>
        /// 세그먼트 방식: 하나의 LineRenderer로 연결된 빔
        /// - Fire 홀드 중 전체가 시각적으로 연결됨
        /// </summary>
        Segment = 0,

        /// <summary>
        /// 자식 빔 방식: 각각 독립 빔이지만 부모-자식 관계로 연결 유지
        /// - 각 빔이 독립적인 LineRenderer를 가짐
        /// </summary>
        ChildBeam = 1
    }

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
        [Tooltip("빔 이동 속도 (확장/수축/펄스 공통)")]
        [SerializeField, Min(1f)] private float beamSpeed = 20f;

        [Tooltip("빔 최대 길이")]
        [SerializeField, Min(1f)] private float maxLength = 50f;

        [Tooltip("빔 두께 (시각적 + 타격 범위). 두꺼울수록 더 많은 섹터 동시 타격")]
        [SerializeField, Min(0.01f)] private float beamWidth = 0.1f;

        [Tooltip("빔 색상")]
        [SerializeField] private Color beamColor = Color.cyan;

        [Tooltip("지속 시간 (0 = 무한)")]
        [SerializeField, Min(0f)] private float duration = 2f;

        [Header("Reflect Settings")]
        [Tooltip("반사 모드\n- Segment: 하나의 연결된 빔 (LineRenderer 다중 포인트)\n- ChildBeam: 독립 빔들이 부모-자식 관계로 연결")]
        [SerializeField] private LaserReflectMode reflectMode = LaserReflectMode.Segment;

        [Tooltip("반사 횟수 (0 = 반사 안함)")]
        [SerializeField, Min(0)] private int reflectCount = 0;

        [Tooltip("반사 시 데미지 배율 (1.0 = 100%)")]
        [SerializeField, Range(0.1f, 1.5f)] private float reflectDamageMultiplier = 0.7f;

        [Tooltip("반사 각도 범위 (degree, 0 = 직각 반사만)")]
        [SerializeField, Range(0f, 180f)] private float reflectAngleRange = 45f;

        public float BeamSpeed => beamSpeed;
        public float MaxLength => maxLength;
        public float BeamWidth => beamWidth;
        public Color BeamColor => beamColor;
        public float Duration => duration;
        public LaserReflectMode ReflectMode => reflectMode;
        public int ReflectCount => reflectCount;
        public float ReflectDamageMultiplier => reflectDamageMultiplier;
        public float ReflectAngleRange => reflectAngleRange;

        public override string ToJson(bool prettyPrint = true)
        {
            var data = new LaserWeaponDataJson
            {
                // Base
                baseData = base.ToJson(false),
                // Laser Specific
                beamSpeed = this.beamSpeed,
                maxLength = this.maxLength,
                beamWidth = this.beamWidth,
                beamColor = new[] { beamColor.r, beamColor.g, beamColor.b, beamColor.a },
                duration = this.duration,
                // Reflect Settings
                reflectMode = (int)this.reflectMode,
                reflectCount = this.reflectCount,
                reflectDamageMultiplier = this.reflectDamageMultiplier,
                reflectAngleRange = this.reflectAngleRange
            };

            return JsonUtility.ToJson(data, prettyPrint);
        }

        public override void FromJson(string json)
        {
            var data = JsonUtility.FromJson<LaserWeaponDataJson>(json);

            // Base - baseData가 있으면 중첩 구조, 없으면 평면 구조
            if (!string.IsNullOrEmpty(data.baseData))
            {
                base.FromJson(data.baseData);
            }
            else
            {
                base.FromJson(json);  // 평면 JSON인 경우 전체 JSON을 base에 전달
            }

            // Laser Specific (값이 0이 아닌 경우만 적용 - 평면 JSON 호환)
            if (data.beamSpeed > 0) this.beamSpeed = data.beamSpeed;
            if (data.maxLength > 0) this.maxLength = data.maxLength;
            if (data.beamWidth > 0) this.beamWidth = data.beamWidth;
            if (data.beamColor != null && data.beamColor.Length == 4)
            {
                this.beamColor = new Color(data.beamColor[0], data.beamColor[1], data.beamColor[2], data.beamColor[3]);
            }
            if (data.duration > 0) this.duration = data.duration;

            // Reflect Settings (기본값 0이 유효하므로 항상 적용)
            this.reflectMode = (LaserReflectMode)data.reflectMode;
            this.reflectCount = data.reflectCount;
            if (data.reflectDamageMultiplier > 0) this.reflectDamageMultiplier = data.reflectDamageMultiplier;
            if (data.reflectAngleRange >= 0) this.reflectAngleRange = data.reflectAngleRange;
        }

        [System.Serializable]
        private class LaserWeaponDataJson
        {
            public string baseData;
            public float beamSpeed;
            public float maxLength;
            public float beamWidth;
            public float[] beamColor;
            public float duration;
            public int reflectMode;
            public int reflectCount;
            public float reflectDamageMultiplier;
            public float reflectAngleRange;
        }
    }
}
