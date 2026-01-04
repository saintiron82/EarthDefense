using System.Collections.Generic;
using UnityEngine;
using Script.SystemCore.Pool;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 원형 섹터 방식으로 청크(정크)를 스폰하는 시스템.
    /// 
    /// [TODO] 향후 확장 계획:
    /// - Weapon 인터페이스로 추상화 (IWeaponSpawner, IProjectileSpawner 등)
    /// - 다양한 발사 패턴 지원 (직선, 나선형, 랜덤 산개 등)
    /// - 투사체 타입 확장 (미사일, 레이저, 드론, 실드 등)
    /// - 무기별 특수 효과 (슬로우, 빙결, 독, 폭발 등)
    /// - ScriptableObject 기반 무기 데이터
    /// </summary>
    public sealed class Sector : MonoBehaviour
    {
        [Header("Refs")]
        [HideInInspector] [SerializeField] private Transform center;
        [HideInInspector] [SerializeField] private PlayerCore player;

        [Header("Pooling")]
        [Tooltip("PoolService로 청크(정크)를 스폰할 때 사용할 프리팹 ID. 비워두면 Instantiate(prefab)를 사용합니다.")]
        [SerializeField] private string chunkPoolId;

        [Header("Layout")]
        [HideInInspector] [SerializeField, Min(1)] private int sectorIndex = 0;
        [HideInInspector] [SerializeField, Min(0.1f)] private float sectorArcDeg = 360f;
        [HideInInspector] [SerializeField] private float sectorStartDeg = 0f;

        [Header("Thickness")]
        [SerializeField] private bool randomThickness = true;
        [SerializeField, Min(0.0001f)] private float thickness = 1.0f;
        [SerializeField, Min(0.0001f)] private float minThickness = 0.6f;
        [SerializeField, Min(0.0001f)] private float maxThickness = 1.6f;

        [Header("Flow")]
        [SerializeField, Min(0f)] private float speed = 0.75f;

        [Header("Hit")]
        [SerializeField, Min(0f)] private float hitR = 1.2f;

        [Header("Spawn")]
        [SerializeField, Min(0f)] private float spawnR = 24.0f;
        [SerializeField] private float minSpawnGap = 0.8f;
        [SerializeField] private float maxSpawnGap = 1.6f;
        [SerializeField, Min(0f)] private float spawnLookahead = 1f;

        private readonly List<ChunkEnemy> _stack = new();
        private ChunkEnemy _last;
        private int _spawnSeq;

        private void Reset()
        {
            if (center == null) center = transform;
        }

        private void Start()
        {
            if (SectorManager.Instance != null)
            {
                SectorManager.Instance.Register(this);
            }
            else
            {
                // 스탠드얼론 사용 시 인스펙터 값으로 동작
                Setup();
            }
        }

        private void OnDestroy()
        {
            SectorManager.Instance?.Unregister(this);
        }

        [ContextMenu("Setup")]
        public void Setup()
        {
            if ( center == null) return;

            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            _stack.Clear();
            _last = null;

            Spawn();
        }

        private void Update()
        {
            if (center == null) return;

            var dt = Time.deltaTime;

            // 파괴된 것 정리 + 비어있는 청크 제거
            for (int s = _stack.Count - 1; s >= 0; s--)
            {
                var chunk = _stack[s];
                if (chunk == null)
                {
                    _stack.RemoveAt(s);
                    continue;
                }

                var mask = chunk.GetComponent<RingSectorDamageMask>();
                if (mask != null && !mask.HasIntactCell())
                {
                    _stack.RemoveAt(s);
                }
            }

            if (_last == null)
            {
                if (_stack.Count > 0) _last = _stack[_stack.Count - 1];
            }

            if (_last == null)
            {
                Spawn();
                return;
            }

            // 조건: 마지막 꼬리(Outer)가 spawnR 안쪽으로 들어오면 스폰
            var tail = _last.OuterRadius;
            var tailPredicted = tail - (speed * dt * spawnLookahead);
            if (tailPredicted < spawnR)
            {
                // 규칙: 새 청크 머리(Inner) = 마지막 꼬리 + gap
                var gap = Random.Range(minSpawnGap, maxSpawnGap);
                Spawn(tail + gap);
            }
        }

        private float NextThickness()
        {
            if (!randomThickness) return thickness;

            var minT = Mathf.Max(0.0001f, minThickness);
            var maxT = Mathf.Max(minT, maxThickness);
            return Random.Range(minT, maxT);
        }

        // headR: 새 청크 머리(InnerRadius)
        // [TODO] 향후 무기 시스템 확장 시:
        // - SpawnProjectile(WeaponData data, Vector3 position, Vector3 direction)
        // - 다양한 투사체 타입을 IProjectile 인터페이스로 통합
        private void Spawn(float? headOverride = null)
        {
            var arc = sectorArcDeg;
            var thetaCenterDeg = sectorStartDeg + arc * 0.5f;

            var t = NextThickness();
            ChunkEnemy enemy = default;
            
            // 풀링 시스템 사용 (이미 최적화됨)
            if (!string.IsNullOrWhiteSpace(chunkPoolId) && PoolService.Instance != null)
            {
                enemy = PoolService.Instance.Get<ChunkEnemy>(chunkPoolId, center.position, Quaternion.identity);
                if (enemy != null) enemy.transform.SetParent(transform);
            }

            if (enemy == null) return;

            // 고유 이름 부여: 슬롯 정보와 시퀀스 번호 포함
            enemy.gameObject.name = $"ChunkEnemy_s{sectorIndex}_#{++_spawnSeq}";
 
            // 청크 설정 (향후 IProjectile.Initialize()로 통합 가능)
            var headR = headOverride ?? spawnR;
            enemy.Configure(center, thetaCenterDeg, arc, Mathf.Max(0f, headR), t, speed, hitR, player,
                Color.HSVToRGB(Random.value, 0.85f, 0.85f), sectorIndex);

            _stack.Add(enemy);
            _last = enemy;
        }
 
        /// <summary>
        /// 청크가 파괴/리턴될 때 스택에서 제거.
        /// </summary>
        public void UnregisterChunk(ChunkEnemy chunk)
        {
            if (chunk == null) return;
            _stack.Remove(chunk);
            if (_last == chunk)
            {
                _last = _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
            }
        }

        /// <summary>
        /// 이 섹터(단일 슬롯)의 첨두 청크를 반환. 앞에서부터 intact 셀을 가진 첫 청크만 반환.
        /// </summary>
        public bool TryGetFrontChunk(out ChunkEnemy chunk, out RingSectorDamageMask mask)
        {
            chunk = null;
            mask = null;
            if (center == null) return false;
            if (_stack.Count == 0) return false;

            for (int i = 0; i < _stack.Count; i++)
            {
                var c = _stack[i];
                if (c == null) continue;
                var m = c.DamageMask;
                if (m == null) continue;
                if (!m.HasIntactCell()) continue;

                chunk = c;
                mask = m;
                return true;
            }

            return false;
        }

        public int GetChunksInStackOrder(List<RingSectorDamageMask> masks)
        {
            if (masks == null) return 0;
            masks.Clear();

            for (int i = 0; i < _stack.Count; i++)
            {
                var chunk = _stack[i];
                if (chunk == null) continue;
                var mask = chunk.DamageMask;
                if (mask == null) continue;
                if (!mask.HasIntactCell()) continue;
                masks.Add(mask);
            }

            return masks.Count;
        }

        public int SectorIndex => sectorIndex;
        public float SectorArcDeg => sectorArcDeg;
        public float SectorStartDeg => sectorStartDeg;

        /// <summary>
        /// 매니저에서 섹터 정보를 주입할 때 사용
        /// </summary>
        public void Initialize(Transform centerTransform, PlayerCore playerCore, int index, float startDeg, float arcDeg)
        {
            center = centerTransform;
            player = playerCore;
            sectorIndex = index;
            sectorStartDeg = startDeg;
            sectorArcDeg = arcDeg;
            Setup();
        }
     }
 }
