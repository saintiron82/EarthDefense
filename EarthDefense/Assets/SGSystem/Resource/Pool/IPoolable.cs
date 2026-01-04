namespace Script.SystemCore.Pool
{
    public interface IPoolable
    {
        /// <summary>
        /// 풀에서 생성 시 자동 주입되는 번들 ID. Return 시 사용.
        /// </summary>
        string PoolBundleId { get; set; }
        
        void OnSpawnFromPool();
        void OnReturnToPool();
    }
}