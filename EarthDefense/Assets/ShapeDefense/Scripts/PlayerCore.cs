using UnityEngine;

namespace ShapeDefense.Scripts
{
    public sealed class PlayerCore : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private Health _health;

        public Health Health => _health;

        private void Reset()
        {
            _health = GetComponent<Health>();
        }
    }
}
