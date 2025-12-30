using UnityEngine;

namespace Script.SystemCore.Resource
{
    [CreateAssetMenu(fileName = "PoolPreset", menuName = "SystemCore/Pool Config Preset", order = 101)]
    public class PoolConfigPreset : ScriptableObject
    {
        [Tooltip("프리셋 설명")]
        [TextArea(2, 4)]
        public string description;

        public PoolConfig config;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (string.IsNullOrEmpty(description))
            {
                description = $"Pool preset: {name}";
            }
        }
    }
}
