using System;
using UnityEngine;

namespace ShapeDefense.Scripts
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [Header("Identity")]
        [Tooltip("개별 Health를 구분하기 위한 고유 ID. 자동으로 InstanceID를 사용합니다.")]
        [SerializeField, HideInInspector] private int uniqueId;
        [Tooltip("팀/아군 구분 키(예: 0=중립, 1=플레이어, 2=적 등).")]
        [SerializeField] private int teamKey;

        [SerializeField] private float maxHp = 10f;
        [SerializeField] private bool destroyOnDeath = true;

        public int UniqueId => uniqueId;
        public int TeamKey => teamKey;
        public float MaxHp => maxHp;
        public float Hp { get; private set; }
        public bool IsDead => Hp <= 0f;

        public event Action<DamageEvent> Damaged;
        public event Action Died;

        private void Awake()
        {
            uniqueId = GetInstanceID();
            Hp = maxHp;
        }

        private void OnEnable()
        {
            DamageableRegistry.Register(this);
        }

        private void OnDisable()
        {
            DamageableRegistry.Unregister(this);
        }

        public void ResetHp(float newMaxHp)
        {
            this.maxHp = Mathf.Max(1f, newMaxHp);
            Hp = this.maxHp;
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

                if (destroyOnDeath)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
