using UnityEngine;

namespace TDM
{
    [CreateAssetMenu(menuName = "Projectiles/RicochetBullet")]
    public class RicochetBullet : KinematicProjectile
    {
        public override void OnFixedUpdate(ProjectileContext context, ref KinematicData data)
        {
            base.OnFixedUpdate(context, ref data);
        }
    }
}