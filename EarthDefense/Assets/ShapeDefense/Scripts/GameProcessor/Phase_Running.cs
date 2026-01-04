using UnityEngine;

namespace Script.GameProcessor
{
    public class Phase_Running 
        : GamePhase
    {
        private float _lastGameOverCheckTime;
        private float _gameOverCheckInterval = 1f;
        private bool _isGameOver;

        public override EGamePhase PhaseType
        {
            get
            {
                return EGamePhase.Running;
            }
        }

        public override void Enter(GameProcessor context)
        {
            Debug.Log("[FSM] Phase_Running.Enter");
            _lastGameOverCheckTime = Time.time;
            _isGameOver = false;
        }

        public override void Update()
        {
            if (_isGameOver)
            {
                return;
            }

            float now = Time.time;
            if (now - _lastGameOverCheckTime >= _gameOverCheckInterval)
            {
                _lastGameOverCheckTime = now;
                CheckGameOver();
            }
        }

        private void CheckGameOver()
        {
            
        }

        private void TriggerGameOver(int maxScore)
        {
            
        }

        public override void Exit()
        {
            Debug.Log("[FSM] Phase_Running.Exit");
        }
    }
}
