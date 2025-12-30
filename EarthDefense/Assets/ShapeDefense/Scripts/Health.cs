using System;
using UnityEngine;

namespace ShapeDefense.Scripts
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHp = 10f;
        [SerializeField] private bool _destroyOnDeath = true;

        public float MaxHp => _maxHp;
        public float Hp { get; private set; }
        public bool IsDead => Hp <= 0f;

        public event Action<DamageEvent> Damaged;
        public event Action Died;

        private void Awake()
        {
            Hp = _maxHp;
        }

        public void ResetHp(float maxHp)
        {
            _maxHp = Mathf.Max(1f, maxHp);
            Hp = _maxHp;
        }

        public void TakeDamage(in DamageEvent e)
        {
            if (IsDead) return;

            Hp -= Mathf.Max(0f, e.Amount);
            Damaged?.Invoke(e);

            if (Hp <= 0f)
            {
                Hp = 0f;
                Died?.Invoke();

                if (_destroyOnDeath)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
