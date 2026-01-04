using UnityEngine;

namespace ShapeDefense.Scripts.Weapons
{
    /// <summary>
    /// 콜라이더 없이 스윕 판정으로 동작하는 빔 발사체. 확장/리트랙 및 틱 데미지 처리.
    /// </summary>
    public sealed class BeamProjectile : BaseProjectile
    {
        [Header("Beam Visual")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Color beamColor = Color.cyan;
        [SerializeField] private HitEffect hitEffect;

        [Header("Beam Motion")]
        [SerializeField] private float extendSpeed = 50f;
        [SerializeField] private float retractSpeed = 70f;
        [SerializeField] private float maxLength = 50f;

        [Header("Hit Settings")]
        [SerializeField, Range(1, 64)] private int sweepSteps = 12;
        [SerializeField, Min(0f)] private float sweepEpsilon = 0f;

        private float _currentLength;
        private float _nextTickTime;
        private float _tickRate = 5f;
        private bool _isRetracting;
        private int _hitsLeft;
        private Health _blockingHealth;
        private RingSectorDamageMask _blockingChunk;
        private int _blockAngleIdx;
        private int _blockRadiusIdx;
        private float _blockLength;

        public void Configure(float width, float hitRadius, int sweepSteps, float sweepEpsilon, float rehitCooldown, float tickRate, float extendSpeed, float retractSpeed, float maxLength, Color color, HitEffect effect)
        {
            this.hitRadius = hitRadius;
            this.sweepSteps = sweepSteps;
            this.sweepEpsilon = sweepEpsilon;
            this.rehitCooldown = rehitCooldown;
            _tickRate = tickRate;
            this.extendSpeed = extendSpeed;
            this.retractSpeed = retractSpeed;
            this.maxLength = maxLength;
            beamColor = color;
            hitEffect = effect;

            if (lineRenderer == null)
            {
                lineRenderer = gameObject.GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
            }
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.positionCount = 2;
            lineRenderer.startColor = beamColor;
            lineRenderer.endColor = beamColor;
        }

        public void BeginRetract()
        {
            _isRetracting = true;
        }

        protected override void OnFired()
        {
            base.OnFired();
            _currentLength = 0f;
            _nextTickTime = Time.time;
            _isRetracting = false;
            _lastHitTimeByTargetId.Clear();
            _hitsLeft = MaxHits <= 0 ? 1 : MaxHits;
            _blockingHealth = null;
            _blockingChunk = null;
            _blockLength = 0f;
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
            }
        }

        protected override void UpdateProjectile()
        {
            float delta = Time.deltaTime;

            if (_isRetracting)
            {
                _currentLength = Mathf.Max(0f, _currentLength - retractSpeed * delta);
                if (_currentLength <= 0f)
                {
                    ReturnToPool();
                    return;
                }
            }
            else
            {
                _currentLength = Mathf.Min(maxLength, _currentLength + extendSpeed * delta);
            }

            Vector2 startPos = transform.position;
            Vector2 aimDir = transform.right;
            float rayLength = _currentLength;
            Vector2 endPos = startPos + aimDir * rayLength;

            // Clamp to the stored blocking point if we already have one
            if (_blockingHealth != null || _blockingChunk != null)
            {
                rayLength = Mathf.Min(rayLength, _blockLength);
                endPos = startPos + aimDir * rayLength;
            }

            if (!_isRetracting && _hitsLeft > 0 && Time.time >= _nextTickTime)
            {
                if (_blockingHealth != null)
                {
                    if (_blockingHealth.IsDead)
                    {
                        _blockingHealth = null;
                        _blockLength = 0f;
                        _hitsLeft = Mathf.Max(0, _hitsLeft - 1);
                    }
                    else
                    {
                        float damagePerTick = Damage / Mathf.Max(0.0001f, _tickRate);
                        var hit = new HitResult(startPos + aimDir * _blockLength, null, _blockingHealth);
                        ApplyHit(hit, aimDir, damagePerTick, hitEffect, false);
                        endPos = startPos + aimDir * _blockLength;
                    }
                }
                else if (_blockingChunk != null)
                {
                    if (_blockAngleIdx >= 0 && _blockRadiusIdx >= 0 && _blockingChunk.IsCellDestroyed(_blockingChunk.AngleCells * _blockRadiusIdx + _blockAngleIdx))
                    {
                        _blockingChunk = null;
                        _hitsLeft = Mathf.Max(0, _hitsLeft - 1);
                        _blockLength = 0f;
                    }
                    else
                    {
                        float damagePerTick = Damage / Mathf.Max(0.0001f, _tickRate);
                        var hit = new HitResult(startPos + aimDir * _blockLength, _blockingChunk, null, _blockAngleIdx, _blockRadiusIdx);
                        ApplyHit(hit, aimDir, damagePerTick, hitEffect, false);
                        endPos = startPos + aimDir * _blockLength;
                    }
                }
                else if (TrySweepHit(startPos, endPos, aimDir, sweepSteps, sweepEpsilon, hitRadius, out var hit) && CanHit(hit))
                {
                    float damagePerTick = Damage / Mathf.Max(0.0001f, _tickRate);
                    ApplyHit(hit, aimDir, damagePerTick, hitEffect, false);

                    _blockLength = Vector2.Distance(startPos, hit.Point);
                    _currentLength = Mathf.Min(_currentLength, _blockLength);
                    endPos = startPos + aimDir * _blockLength;

                    if (hit.Health != null)
                    {
                        _blockingHealth = hit.Health;
                        _blockingChunk = null;
                    }
                    else if (hit.Chunk != null)
                    {
                        _blockingChunk = hit.Chunk;
                        _blockingHealth = null;
                        _blockAngleIdx = hit.AngleIndex;
                        _blockRadiusIdx = hit.RadiusIndex;
                    }

                    if (MaxHits <= 1)
                    {
                        BeginRetract();
                    }
                }

                if (_hitsLeft <= 0)
                {
                    BeginRetract();
                }

                _nextTickTime = Time.time + (1f / _tickRate);
            }

            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, endPos);
            }
        }

        protected override void OnLifetimeExpired()
        {
            BeginRetract();
        }

        public override void OnReturnToPool()
        {
            base.OnReturnToPool();
            transform.SetParent(null);
            _blockingHealth = null;
            _blockingChunk = null;
            _blockLength = 0f;
            _currentLength = 0f;
            _isRetracting = false;
            _hitsLeft = 0;
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        public void OverrideMaxHits(int maxHits)
        {
            _maxHits = maxHits;
        }
    }
}
