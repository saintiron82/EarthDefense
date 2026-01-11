using UnityEngine;

namespace ShapeDefense.Scripts
{
    public sealed class PlayerCore : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private Health _health;
        
        [SerializeField] private WeaponController _weaponController;

        public Health Health => _health;

        private void Reset()
        {
            _health = GetComponent<Health>();
            _weaponController.SetTeamKey(_health.TeamKey);
        }

        private void Start()
        {
            Debug.Log($"[PlayerCore] Position: {transform.position}");
        }
    }
}
