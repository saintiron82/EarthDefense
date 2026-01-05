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
        private bool _isDetached;
        private Vector2 _detachDirection = Vector2.right;
        private float _detachSpeed;
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

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(beamColor, 0f), new GradientColorKey(beamColor, 1f) },
                new[] { new GradientAlphaKey(beamColor.a, 0f), new GradientAlphaKey(beamColor.a, 1f) });
            lineRenderer.colorGradient = gradient;

            if (lineRenderer.material != null)
            {
                var mat = lineRenderer.material;
                mat.color = beamColor;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", beamColor);
                }
                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", beamColor);
                }
                if (mat.HasProperty("_TintColor"))
                {
                    mat.SetColor("_TintColor", beamColor);
                }
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", beamColor);
                }
            }
        }

        public void BeginRetract()
        {
            _isRetracting = true;
        }

        public void OverrideMaxHits(int maxHits)
        {
            _maxHits = maxHits;
        }

        public void DetachAndExpire(Vector2 direction, float travelSpeed, float remainingLifetime)
        {
            _isRetracting = true;
            _detachDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            _detachSpeed = Mathf.Max(0f, travelSpeed);
            _isDetached = true;
            _spawnTime = Time.time;
            _lifetime = remainingLifetime;
            ClearBlock();
            _blockLength = 0f;
            transform.SetParent(null);
        }

        protected override void OnFired()
        {
            base.OnFired();
            _currentLength = 0f;
            _nextTickTime = Time.time;
            _isRetracting = false;
            _isDetached = false;
            _detachSpeed = 0f;
            _detachDirection = Vector2.right;
            _lastHitTimeByTargetId.Clear();
            _hitsLeft = MaxHits <= 0 ? int.MaxValue : MaxHits;
            _blockingHealth = null;
            _blockingChunk = null;
            _blockLength = 0f;
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
            }
        }

        private void ClearBlock()
        {
            _blockingHealth = null;
            _blockingChunk = null;
            _blockLength = 0f;
        }

        protected override void UpdateProjectile()
        {
            float delta = Time.deltaTime;

            if (_isDetached && _detachSpeed > 0f)
            {
                transform.position += (Vector3)(_detachDirection * _detachSpeed * delta);
            }

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
            float desiredLength = _currentLength;

            // 기존 블록 유효성 확인
            if (_blockingHealth != null && _blockingHealth.IsDead)
            {
                ClearBlock();
            }
            else if (_blockingChunk != null && _blockAngleIdx >= 0 && _blockRadiusIdx >= 0 && _blockingChunk.IsCellDestroyed(_blockingChunk.AngleCells * _blockRadiusIdx + _blockAngleIdx))
            {
                ClearBlock();
            }

            bool hasSweepHit = false;
            HitResult sweepHit = default;
            Vector2 sweepEnd = startPos + aimDir * desiredLength;

            if (TrySweepHit(startPos, sweepEnd, aimDir, sweepSteps, sweepEpsilon, hitRadius, out sweepHit))
            {
                hasSweepHit = true;
                _blockLength = Vector2.Distance(startPos, sweepHit.Point);
                sweepEnd = startPos + aimDir * Mathf.Min(desiredLength, _blockLength);
                _currentLength = Mathf.Min(_currentLength, _blockLength);

                if (sweepHit.Health != null)
                {
                    _blockingHealth = sweepHit.Health;
                    _blockingChunk = null;
                }
                else if (sweepHit.Chunk != null)
                {
                    _blockingChunk = sweepHit.Chunk;
                    _blockingHealth = null;
                    _blockAngleIdx = sweepHit.AngleIndex;
                    _blockRadiusIdx = sweepHit.RadiusIndex;
                }
            }
            else if (_blockingHealth != null || _blockingChunk != null)
            {
                sweepEnd = startPos + aimDir * Mathf.Min(desiredLength, _blockLength);
                _currentLength = Mathf.Min(_currentLength, _blockLength);
            }

            if (_hitsLeft > 0 && Time.time >= _nextTickTime)
            {
                if (hasSweepHit && CanHit(sweepHit))
                {
                    float damagePerTick = Damage / Mathf.Max(0.0001f, _tickRate);
                    ApplyHit(sweepHit, aimDir, damagePerTick, hitEffect, false);
                    if (_hitsLeft != int.MaxValue)
                    {
                        _hitsLeft = Mathf.Max(0, _hitsLeft - 1);

                        if (_hitsLeft <= 0)
                        {
                            BeginRetract();
                        }
                    }
                }

                _nextTickTime = Time.time + (1f / _tickRate);
            }

            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, sweepEnd);
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
            _isDetached = false;
            _detachSpeed = 0f;
            _detachDirection = Vector2.right;
            _hitsLeft = 0;
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
    }
}
