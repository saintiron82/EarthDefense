using System.Collections.Generic;
using UnityEngine;

namespace Polar.Weapons
{
    [CreateAssetMenu(fileName = "PolarWeaponDataTable", menuName = "EarthDefense/Polar/Weapon Data Table", order = 0)]
    public class PolarWeaponDataTable : ScriptableObject
    {
        [SerializeField] private List<PolarWeaponData> weapons = new List<PolarWeaponData>();

        public IReadOnlyList<PolarWeaponData> Weapons => weapons;

        public PolarWeaponData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return weapons.Find(w => w.Id == id);
        }

#if UNITY_EDITOR
        [ContextMenu("Validate Data")]
        private void ValidateData()
        {
            var ids = new HashSet<string>();
            int errors = 0;
            foreach (var w in weapons)
            {
                if (w == null) { errors++; Debug.LogError("[PolarWeaponDataTable] Null entry"); continue; }
                if (string.IsNullOrEmpty(w.Id)) { errors++; Debug.LogError($"[PolarWeaponDataTable] Empty Id: {w.WeaponName}"); }
                if (!string.IsNullOrEmpty(w.Id) && !ids.Add(w.Id)) { errors++; Debug.LogError($"[PolarWeaponDataTable] Duplicate Id: {w.Id}"); }
                if (string.IsNullOrEmpty(w.WeaponBundleId)) { errors++; Debug.LogError($"[PolarWeaponDataTable] {w.Id} WeaponBundleId empty"); }
            }
            if (errors == 0) Debug.Log($"[PolarWeaponDataTable] Validation OK ({weapons.Count} items)");
        }
#endif
    }
}

