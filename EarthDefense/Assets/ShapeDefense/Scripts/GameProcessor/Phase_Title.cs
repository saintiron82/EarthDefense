using UnityEngine;

namespace Script.GameProcessor
{
    public class Phase_Title 
        : GamePhase
    {
        public override EGamePhase PhaseType
        {
            get
            {
                return EGamePhase.Title;
            }
        }

        public override void Enter(GameProcessor context)
        {
            Debug.Log("[FSM] Phase_Title.Enter");

            // TODO: Show title screen UI and wait for user input to proceed.
            // For now, immediately continue to Init.

            context.TransitionTo(EGamePhase.Init);
        }

        public override void Update()
        {
        }

        public override void Exit()
        {
            Debug.Log("[FSM] Phase_Title.Exit");
        }
    }
}
