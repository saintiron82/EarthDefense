using System.Collections.Generic;
using UnityEngine;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 콜라이더/물리 이벤트 없이 수학 기반 판정을 하기 위해,
    /// 씬에 존재하는 데미지 대상들을 등록/조회하는 간단한 레지스트리.
    /// 
    /// - Chunks: RingSectorDamageMask (셀 기반 솔리드/구멍 판정)
    /// - Health: 단일 HP 대상
    /// 
    /// 주의: 활성 오브젝트의 OnEnable/OnDisable에서 등록/해제합니다.
    /// </summary>
    public static class DamageableRegistry
    {
        private static readonly List<RingSectorDamageMask> SChunks = new();
        private static readonly List<Health> SHealths = new();
        
        // 공간 분할 그리드 (셀 크기 = 5.0f)
        private static readonly SpatialGrid<RingSectorDamageMask> SChunkGrid = new(5f);
        private static readonly SpatialGrid<Health> SHealthGrid = new(5f);
        
        private static int _pruneFrameCounter;
        private const int PruneInterval = 30; // 30프레임마다 1번만 정리

        public static IReadOnlyList<RingSectorDamageMask> Chunks => SChunks;
        public static IReadOnlyList<Health> Healths => SHealths;

        public static void Register(RingSectorDamageMask mask)
        {
            if (mask == null) return;
            if (!SChunks.Contains(mask))
            {
                SChunks.Add(mask);
                
                // 청크의 실제 메시 위치 계산 (피봇이 원점이지만 메시는 멀리 있음)
                // 메시 중심 = 피봇 + (InnerRadius + Thickness/2) 방향
                // 하지만 원형 메시이므로 모든 방향에 등록 필요
                // 임시 해결: 피봇 위치 사용하되, 나중에 개선
                SChunkGrid.Add(mask, mask.transform.position);
            }
        }

        public static void Unregister(RingSectorDamageMask mask)
        {
            if (mask == null) return;
            SChunks.Remove(mask);
            SChunkGrid.Remove(mask);
        }

        public static void Register(Health health)
        {
            if (health == null) return;
            if (!SHealths.Contains(health))
            {
                SHealths.Add(health);
                SHealthGrid.Add(health, health.transform.position);
            }
        }

        public static void Unregister(Health health)
        {
            if (health == null) return;
            SHealths.Remove(health);
            SHealthGrid.Remove(health);
        }

        /// <summary>
        /// 청크 위치 업데이트 (이동하는 청크용)
        /// </summary>
        public static void UpdateChunkPosition(RingSectorDamageMask mask, Vector3 newPosition)
        {
            if (mask == null) return;
            SChunkGrid.UpdatePosition(mask, newPosition);
        }

        /// <summary>
        /// Health 위치 업데이트 (이동하는 적용)
        /// </summary>
        public static void UpdateHealthPosition(Health health, Vector3 newPosition)
        {
            if (health == null) return;
            SHealthGrid.UpdatePosition(health, newPosition);
        }

        /// <summary>
        /// 파괴된 레퍼런스(null)들을 정리합니다.
        /// 비용 최소화를 위해 필요할 때만 호출하세요.
        /// </summary>
        public static void Prune()
        {
            SChunks.RemoveAll(m => m == null);
            SHealths.RemoveAll(h => h == null);
            SChunkGrid.Prune();
            SHealthGrid.Prune();
        }
        
        /// <summary>
        /// 주기적으로만 Prune을 수행합니다. (30프레임마다 1회)
        /// </summary>
        public static void PruneIfNeeded()
        {
            if (++_pruneFrameCounter >= PruneInterval)
            {
                _pruneFrameCounter = 0;
                Prune();
            }
        }

        // ===== 공간 분할 쿼리 API =====

        /// <summary>
        /// 선분(from→to)과 교차 가능성이 있는 청크들을 반환 (공간 최적화)
        /// </summary>
        public static void QueryChunksInLine(Vector2 from, Vector2 to, List<RingSectorDamageMask> results)
        {
            SChunkGrid.QueryLine(from, to, results);
        }

        /// <summary>
        /// 선분(from→to)과 교차 가능성이 있는 Health들을 반환 (공간 최적화)
        /// </summary>
        public static void QueryHealthsInLine(Vector2 from, Vector2 to, List<Health> results)
        {
            SHealthGrid.QueryLine(from, to, results);
        }

        /// <summary>
        /// 특정 반경 내의 청크들을 반환
        /// </summary>
        public static void QueryChunksInRadius(Vector2 center, float radius, List<RingSectorDamageMask> results)
        {
            SChunkGrid.QueryRadius(center, radius, results);
        }

        /// <summary>
        /// 특정 반경 내의 Health들을 반환
        /// </summary>
        public static void QueryHealthsInRadius(Vector2 center, float radius, List<Health> results)
        {
            SHealthGrid.QueryRadius(center, radius, results);
        }
    }
}
