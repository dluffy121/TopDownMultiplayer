using Fusion;
using UnityEngine;

namespace TDM
{
    public struct KinematicData : INetworkStruct
    {
        // TIP : takes 1 byte, can use a bit flag instead if more than 1
        /// <summary>
        /// Whether the projectile is Finished, timed out or hit something
        /// </summary>
        public bool IsFinished;

        /// <summary>
        /// Type of the Projectile
        /// </summary>
        public byte ProjectileType;

        /// <summary>
        /// Fire tick of the fired projectile, lifetime
        /// </summary>
        public int FireTick;

        public Vector3Compressed Velocity;

        public Vector3Compressed Position;

        public static Vector3 GetMovePosition(KinematicData data, NetworkRunner runner, float currentTick)
        {
            float time = (currentTick - data.FireTick) * runner.DeltaTime;

            if (time <= 0f)
                return data.Position;

            return data.Position + (Vector3)data.Velocity * time;
        }
    }
}