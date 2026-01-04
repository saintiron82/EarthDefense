using System.Collections.Generic;
using UnityEngine;
using ShapeDefense.Scripts.Weapons;

namespace ShapeDefense.Scripts
{
    public sealed class Bullet : BaseProjectile
    {
        // BaseProjectile에서 상속받음:
        // - speed, damage, lifeTime, hitRadius (프리팹 스펙)
        // - maxHits, rehitCooldown (히트 동작)
        // - _spawnTime, _source, _sourceTeamKey, _direction (런타임)
        
        [Header("Bullet Movement")]
        [Tooltip("한 프레임 이동 구간(prev->next)을 몇 번 샘플링할지. 값이 클수록 '구멍 관통/뒤 정크 히트'가 정확해지지만 비용 증가.")]
        [SerializeField, Range(1, 64)] private int sweepSteps = 12;

        [Tooltip("스윕 샘플링 시 소량의 안전 여유(월드 거리). 0이면 샘플 포인트만 검사.")]
        [SerializeField, Min(0f)] private float sweepEpsilon;

        [Header("Hit Effects")]
        [Tooltip("히트 순간 호출할 연출 컴포넌트(선택). Bullet 프리팹에 붙이거나, 외부에서 주입해도 됩니다.")]
        [SerializeField] private HitEffect hitEffect;

        [Header("Visual")]
        [Tooltip("스프라이트가 셋업되지 않은 경우 자동으로 붙일 간단한 스프라이트 렌더러 설정")]
        [SerializeField] private bool autoEnsureSpriteRenderer = true;
        [SerializeField] private Color spriteColor = Color.white;

        [Header("Debug")]
        [SerializeField] private bool debugLogHits = true;
        
        [Header("Runtime Info (Read-only)")] 
        [SerializeField, Tooltip("남은 히트 횟수")] 
        private int debugHitsLeft;

        private int _hitsLeft;
        private SpriteRenderer _sr;
        private static Sprite _defaultSprite;

        /// <summary>
        /// 발사 시 Bullet 초기화
        /// </summary>
        protected override void OnFired()
        {
            _hitsLeft = MaxHits;
            ResetHitState();
            debugHitsLeft = _hitsLeft;
        }

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            _hitsLeft = MaxHits;
        }

        public override void OnReturnToPool()
        {
            base.OnReturnToPool();
            _hitsLeft = 0;
        }

        protected override void UpdateProjectile()
        {
            var curr = (Vector2)transform.position;
            var next = curr + _direction * (Speed * Time.deltaTime);

            if (_hitsLeft > 0 && TrySweepHit(curr, next, _direction, sweepSteps, sweepEpsilon, hitRadius, out var hit))
            {
                if (CanHit(hit))
                {
                    _hitsLeft = Mathf.Max(0, _hitsLeft - 1);
                    debugHitsLeft = _hitsLeft;
                    ApplyHit(hit, _direction, Damage, hitEffect, debugLogHits);
                    if (_hitsLeft <= 0)
                    {
                        ReturnToPool();
                    }
                }
            }

            transform.position = next;
        }
        
        protected override void OnLifetimeExpired()
        {
            ReturnToPool();
        }

        private void OnValidate()
        {
            if (!autoEnsureSpriteRenderer) return;
            if (Application.isPlaying) return;
            EnsureSpriteRenderer();
        }

        private void EnsureSpriteRenderer()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

            if (_sr.sprite == null)
            {
                if (_defaultSprite == null)
                {
                    var tex = Texture2D.whiteTexture;
                    _defaultSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    _defaultSprite.name = "Bullet_DefaultSprite";
                }

                _sr.sprite = _defaultSprite;
            }

            _sr.color = spriteColor;
            _sr.sortingOrder = 100;
        }

        private void Reset()
        {
            if (autoEnsureSpriteRenderer)
            {
                EnsureSpriteRenderer();
            }
        }
    }
}
