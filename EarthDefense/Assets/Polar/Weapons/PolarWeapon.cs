using UnityEngine;
using Script.SystemCore.Pool;
using Script.SystemCore.Resource;

namespace Polar.Weapons
{
    /// <summary>
    /// Polar 전용 무기: PolarProjectile/PolarBeamProjectile 발사.
    /// </summary>
    public sealed class PolarWeapon : MonoBehaviour
    {
        [SerializeField] private PolarWeaponData weaponData;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private PolarBeamProjectile beamPrefab;
        [SerializeField] private float projectileSpeed = 10f;

        private IPolarField _field;
        private bool _usePool;

        public void Initialize(IPolarField field, PolarWeaponData data = null, bool usePool = false)
        {
            _field = field;
            if (data != null) weaponData = data;
            _usePool = usePool;
            LoadPrefabsFromBundlesIfNeeded();
        }

        private void LoadPrefabsFromBundlesIfNeeded()
        {
            if (weaponData == null) return;
            var resource = ResourceService.Instance;
            if (resource != null)
            {
                if (projectilePrefab == null && !string.IsNullOrEmpty(weaponData.ProjectileBundleId))
                {
                    projectilePrefab = resource.LoadPrefab(weaponData.ProjectileBundleId);
                }
                if (beamPrefab == null && !string.IsNullOrEmpty(weaponData.BeamBundleId))
                {
                    var go = resource.LoadPrefab(weaponData.BeamBundleId);
                    if (go != null)
                    {
                        beamPrefab = go.GetComponent<PolarBeamProjectile>();
                        if (beamPrefab == null)
                        {
                            beamPrefab = go.AddComponent<PolarBeamProjectile>();
                        }
                    }
                }
            }
        }

        public void FireProjectile(float angleDeg, float startRadius)
        {
            if (_field == null || weaponData == null || (projectilePrefab == null && !_usePool)) return;
            Vector3 origin = (_field as Component) != null ? ((Component)_field).transform.position : Vector3.zero;

            PolarProjectile proj = null;
            if (_usePool && PoolService.Instance != null && !string.IsNullOrEmpty(weaponData.ProjectileBundleId))
            {
                var pooled = PoolService.Instance.Get<PolarProjectile>(weaponData.ProjectileBundleId, origin, Quaternion.identity);
                proj = pooled;
            }
            else
            {
                var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
                go.TryGetComponent(out proj);
            }

            if (proj != null)
            {
                proj.Launch(_field, weaponData, angleDeg, startRadius, projectileSpeed);
            }
        }

        public void FireBeam(Vector2 origin, Vector2 direction)
        {
            if (_field == null || weaponData == null || (beamPrefab == null && !_usePool)) return;

            PolarBeamProjectile beam = null;
            if (_usePool && PoolService.Instance != null && !string.IsNullOrEmpty(weaponData.BeamBundleId))
            {
                var pooled = PoolService.Instance.Get<PolarBeamProjectile>(weaponData.BeamBundleId, origin, Quaternion.identity);
                beam = pooled;
            }
            else
            {
                beam = Instantiate(beamPrefab, origin, Quaternion.identity);
            }

            if (beam != null)
            {
                beam.Fire(_field, weaponData, origin, direction);
            }
        }
    }
}
