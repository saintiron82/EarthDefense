using Polar.Field;
using Polar.Weapons;
using UnityEngine;
using SG;
using ShapeDefense.Scripts;

namespace Script.GameProcessor
{
    public enum NewGameStep
    {
        None,
        MapGeneration,
        CityPlacement,
        CityLogistics,
        Rendering,
        Completed,
    }

    public class Phase_NewGame 
        : GamePhase
    {
        public override EGamePhase PhaseType
        {
            get
            {
                return EGamePhase.NewGame;
            }
        }
        [SerializeField] private PolarFieldController _polarFieldController;
        [SerializeField] private PlayerWeaponManager _weaponManager;
        public override void Enter(GameProcessor context)
        {
            _polarFieldController.gameObject.SetActive(true);
            _weaponManager.gameObject.SetActive(true);
            _weaponManager.Init();
        }

        public override void Update()
        {
            
        }

        public override void Exit()
        {
            
        }
    }
}
