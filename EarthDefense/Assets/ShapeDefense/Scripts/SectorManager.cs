using System.Collections.Generic;
using UnityEngine;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 섹터(단일 슬롯)들을 총괄 관리. 각 SectorSpawner는 한 섹터만 담당한다.
    /// </summary>
    public sealed class SectorManager : MonoBehaviour
    {
        public static SectorManager Instance { get; private set; }

        [Header("Auto Build")]
        [SerializeField] private bool autoBuildOnStart = true;
        [SerializeField, Min(1)] private int sectorCount = 8;
        [SerializeField] private Sector sectorPrefab;
        [SerializeField] private Transform center;
        [SerializeField] private PlayerCore player;

        [Header("Existing Sectors")]
        [SerializeField] private List<Sector> sectors = new();

        private readonly Dictionary<int, Sector> _sectorBySlot = new();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (autoBuildOnStart)
            {
                RebuildSectors();
            }
        }

        public int SectorCount => sectors.Count;

        public float GetArcDeg()
        {
            return sectors.Count > 0 ? 360f / sectors.Count : 360f;
        }

        [ContextMenu("Rebuild Sectors")]
        public void RebuildSectors()
        {
            ClearSectors();
            if (sectorPrefab == null || center == null || player == null || sectorCount <= 0) return;

            float arc = 360f / sectorCount;
            for (int i = 0; i < sectorCount; i++)
            {
                float startDeg = i * arc;
                var sector = Instantiate(sectorPrefab, transform);
                sector.Initialize(center, player, i, startDeg, arc);
                Register(sector);
            }
        }

        private void ClearSectors()
        {
            foreach (var s in sectors)
            {
                if (s != null) Destroy(s.gameObject);
            }
            sectors.Clear();
            _sectorBySlot.Clear();
        }

        public void Register(Sector sector)
        {
            if (sector == null) return;
            if (!sectors.Contains(sector))
            {
                sectors.Add(sector);
            }
            _sectorBySlot[sector.SectorIndex] = sector;
        }

        public void Unregister(Sector sector)
        {
            if (sector == null) return;
            sectors.Remove(sector);
            if (_sectorBySlot.TryGetValue(sector.SectorIndex, out var s) && s == sector)
            {
                _sectorBySlot.Remove(sector.SectorIndex);
            }
        }

        public void UnregisterChunk(int SectorIndex, ChunkEnemy chunk)
        {
            if (_sectorBySlot.TryGetValue(SectorIndex, out var sector))
            {
                sector.UnregisterChunk(chunk);
            }
        }

        public bool TryGetSector(float angleDeg, out Sector sector)
        {
            sector = null;
            if (sectors.Count == 0) return false;

            float arc = GetArcDeg();
            float normalized = Mathf.Repeat(angleDeg + 360f, 360f);
            int slotIndex = Mathf.Clamp(Mathf.FloorToInt(normalized / arc), 0, Mathf.Max(0, sectors.Count - 1));

            return _sectorBySlot.TryGetValue(slotIndex, out sector) && sector != null;
        }

        public bool TryGetFrontChunk(float angleDeg, out ChunkEnemy chunk, out RingSectorDamageMask mask)
        {
            chunk = null;
            mask = null;
            if (sectors.Count == 0) return false;

            float arc = GetArcDeg();
            float normalized = Mathf.Repeat(angleDeg + 360f, 360f);
            int slotIndex = Mathf.Clamp(Mathf.FloorToInt(normalized / arc), 0, Mathf.Max(0, sectors.Count - 1));

            if (_sectorBySlot.TryGetValue(slotIndex, out var sector) && sector != null)
            {
                return sector.TryGetFrontChunk(out chunk, out mask);
            }

            return false;
        }
    }
}
