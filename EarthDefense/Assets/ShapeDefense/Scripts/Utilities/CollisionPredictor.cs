using UnityEngine;
using System.Collections.Generic;

namespace ShapeDefense.Scripts.Utilities
{
    /// <summary>
    /// 수학적 충돌 예측 시스템
    /// 레이저 빔이 PolarField 청크와 외부 적들을 타겟팅하는 시스템
    /// PolarField 자체가 주요 적 타겟
    /// </summary>
    public static class CollisionPredictor
    {
        /// <summary>
        /// 충돌 예측 결과
        /// </summary>
        public struct CollisionResult
        {
            public Vector2 hitPoint;
            public Vector2 normal;
            public Collider2D collider;
            public float distance;
            public CollisionType type;
            public GameObject target;

            public CollisionResult(Vector2 point, Vector2 normal, Collider2D col, float dist, CollisionType type, GameObject target = null)
            {
                this.hitPoint = point;
                this.normal = normal;
                this.collider = col;
                this.distance = dist;
                this.type = type;
                this.target = target;
            }
        }

        public enum CollisionType
        {
            None,
            PolarField,     // 극장 경계
            Enemy,          // 적 오브젝트
            Obstacle,       // 일반 장애물
            Projectile      // 발사체
        }

        /// <summary>
        /// 주어진 위치와 방향으로 충돌 예측 수행 (PolarField 청크 + 외부 적들 타겟팅)
        /// 빔의 넓이를 고려한 영역 충돌 검출
        /// </summary>
        /// <param name="origin">시작점</param>
        /// <param name="direction">방향</param>
        /// <param name="beamWidth">빔의 넓이 (충돌 검출 범위)</param>
        /// <param name="maxDistance">최대 거리</param>
        /// <param name="layerMask">충돌 검출할 레이어</param>
        /// <param name="predictMovement">동적 타겟 예측 여부</param>
        /// <returns>충돌 결과 리스트 (거리순 정렬)</returns>
        public static List<CollisionResult> PredictCollisions(Vector2 origin, Vector2 direction, float beamWidth = 0.1f, float maxDistance = 100f, LayerMask layerMask = default, bool predictMovement = true)
        {
            var results = new List<CollisionResult>();
            direction = direction.normalized;

            // 1. PolarField 청크 충돌 계산 (주요 타겟 - 빔 넓이 고려)
            var polarFieldCollisions = PredictPolarFieldChunkCollisionWithWidth(origin, direction, beamWidth, maxDistance);
            results.AddRange(polarFieldCollisions);

            // 2. Physics2D.CapsuleCastAll로 빔 넓이를 고려한 충돌 검출
            var beamRadius = beamWidth * 0.5f;
            var endPoint = origin + direction * maxDistance;

            // 캡슐 형태로 빔 전체 경로 검출
            var hits = Physics2D.CapsuleCastAll(
                origin,
                Vector2.one * beamWidth, // 캡슐 크기
                CapsuleDirection2D.Horizontal,
                0f, // 회전
                direction,
                maxDistance,
                layerMask
            );

            foreach (var hit in hits)
            {
                var collisionType = DetermineCollisionType(hit.collider);
                var result = new CollisionResult(
                    hit.point,
                    hit.normal,
                    hit.collider,
                    hit.distance,
                    collisionType,
                    hit.collider.gameObject
                );

                results.Add(result);
            }

            // 3. 동적 타겟 예측 (외부에서 접근하는 적들, 빔 넓이 고려)
            if (predictMovement)
            {
                var dynamicCollisions = PredictDynamicTargets(origin, direction, beamWidth, maxDistance, layerMask);
                results.AddRange(dynamicCollisions);
            }

            // 거리순 정렬
            results.Sort((a, b) => a.distance.CompareTo(b.distance));

            return results;
        }

        /// <summary>
        /// 가장 가까운 충돌점만 반환 (빔 넓이 고려)
        /// </summary>
        public static CollisionResult? PredictFirstCollision(Vector2 origin, Vector2 direction, float beamWidth = 0.1f, float maxDistance = 100f, LayerMask layerMask = default, bool predictMovement = true)
        {
            var collisions = PredictCollisions(origin, direction, beamWidth, maxDistance, layerMask, predictMovement);
            return collisions.Count > 0 ? collisions[0] : (CollisionResult?)null;
        }

