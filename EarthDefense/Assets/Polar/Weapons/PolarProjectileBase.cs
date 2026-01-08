using UnityEngine;
using Script.SystemCore.Pool;

namespace Polar.Weapons
{
    /// <summary>
    /// Polar 투사체 공통 추상 클래스 (하이브리드 아키텍처 + SGSystem 통합)
    /// - 메인 클래스(PolarWeapon)는 공통 제어 (발사 타이밍, 풀링, 리소스)
    /// - 개별 투사체는 독립 로직 (이동, 충돌, 피해 적용)
    /// - SGSystem PoolService와 완전 통합 (IPoolable 구현)
    /// </summary>
    public abstract class PolarProjectileBase : MonoBehaviour, IPoolable
    {
        protected IPolarField _field;
        protected PolarWeaponData _weaponData;
        protected bool _isActive;

        /// <summary>
        /// 투사체 활성 상태
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// 무기 데이터
        /// </summary>
        public PolarWeaponData WeaponData => _weaponData;

        /// <summary>
        /// IPoolable: 풀 번들 ID (PoolService에서 자동 주입)
        /// </summary>
        public string PoolBundleId { get; set; }

        /// <summary>
        /// IPoolable: 풀에서 꺼낼 때 자동 호출
        /// </summary>
        public void OnSpawnFromPool()
        {
            _isActive = false;  // Launch에서 true로 설정
            OnPoolSpawn();
        }

        /// <summary>
        /// IPoolable: 풀에 반환할 때 자동 호출
        /// </summary>
        public void OnReturnToPool()
        {
            _isActive = false;
            _field = null;
            _weaponData = null;
            OnPoolReturn();
        }

        /// <summary>
        /// 하위 클래스 전용: 풀 스폰 시 추가 초기화
        /// </summary>
        protected virtual void OnPoolSpawn() { }

        /// <summary>
        /// 하위 클래스 전용: 풀 반환 시 추가 정리
        /// </summary>
        protected virtual void OnPoolReturn() { }

        /// <summary>
        /// 투사체 발사 (개별 구현 필요)
        /// </summary>
        public abstract void Launch(IPolarField field, PolarWeaponData weaponData);

        /// <summary>
        /// 투사체 비활성화
        /// </summary>
        public virtual void Deactivate()
        {
            _isActive = false;
        }

        /// <summary>
        /// 풀 반환 (SGSystem PoolService 사용)
        /// </summary>
        public virtual void ReturnToPool()
        {
            Deactivate();
            
            // PoolService에 반환
            if (PoolService.Instance != null && !string.IsNullOrEmpty(PoolBundleId))
            {
                PoolService.Instance.Return(this);
            }
            else
            {
                // Fallback: PoolService 없으면 직접 파괴
                Debug.LogWarning($"[PolarProjectileBase] PoolService not available. Destroying {gameObject.name}");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 매 프레임 업데이트 (개별 구현 필요)
        /// </summary>
        protected virtual void Update()
        {
            if (!_isActive || _field == null) return;
            OnUpdate(Time.deltaTime);
        }

        /// <summary>
        /// 개별 투사체 업데이트 로직
        /// </summary>
        protected abstract void OnUpdate(float deltaTime);
    }
}
