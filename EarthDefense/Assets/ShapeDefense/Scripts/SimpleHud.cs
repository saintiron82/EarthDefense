using UnityEngine;
using UnityEngine.UI;

namespace ShapeDefense.Scripts
{
    public sealed class SimpleHud : MonoBehaviour
    {
        [SerializeField] private PlayerCore core;
        [SerializeField] private Text hpText;
        [SerializeField] private Text timeText;

        private float _timeAlive;

        private void Update()
        {
            _timeAlive += Time.deltaTime;

            if (core != null && core.Health != null && hpText != null)
            {
                hpText.text = $"CORE HP: {Mathf.CeilToInt(core.Health.Hp)} / {Mathf.CeilToInt(core.Health.MaxHp)}";
            }

            if (timeText != null)
            {
                timeText.text = $"TIME: {_timeAlive:0.0}s";
            }
        }
    }
}
