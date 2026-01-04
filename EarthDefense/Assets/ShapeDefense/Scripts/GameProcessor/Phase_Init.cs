using UnityEngine;

namespace Script.GameProcessor
{
    public class Phase_Init 
        : GamePhase
    {
        public override EGamePhase PhaseType
        {
            get
            {
                return EGamePhase.Init;
            }
        }

        public override void Enter(GameProcessor context)
        {
            Debug.Log("[FSM] Phase_Init.Enter");
            context.TransitionTo(EGamePhase.Menu);
        }

        public override void Update()
        {
        }

        public override void Exit()
        {
            Debug.Log("[FSM] Phase_Init.Exit");
        }
    }
}
