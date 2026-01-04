using UnityEngine;

namespace Script.GameProcessor
{
    public class Phase_Menu 
        : GamePhase
    {
        private GameProcessor _context;

        public override EGamePhase PhaseType
        {
            get
            {
                return EGamePhase.Menu;
            }
        }

        public override void Enter(GameProcessor context)
        {
            _context = context;

            Debug.Log("[FSM] Phase_Menu.Enter");

            // TODO: Show main menu UI and handle map/slot selection.
            // For now, immediately start the NewGame setup phase.

            _context.TransitionTo(EGamePhase.NewGame);
        }

        public override void Update()
        {
        }

        public override void Exit()
        {
            Debug.Log("[FSM] Phase_Menu.Exit");
        }
    }
}
