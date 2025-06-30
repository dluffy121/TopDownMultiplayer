using Fusion;
using UnityEngine;

namespace TDM
{
    [CreateAssetMenu(menuName = "Projectiles/Hitscan")]
    public class HitscanProjectile : ScriptableObject
    {
        [SerializeField] private float _maxDistance = 100f;
        [SerializeField] LayerMask _hitMask;

        [SerializeField] private HitscanProjectileVisual _visualPrefab;

        public virtual HitscanData GetHitscanData(ProjectileContext context, Vector3 firePosition, Vector3 direction)
        {
            HitscanData data = new()
            {
                FirePosition = firePosition,
                FireDirection = direction
            };

            if (context.Runner.LagCompensation.Raycast(firePosition,
                                                       direction,
                                                       _maxDistance,
                                                       context.Owner,
                                                       out LagCompensatedHit hitInfo,
                                                       _hitMask))
            {
                data.ImpactPosition = hitInfo.Point;
                data.ImpactNormal = hitInfo.Normal;
            }

            return data;
        }

        public HitscanProjectileVisual GetVisualInstance()
        {
            // TODO : Pooling
            HitscanProjectileVisual visualInstance = Instantiate(_visualPrefab);
            visualInstance.gameObject.SetActive(true);
            return visualInstance;
        }

        internal void RemoveVisualInstance(HitscanProjectileVisual visual)
        {
            // TODO : Pooling
            Destroy(visual);
        }
    }
}