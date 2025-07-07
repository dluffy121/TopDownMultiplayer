using Fusion;
using UnityEngine;

namespace TDM
{
    public static class HitProcessor
    {
        public static bool ProcessProjectileHit(ProjectileContext context,
                                                GameObject hitGameObject,
                                                byte damage,
                                                Vector3 point,
                                                Vector3 direction,
                                                Vector3 normal)
        {
            if (!context.Runner.TryGetPlayerObject(context.Owner, out NetworkObject playerObject))
                return false;

            IHitInstigator instigator = playerObject.GetComponent<IHitInstigator>();

            if (!hitGameObject.TryGetComponent(out IHitTarget target)) return false;

            return ProcessProjectileHit(target,
                                        instigator,
                                        damage,
                                        point,
                                        direction,
                                        normal);
        }

        public static bool ProcessProjectileHit(IHitTarget target,
                                                IHitInstigator instigator,
                                                byte damage,
                                                Vector3 point,
                                                Vector3 direction,
                                                Vector3 normal)
        {
            HitData hit = new()
            {
                Target = target,
                Instigator = instigator,
                Damage = damage,
                Point = point,
                Direction = direction,
                Normal = normal,
            };

            return target.TryTakeHit(ref hit);
        }
    }
}