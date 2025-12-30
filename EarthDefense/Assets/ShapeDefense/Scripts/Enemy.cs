using UnityEngine;

namespace ShapeDefense.Scripts
{
    [RequireComponent(typeof(Health))]
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] private float _contactDamage = 5f;
        [SerializeField] private float _contactCooldown = 0.25f;

        private float _nextContactTime;
        private Health _health;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        public void Configure(float hp, float contactDamage)
        {
            _health.ResetHp(hp);
            _contactDamage = contactDamage;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time < _nextContactTime) return;

            if (other.TryGetComponent<PlayerCore>(out var core) && core.Health != null)
            {
                _nextContactTime = Time.time + _contactCooldown;
                var p = (Vector2)transform.position;
                var dir = ((Vector2)other.transform.position - p).normalized;
                core.Health.TakeDamage(new DamageEvent(_contactDamage, p, dir, gameObject));
            }
        }
    }
}
