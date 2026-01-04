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
        [SerializeField] private SectorManager _sectorManager;
        [SerializeField] private WeaponController _weaponController;
        public override void Enter(GameProcessor context)
        {
            _sectorManager.gameObject.SetActive(true);
            _weaponController.gameObject.SetActive(true);
            _weaponController.Init();
        }

        public override void Update()
        {
            
        }

        public override void Exit()
        {
            
        }
    }
}
