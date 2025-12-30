using UnityEngine;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 무기 스탯(프로토타입용). 나중에 ScriptableObject로 분리하거나 업그레이드 시스템과 연결.
    /// </summary>
    [System.Serializable]
    public sealed class WeaponStats
    {
        [Min(0.01f)] public float FireRate = 8f;        // shots per second
        [Min(0.01f)] public float Damage = 1f;
        [Min(0.1f)] public float BulletSpeed = 14f;
        [Min(0.1f)] public float BulletLifeTime = 2.5f;
        [Min(0.01f)] public float BulletRadius = 0.07f; // collider radius
        [Min(0.1f)] public float BulletVisualScale = 0.14f;
    }
}