        /// <summary>
        /// 동적 타겟들의 미래 위치 예측 (외부에서 접근하는 적들, 빔 넓이 고려)
        /// </summary>
        private static List<CollisionResult> PredictDynamicTargets(Vector2 origin, Vector2 direction, float beamWidth, float maxDistance, LayerMask layerMask)
        {
            var results = new List<CollisionResult>();

            // 이동 중인 적 오브젝트들을 찾기
            var movingTargets = FindMovingTargets(origin, maxDistance, layerMask);

            foreach (var target in movingTargets)
            {
                var prediction = PredictMovingTargetCollision(origin, direction, target);
                if (prediction.HasValue)
                {
                    results.Add(prediction.Value);
                }
            }

            return results;
        }

        /// <summary>
        /// 이동 중인 타겟과의 충돌 예측 (선형 예측)
        /// </summary>
        private static CollisionResult? PredictMovingTargetCollision(Vector2 origin, Vector2 direction, MovingTarget target)
        {
            // 타겟의 현재 위치와 속도
            Vector2 targetPos = target.position;
            Vector2 targetVel = target.velocity;

            // 상대 속도 계산 (타겟을 고정점으로 보는 관점)
            // 레이저는 즉시 발사되므로 속도는 무한대로 근사

            // 선형 방정식으로 교점 계산
            // 레이저: P_laser(t) = origin + direction * speed * t
            // 타겟: P_target(t) = targetPos + targetVel * t

            // 충돌 조건: |P_laser(t) - P_target(t)| <= target.radius

            float a = direction.sqrMagnitude;
            float b = 2f * Vector2.Dot(origin - targetPos, direction) - 2f * Vector2.Dot(targetVel, direction);
            float c = (origin - targetPos).sqrMagnitude - target.radius * target.radius;

            // 이차방정식의 해
            float discriminant = b * b - 4f * a * c;

            if (discriminant < 0f) return null; // 충돌 없음

            float t1 = (-b - Mathf.Sqrt(discriminant)) / (2f * a);
            float t2 = (-b + Mathf.Sqrt(discriminant)) / (2f * a);

            // 가장 가까운 미래 시점 선택
            float t = (t1 > 0f) ? t1 : t2;
            if (t <= 0f) return null; // 과거 충돌은 무시

            // 충돌 지점 계산
            Vector2 futureTargetPos = targetPos + targetVel * t;
            Vector2 laserHitPoint = origin + direction * Vector2.Distance(origin, futureTargetPos);

            float distance = Vector2.Distance(origin, laserHitPoint);
            Vector2 normal = (laserHitPoint - futureTargetPos).normalized;

            return new CollisionResult(
                laserHitPoint,
                normal,
                target.collider,
                distance,
                DetermineCollisionType(target.collider),
                target.gameObject
            );
        }

        /// <summary>
        /// PolarField 청크와의 정확한 충돌 계산 (수축 방지를 위한 타격 지점)
        /// </summary>
        private static CollisionResult? PredictPolarFieldChunkCollision(Vector2 origin, Vector2 direction, float maxDistance)
        {
            // 레이저 방향에 해당하는 각도 계산
            var angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angleDeg < 0f) angleDeg += 360f;

            // SectorManager를 통해 해당 섹터 찾기
            if (SectorManager.Instance == null) return null;

            if (!SectorManager.Instance.TryGetSector(angleDeg, out var sector) || sector == null)
                return null;

            // 해당 섹터의 가장 앞쪽 청크 찾기 (가장 가까운 적 타겟)
            if (!sector.TryGetFrontChunk(out var frontChunk, out _) || frontChunk == null)
            {
                // 청크가 없으면 해당 섹터에 공격할 대상이 없음
                return null;
            }

            // 청크의 InnerRadius가 레이저 타격 지점 (수축을 막기 위해 밀어낼 지점)
            float targetRadius = frontChunk.InnerRadius;

            // 지구 중심(Vector2.zero)에서 레이저 방향으로 targetRadius만큼 떨어진 지점
            Vector2 hitPoint = Vector2.zero + direction * targetRadius;
            float distance = Vector2.Distance(origin, hitPoint);

            if (distance > maxDistance) return null;

            // 법선 벡터 (충돌 지점에서 중심으로 향하는 방향 - 밀어낼 방향의 반대)
            Vector2 normal = -direction;

            return new CollisionResult(
                hitPoint,
                normal,
                null, // PolarField 청크는 별도 콜라이더가 없을 수 있음
                distance,
                CollisionType.PolarField,
                frontChunk.gameObject // 타격할 청크 오브젝트
            );
        }

