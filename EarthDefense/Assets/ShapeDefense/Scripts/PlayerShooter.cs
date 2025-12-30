using UnityEngine;

namespace ShapeDefense.Scripts
{
    public sealed class PlayerShooter : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Camera camera;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Bullet bulletPrefab;

        [Header("Stats")]
        [SerializeField] private WeaponStats stats = new WeaponStats();

        [Header("Tuning")]
        [SerializeField] private bool holdToFire = true;

        private float _nextFireTime;

        private void Reset()
        {
            camera = Camera.main;
        }

        private void Awake()
        {
            if (camera == null) camera = Camera.main;
        }

        private void Update()
        {
            if (camera == null || bulletPrefab == null) return;

            var wantFire = holdToFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
            if (!wantFire) return;
            if (Time.time < _nextFireTime) return;

            var world = camera.ScreenToWorldPoint(Input.mousePosition);
            var dir = ((Vector2)world - (Vector2)transform.position);
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();

            var fireInterval = 1f / Mathf.Max(0.01f, stats.FireRate);
            _nextFireTime = Time.time + fireInterval;

            var muzzlePos = muzzle != null ? (Vector2)muzzle.position : (Vector2)transform.position;
            var bullet = Instantiate(bulletPrefab, muzzlePos, Quaternion.identity);
            bullet.transform.localScale = Vector3.one * stats.BulletVisualScale;

            // collider radius 맞추기
            if (bullet.TryGetComponent<CircleCollider2D>(out var col))
            {
                col.radius = stats.BulletRadius;
                col.isTrigger = true;
            }

            bullet.Fire(dir, stats.BulletSpeed, stats.Damage, stats.BulletLifeTime, gameObject);
        }
    }
}
