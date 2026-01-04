using Unity.VisualScripting;
using UnityEngine;

namespace Script.GameProcessor
{
    public abstract class GamePhase : MonoBehaviour
    {
        public abstract EGamePhase PhaseType { get; }
        public abstract void Enter(GameProcessor context);

        public abstract void Update();

        public abstract void Exit();
    }
}

