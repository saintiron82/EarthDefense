using UnityEngine;
using System.Collections.Generic;

namespace Script.GameProcessor
{
    public enum EGamePhase
    {
        Title,
        Init,
        Menu,
        NewGame,
        Running,
    }

    public class GameProcessor : MonoBehaviour
    {
        private readonly Dictionary<EGamePhase, GamePhase> _phases = new Dictionary<EGamePhase, GamePhase>();

        private GamePhase _currentPhase;

        [Header("Debug")] 
        [SerializeField]
        private EGamePhase _currentPhaseType;

        public EGamePhase CurrentPhaseType
        {
            get
            {
                return _currentPhaseType;
            }
        }

        private void Awake()
        {
            CacheChildPhases();
        }

        private void Start()
        {
            TransitionTo(EGamePhase.Title);
        }

        private void Update()
        {
            if (_currentPhase == null)
            {
                return;
            }

            _currentPhase.Update();
        }

        private void CacheChildPhases()
        {
            _phases.Clear();

            GamePhase[] childPhases = GetComponentsInChildren<GamePhase>(true);

            for (int i = 0; i < childPhases.Length; i++)
            {
                GamePhase phase = childPhases[i];

                if (phase == null)
                {
                    continue;
                }

                EGamePhase key = phase.PhaseType;

                if (_phases.ContainsKey(key))
                {
                    Debug.LogWarning("[FSM] Duplicate phase for type " + key + " on object " + phase.name + ". Ignoring additional instance.");
                    continue;
                }

                _phases.Add(key, phase);
            }
        }

        public void TransitionTo(EGamePhase phaseType)
        {
            GamePhase nextPhase;

            if (!_phases.TryGetValue(phaseType, out nextPhase) || nextPhase == null)
            {
                Debug.LogError("[FSM] No GamePhase component found for type " + phaseType + ".");
                return;
            }

            if (_currentPhase != null)
            {
                _currentPhase.Exit();
            }

            _currentPhase = nextPhase;
            _currentPhaseType = phaseType;

            Debug.Log("[FSM] Entering Phase: " + _currentPhaseType);

            _currentPhase.Enter(this);
        }
    }
}