        /// <summary>
        /// PolarField 청크와의 빔 넓이 고려 충돌 계산
        /// 빔이 여러 섹터에 걸칠 때 모든 해당 청크들 반환
        /// </summary>
        private static List<CollisionResult> PredictPolarFieldChunkCollisionWithWidth(Vector2 origin, Vector2 direction, float beamWidth, float maxDistance)
        {
            var results = new List<CollisionResult>();

            if (SectorManager.Instance == null) return results;

            // 빔 중심 방향의 각도
            var centerAngleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (centerAngleDeg < 0f) centerAngleDeg += 360f;

            // 빔의 각도 범위 계산 (넓이를 고려한 좌우 각도)
            var beamRadius = beamWidth * 0.5f;
            var angleSpread = Mathf.Rad2Deg * Mathf.Atan2(beamRadius, Vector2.Distance(origin, Vector2.zero));

            var startAngle = centerAngleDeg - angleSpread;
            var endAngle = centerAngleDeg + angleSpread;

            // 각도 범위 정규화
            if (startAngle < 0f) startAngle += 360f;
            if (endAngle >= 360f) endAngle -= 360f;

            // 해당 각도 범위의 모든 섹터에서 충돌 검출
            var checkedSectors = new HashSet<int>();
            var currentAngle = startAngle;

            // 각도 범위를 순회하며 섹터들 확인
            var angleStep = 360f / 64f; // 예시: 64개 섹터 가정
            var steps = Mathf.CeilToInt((endAngle - startAngle) / angleStep) + 1;

            for (int i = 0; i <= steps; i++)
            {
                var checkAngle = startAngle + (i * angleStep);
                if (checkAngle > endAngle) checkAngle = endAngle;

                // 해당 각도의 섹터에서 충돌 확인
                var sectorCollision = PredictPolarFieldChunkCollision(origin,
                    new Vector2(Mathf.Cos(checkAngle * Mathf.Deg2Rad), Mathf.Sin(checkAngle * Mathf.Deg2Rad)),
                    maxDistance);

                if (sectorCollision.HasValue)
                {
                    // 중복 방지를 위해 섹터 인덱스로 체크
                    var sectorHash = sectorCollision.Value.target?.GetInstanceID() ?? 0;
                    if (!checkedSectors.Contains(sectorHash))
                    {
                        checkedSectors.Add(sectorHash);
                        results.Add(sectorCollision.Value);
                    }
                }

                if (checkAngle >= endAngle) break;
            }

            return results;
        }

        /// <summary>
        /// 충돌체 타입 판별
        /// </summary>
        private static CollisionType DetermineCollisionType(Collider2D collider)
        {
            if (collider == null) return CollisionType.None;

            var go = collider.gameObject;

            // 태그나 레이어로 판별
            if (go.CompareTag("Enemy")) return CollisionType.Enemy;
            if (go.CompareTag("Projectile")) return CollisionType.Projectile;
            if (go.layer == LayerMask.NameToLayer("PolarField")) return CollisionType.PolarField;

            return CollisionType.Obstacle;
        }

        /// <summary>
        /// 주변의 이동 중인 타겟들 찾기
        /// </summary>
        private static List<MovingTarget> FindMovingTargets(Vector2 origin, float radius, LayerMask layerMask)
        {
            var targets = new List<MovingTarget>();
            var colliders = Physics2D.OverlapCircleAll(origin, radius, layerMask);

            foreach (var col in colliders)
            {
                var rb = col.GetComponent<Rigidbody2D>();
                if (rb != null && !rb.isKinematic && rb.linearVelocity.sqrMagnitude > 0.01f)
                {
                    targets.Add(new MovingTarget
                    {
                        position = rb.position,
                        velocity = rb.linearVelocity,
                        radius = GetTargetRadius(col),
                        collider = col,
                        gameObject = col.gameObject
                    });
                }
            }

            return targets;
        }

        /// <summary>
        /// 타겟의 충돌 반지름 계산
        /// </summary>
        private static float GetTargetRadius(Collider2D collider)
        {
            if (collider is CircleCollider2D circle)
                return circle.radius * Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);

            if (collider is BoxCollider2D box)
                return Mathf.Max(box.size.x, box.size.y) * 0.5f * Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);

            // 기본값
            return 0.5f;
        }


        /// <summary>
        /// 이동 중인 타겟 정보
        /// </summary>
        private struct MovingTarget
        {
            public Vector2 position;
            public Vector2 velocity;
            public float radius;
            public Collider2D collider;
            public GameObject gameObject;
        }
    }
}