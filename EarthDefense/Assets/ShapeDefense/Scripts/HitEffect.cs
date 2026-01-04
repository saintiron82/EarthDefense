using UnityEngine;
using Script.SystemCore.Pool;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 히트 순간의 연출(파티클/스프라이트/사운드/카메라 흔들림 등)을 담당.
    /// Bullet/레이저/폭발 등 어떤 공격에서도 동일한 인터페이스로 호출할 수 있게 최소 형태로 둡니다.
    /// </summary>
    public sealed class HitEffect : MonoBehaviour
    {
        [Header("Pool")]
        [Tooltip("PoolService에서 가져올 이펙트 프리팹 ID. (Resource/PoolConfig에 등록된 ID)")]
        [SerializeField] private string hitEffectPoolId;

        [Tooltip("풀에서 가져온 이펙트를 얼마 후 반환할지(초)")]
        [SerializeField, Min(0.01f)] private float returnAfter = 0.25f;

        [Header("Debug")]
        [SerializeField] private bool logWhenNoPool;

        /// <summary>
        /// 히트 순간 호출.
        /// point/dir/source/target 정보를 받아 연출을 결정할 수 있습니다.
        /// </summary>
        public void Trigger(in DamageEvent e, GameObject target)
        {
            // 핵심 정책: HitEffect는 Instantiate/Destroy를 직접 하지 않는다.
            // PoolService(리소스 시스템 연동)에서 이펙트를 스폰하고, 일정 시간 뒤 반환한다.
            if (string.IsNullOrWhiteSpace(hitEffectPoolId)) return;

            var pool = PoolService.Instance;
            if (pool == null)
            {
                if (logWhenNoPool)
                {
                    UnityEngine.Debug.LogWarning("[HitEffect] PoolService.Instance is null. Effect skipped.");
                }
                return;
            }

            var go = pool.Get(hitEffectPoolId, e.Point, Quaternion.identity);
            if (go == null)
            {
                if (logWhenNoPool)
                {
                    UnityEngine.Debug.LogWarning($"[HitEffect] Pool get failed: {hitEffectPoolId}");
                }
                return;
            }

            // 필요하면 방향/타겟 정보도 세팅할 수 있게 훅(간단 인터페이스)
            if (go.TryGetComponent<IHitEffectReceiver>(out var receiver))
            {
                receiver.OnHit(e, target);
            }
        }

    }

    /// <summary>
    /// 풀에서 스폰되는 실제 이펙트 프리팹이 히트 정보를 받고 싶을 때 구현.
    /// (파티클 방향, 텍스처 변경, 데칼, 사운드 등)
    /// </summary>
    public interface IHitEffectReceiver
    {
        void OnHit(in DamageEvent e, GameObject target);
    }
}
