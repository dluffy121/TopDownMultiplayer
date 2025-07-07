using Fusion;
using UnityEngine;

namespace TDM
{
    [CreateAssetMenu(menuName = "Projectiles/Hitscan")]
    public class HitscanProjectile : ScriptableObject
    {
        [SerializeField] private float _maxDistance = 100f;
        [SerializeField] LayerMask _hitMask;
        [SerializeField] byte _baseDamage;

        [SerializeField] private HitscanProjectileVisual _visualPrefab;

        public virtual HitscanData GetHitscanData(ProjectileContext context, Vector3 firePosition, Vector3 direction)
        {
            HitscanData data = new()
            {
                FirePosition = firePosition,
                FireDirection = direction
            };

            HitOptions hitOptions = HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;
            if (context.Runner.LagCompensation.Raycast(firePosition,
                                                       direction,
                                                       _maxDistance,
                                                       context.Owner,
                                                       out LagCompensatedHit hit,
                                                       _hitMask,
                                                       hitOptions))
            {
                data.ImpactPosition = hit.Point;
                data.ImpactNormal = hit.Normal;

                HitProcessor.ProcessProjectileHit(context,
                                                  hit.GameObject,
                                                  _baseDamage,
                                                  hit.Point,
                                                  direction,
                                                  hit.Normal);
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
            Destroy(visual.gameObject);
        }
    }
}