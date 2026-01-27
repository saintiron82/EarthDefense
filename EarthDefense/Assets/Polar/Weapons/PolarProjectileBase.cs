using UnityEngine;
using Script.SystemCore.Pool;
using Polar.Weapons.Effects;
using System.Collections.Generic;

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
        
        // 극좌표 이동 공통 필드
        protected float _angleDeg;
        protected float _radius;
        protected float _speed;
        protected bool _hasReachedWall;
        
        // Effect 발동 추적
        private float _launchTime;
        private float _traveledDistance;
        private Dictionary<PolarEffectBase, int> _effectTriggerCounts = new Dictionary<PolarEffectBase, int>();
        private Dictionary<PolarEffectBase, float> _effectLastTriggerTime = new Dictionary<PolarEffectBase, float>();
        private Dictionary<PolarEffectBase, float> _effectNextIntervalTime = new Dictionary<PolarEffectBase, float>();
        private List<Coroutine> _activeCoroutines = new List<Coroutine>();

        private int _remainingPenetrations;

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
            // 실행 중인 코루틴 정리
            foreach (var coroutine in _activeCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            _activeCoroutines.Clear();
            
            _isActive = false;
            _field = null;
            _weaponData = null;
            _hasReachedWall = false;
            _angleDeg = 0f;
            _radius = 0f;
            _speed = 0f;
            _remainingPenetrations = 0;
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
        /// 극좌표 기반 투사체 발사 (공통 패턴)
        /// </summary>
        protected void LaunchPolar(IPolarField field, PolarWeaponData weaponData, float angleDeg, float startRadius, float speed)
        {
            _field = field;
            _weaponData = weaponData;
            _isActive = true;
            _hasReachedWall = false;
            
            _angleDeg = angleDeg;
            _radius = startRadius;
            _speed = speed;
            
            _launchTime = Time.time;
            _traveledDistance = 0f;
            _effectTriggerCounts.Clear();
            _effectLastTriggerTime.Clear();
            _effectNextIntervalTime.Clear();
            
            // 관통 횟수 초기화
            _remainingPenetrations = weaponData != null ? weaponData.ImpactPolicy.penetrationCount : 0;
            
            UpdatePolarPosition();
            
            // OnLaunch 시점 Effect 발동
            TriggerEffectsByType(EffectTriggerType.OnLaunch);
        }
        
        /// <summary>
        /// 극좌표 위치 업데이트 (공통 로직)
        /// </summary>
        protected void UpdatePolarPosition()
        {
            if (_field == null) return;

            float angleRad = _angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            // IPolarField.CenterPosition 우선 사용 (테스트/런타임 모두 안전)
            Vector3 center = _field.CenterPosition;
            transform.position = center + (Vector3)(dir * _radius);
        }
        
        /// <summary>
        /// 극좌표 이동 및 벽 충돌 체크 (공통 로직)
        /// </summary>
        /// <returns>벽에 충돌했으면 true, 충돌 지점의 섹터 인덱스와 위치를 out으로 반환</returns>
        protected bool UpdatePolarMovementAndCheckWallCollision(float deltaTime, out int hitSectorIndex, out Vector2 hitPosition)
        {
            hitSectorIndex = -1;
            hitPosition = Vector2.zero;

            if (_field == null || _hasReachedWall)
            {
                return false;
            }

            // 외부로 이동
            _radius += _speed * deltaTime;
            _traveledDistance += _speed * deltaTime;
            UpdatePolarPosition();

            // 주기적/거리/시간 기반 Effect 체크
            CheckPeriodicEffects();

            // 벽 충돌 체크
            float angleRad = _angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            Vector2 currentPos = (Vector2)_field.CenterPosition + dir * _radius;

            int sectorIndex = _field.AngleToSectorIndex(_angleDeg);
            float sectorRadius = _field.GetSectorRadius(sectorIndex);

            if (_radius >= sectorRadius - 0.1f)
            {
                _hasReachedWall = true;
                hitSectorIndex = sectorIndex;
                hitPosition = currentPos;

                HandleWallImpact(hitSectorIndex, hitPosition);
                return true;
            }

            return false;
        }

        private void HandleWallImpact(int sectorIndex, Vector2 hitPosition)
        {
            // 1) Effect
            TriggerEffectsByType(EffectTriggerType.OnImpact, sectorIndex, hitPosition);

            // 2) 정책 기반 데미지
            if (_weaponData == null || _field == null) return;

            var policy = _weaponData.ImpactPolicy;
            bool shouldApplyDamage = policy.hitResponse == ProjectileHitResponse.StopAndApplyDamage ||
                                     policy.hitResponse == ProjectileHitResponse.PenetrateAndDamage;

            if (shouldApplyDamage)
            {
                ApplyImpactDamageToSector(sectorIndex, _weaponData);
            }

            // 3) 관통/소멸 결정
            bool isPenetrating = policy.hitResponse == ProjectileHitResponse.PenetrateAndDamage ||
                                 policy.hitResponse == ProjectileHitResponse.PenetrateNoDamage;

            if (!isPenetrating)
            {
                // Stop* 계열: 그대로 소멸(상위에서 ReturnToPool 처리)
                return;
            }

            // 관통 가능 횟수 처리
            if (policy.penetrationCount == 0)
            {
                return; // 관통 아님
            }

            if (policy.penetrationCount > 0)
            {
                _remainingPenetrations--;
                if (_remainingPenetrations < 0)
                {
                    return;
                }
            }

            // 관통이면 "벽에 닿았음" 상태를 해제하고 계속 진행 가능하게 만든다.
            _hasReachedWall = false;
            _radius = Mathf.Min(_radius, _field.GetSectorRadius(sectorIndex) - 0.15f);
        }

        protected virtual void ApplyImpactDamageToSector(int sectorIndex, PolarWeaponData weaponData)
        {
            // 최소 구현: 섹터 단일 타격
            var combat = PolarCombatProperties.FromWeaponData(weaponData);
            _field.SetLastWeaponKnockback(combat.KnockbackPower);
            _field.ApplyDamageToSector(sectorIndex, combat.Damage);

            if (_field.EnableWoundSystem)
            {
                _field.ApplyWound(sectorIndex, combat.WoundIntensity);
            }
        }

        /// <summary>
        /// 충돌 시 추가 효과 발동 (하위 호환)
        /// </summary>
        protected void TriggerImpactEffects(int sectorIndex, Vector2 position)
        {
            TriggerEffectsByType(EffectTriggerType.OnImpact, sectorIndex, position);
        }
        
        /// <summary>
        /// 특정 시점의 Effect 발동
        /// </summary>
        protected void TriggerEffectsByType(EffectTriggerType triggerType, int sectorIndex = -1, Vector2 position = default)
        {
            if (_weaponData == null || _weaponData.ImpactEffects == null || _weaponData.ImpactEffects.Length == 0)
            {
                return;
            }
            
            foreach (var effectObj in _weaponData.ImpactEffects)
            {
                // null 체크
                if (effectObj == null) continue;
                
                if (effectObj is PolarEffectBase effect)
                {
                    // 트리거 타입 확인
                    if (effect.TriggerCondition.triggerType != triggerType)
                    {
                        continue;
                    }
                    
                    // 발동 가능 여부 체크
                    if (!CanTriggerEffect(effect))
                    {
                        continue;
                    }
                    
                    try
                    {
                        // 지연 발동
                        if (effect.TriggerCondition.delay > 0f)
                        {
                            var coroutine = StartCoroutine(TriggerEffectDelayed(effect, sectorIndex, position, effect.TriggerCondition.delay));
                            _activeCoroutines.Add(coroutine);
                        }
                        else
                        {
                            // 즉시 발동
                            effect.OnImpact(_field, sectorIndex != -1 ? sectorIndex : GetCurrentSectorIndex(), 
                                position != default ? position : GetCurrentPosition(), _weaponData);
                            
                            // 트리거 기록
                            RecordEffectTrigger(effect);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[Effect] Failed to trigger {effect.EffectName}: {e.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Effect 발동 가능 여부 확인
        /// </summary>
        private bool CanTriggerEffect(PolarEffectBase effect)
        {
            var condition = effect.TriggerCondition;
            
            // 최대 발동 횟수 체크
            if (condition.maxTriggerCount > 0)
            {
                if (_effectTriggerCounts.TryGetValue(effect, out int count) && count >= condition.maxTriggerCount)
                {
                    return false;
                }
            }
            
            // 쿨다운 체크
            if (condition.cooldown > 0f)
            {
                if (_effectLastTriggerTime.TryGetValue(effect, out float lastTime))
                {
                    if (Time.time - lastTime < condition.cooldown)
                    {
                        return false;
                    }
                }
            }
            
            // 확률 체크
            if (!condition.CanTrigger())
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Effect 발동 기록
        /// </summary>
        private void RecordEffectTrigger(PolarEffectBase effect)
        {
            // 발동 횟수 증가
            if (!_effectTriggerCounts.ContainsKey(effect))
            {
                _effectTriggerCounts[effect] = 0;
            }
            _effectTriggerCounts[effect]++;
            
            // 마지막 발동 시간 기록
            _effectLastTriggerTime[effect] = Time.time;
        }
        
        /// <summary>
        /// 주기적 Effect 체크 (OnInterval, OnDistance, OnTime)
        /// </summary>
        private void CheckPeriodicEffects()
        {
            if (_weaponData == null || _weaponData.ImpactEffects == null)
            {
                return;
            }

            foreach (var effectObj in _weaponData.ImpactEffects)
            {
                if (effectObj is PolarEffectBase effect)
                {
                    var condition = effect.TriggerCondition;

                    if (condition.triggerType == EffectTriggerType.OnInterval)
                    {
                        if (!_effectNextIntervalTime.ContainsKey(effect))
                        {
                            _effectNextIntervalTime[effect] = Time.time + condition.interval;
                        }

                        if (Time.time >= _effectNextIntervalTime[effect])
                        {
                            TriggerEffectsByType(EffectTriggerType.OnInterval);
                            _effectNextIntervalTime[effect] = Time.time + condition.interval;
                        }
                    }
                    else if (condition.triggerType == EffectTriggerType.OnDistance)
                    {
                        float steps = Mathf.Floor(_traveledDistance / condition.distanceStep);
                        int expectedTriggers = Mathf.FloorToInt(steps);
                        int currentTriggers = _effectTriggerCounts.ContainsKey(effect) ? _effectTriggerCounts[effect] : 0;

                        if (currentTriggers < expectedTriggers)
                        {
                            TriggerEffectsByType(EffectTriggerType.OnDistance);
                        }
                    }
                    else if (condition.triggerType == EffectTriggerType.OnTime)
                    {
                        float elapsedTime = Time.time - _launchTime;

                        if (elapsedTime >= condition.triggerTime)
                        {
                            if (!_effectTriggerCounts.ContainsKey(effect) || _effectTriggerCounts[effect] == 0)
                            {
                                TriggerEffectsByType(EffectTriggerType.OnTime);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 지연 발동 코루틴
        /// </summary>
        private System.Collections.IEnumerator TriggerEffectDelayed(PolarEffectBase effect, int sectorIndex, Vector2 position, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (_isActive && effect != null)
            {
                effect.OnImpact(_field, sectorIndex != -1 ? sectorIndex : GetCurrentSectorIndex(), 
                    position != default ? position : GetCurrentPosition(), _weaponData);
                RecordEffectTrigger(effect);
            }
        }
        
        /// <summary>
        /// 현재 위치의 섹터 인덱스 조회
        /// </summary>
        protected int GetCurrentSectorIndex()
        {
            if (_field == null) return -1;
            return _field.AngleToSectorIndex(_angleDeg);
        }
        
        /// <summary>
        /// 현재 위치 (월드 좌표)
        /// </summary>
        protected Vector2 GetCurrentPosition()
        {
            if (_field == null) return Vector2.zero;

            float angleRad = _angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            Vector2 center = _field.CenterPosition;

            return center + dir * _radius;
        }

        /// <summary>
        /// 투사체 발사 (개별 구현 필요)
        /// </summary>
        public abstract void Launch(IPolarField field, PolarWeaponData weaponData);

        /// <summary>
        /// 투사체 비활성화
        /// </summary>
        public virtual void Deactivate()
        {
            if (!_isActive) return;
            
            // OnDestroy Effect 발동
            TriggerEffectsByType(EffectTriggerType.OnDestroy);
            
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
