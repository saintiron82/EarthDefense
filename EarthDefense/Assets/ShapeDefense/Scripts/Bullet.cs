using UnityEngine;

namespace ShapeDefense.Scripts
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class Bullet : MonoBehaviour
    {
        private float _damage;
        private float _speed;
        private float _lifeTime;
        private float _spawnTime;
        private Vector2 _dir;
        private GameObject _source;

        public void Fire(Vector2 dir, float speed, float damage, float lifeTime, GameObject source)
        {
            _dir = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.right;
            _speed = speed;
            _damage = damage;
            _lifeTime = lifeTime;
            _source = source;
            _spawnTime = Time.time;
        }

        private void Update()
        {
            transform.position += (Vector3)(_dir * (_speed * Time.deltaTime));

            if (Time.time - _spawnTime >= _lifeTime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<Health>(out var health))
            {
                // 플레이어 코어에 맞는 경우는 현재 설계에서 제외(Enemy가 접촉 피해)
                if (other.GetComponent<PlayerCore>() != null) return;

                var p = (Vector2)transform.position;
                var dir = _dir;
                health.TakeDamage(new DamageEvent(_damage, p, dir, _source));
                Destroy(gameObject);
            }
        }

        private void Reset()
        {
            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
        }
    }
}
