using UnityEngine;

namespace ShapeDefense.Scripts
{
    public sealed class EnemyMotor : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _speed = 2.5f;

        public void Configure(Transform target, float speed)
        {
            _target = target;
            _speed = speed;
        }

        private void Update()
        {
            if (_target == null) return;

            var p = (Vector2)transform.position;
            var t = (Vector2)_target.position;
            var dir = (t - p);
            var dist = dir.magnitude;
            if (dist < 0.001f) return;

            dir /= dist;
            transform.position = p + dir * (_speed * Time.deltaTime);
        }
    }
}
