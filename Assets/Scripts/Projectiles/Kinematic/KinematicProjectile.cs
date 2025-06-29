using UnityEngine;
using Fusion;

namespace TDM
{
    [CreateAssetMenu(menuName = "Projectiles/Kinematic")]
    public class KinematicProjectile : ScriptableObject
    {
        [SerializeField] float _speed;
        [SerializeField] float _maxDistance;
        [SerializeField] float _maxTime;

        [SerializeField] LayerMask hitMask;

        [SerializeField] KinematicProjectileVisual _visualPrefab;

        protected int _lifetimeTicks = -1;

        public virtual KinematicData GetKinematicData(ProjectileContext context, Vector3 firePosition, Vector3 direction)
        {
            // Anticipate total lifetime for this type of projectile 
            if (_lifetimeTicks < 0)
            {
                // If speed is a dynamic concept, can make the method virtual to anticipate the life based on speed change
                int maxDistanceTicks = Mathf.RoundToInt(_maxDistance / _speed * context.Runner.TickRate);
                int maxTimeTicks = Mathf.RoundToInt(_maxTime * context.Runner.TickRate);

                _lifetimeTicks =
                    maxDistanceTicks > 0 && maxTimeTicks > 0
                    ? Mathf.Min(maxDistanceTicks, maxTimeTicks)
                    : (maxDistanceTicks > 0 ? maxDistanceTicks : maxTimeTicks);
            }

            return new()
            {
                Position = firePosition,
                Velocity = direction * _speed
            };
        }

        public KinematicProjectileVisual GetVisualInstance()
        {
            // TODO : Pooling
            KinematicProjectileVisual visualInstance = Instantiate(_visualPrefab);
            visualInstance.gameObject.SetActive(true);
            return visualInstance;
        }

        internal void RemoveVisualInstance(KinematicProjectileVisual visual)
        {
            // TODO : Pooling
            Destroy(visual);
        }

        public virtual void OnFixedUpdate(ProjectileContext context, ref KinematicData data)
        {
            NetworkRunner runner = context.Runner;

            Vector3 previousTickPos = KinematicData.GetMovePosition(data, runner, runner.Tick - 1);
            Vector3 currentTickPos = KinematicData.GetMovePosition(data, runner, runner.Tick);

            Vector3 direction = currentTickPos - previousTickPos;
            float distance = direction.magnitude;
            direction /= distance;

            HitOptions hitOptions = HitOptions.IncludePhysX | HitOptions.SubtickAccuracy | HitOptions.IgnoreInputAuthority;
            if (runner.LagCompensation.Raycast(previousTickPos,
                                                       direction,
                                                       distance,
                                                       context.Owner,
                                                       out LagCompensatedHit hit,
                                                       hitMask,
                                                       hitOptions))
            {
                // Destroy(hit.GameObject);

                data.IsFinished = true;
            }

            if (runner.Tick - data.FireTick >= _lifetimeTicks)
                data.IsFinished = true;

            Debug.LogError("Fixed Update: " + previousTickPos);
        }
    }
}