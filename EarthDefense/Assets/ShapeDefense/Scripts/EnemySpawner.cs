using UnityEngine;

namespace ShapeDefense.Scripts
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform target;
        [SerializeField] private Enemy enemyPrefab;

        [Header("Spawn")]
        [SerializeField] private float spawnRadius = 8f;
        [SerializeField] private float spawnRatePerSecond = 1.0f;
        [SerializeField] private int maxAlive = 80;

        [Header("Difficulty")]
        [SerializeField] private float baseEnemyHp = 3f;
        [SerializeField] private float enemyHpScalePerMinute = 0.35f;
        [SerializeField] private float baseEnemySpeed = 1.8f;
        [SerializeField] private float enemySpeedScalePerMinute = 0.08f;
        [SerializeField] private float baseContactDamage = 2f;
        [SerializeField] private float contactDamageScalePerMinute = 0.25f;

        private float _nextSpawnTime;
        private float _timeAlive;
        private int _alive;

        private void Update()
        {
            if (enemyPrefab == null || target == null) return;

            _timeAlive += Time.deltaTime;

            if (Time.time < _nextSpawnTime) return;
            if (_alive >= maxAlive) return;

            var interval = 1f / Mathf.Max(0.01f, spawnRatePerSecond);
            _nextSpawnTime = Time.time + interval;

            SpawnOne();
        }

        private void SpawnOne()
        {
            var a = Random.value * Mathf.PI * 2f;
            var p = (Vector2)target.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * spawnRadius;

            var enemy = Instantiate(enemyPrefab, p, Quaternion.identity);
            _alive++;

            var minutes = _timeAlive / 60f;
            var hp = baseEnemyHp * (1f + enemyHpScalePerMinute * minutes);
            var speed = baseEnemySpeed * (1f + enemySpeedScalePerMinute * minutes);
            var dmg = baseContactDamage * (1f + contactDamageScalePerMinute * minutes);

            enemy.Configure(hp, dmg);

            if (enemy.TryGetComponent<EnemyMotor>(out var motor))
            {
                motor.Configure(target, speed);
            }

            // 죽으면 카운트 감소
            if (enemy.TryGetComponent<Health>(out var health))
            {
                health.Died += () => _alive = Mathf.Max(0, _alive - 1);
            }
        }
    }
}
