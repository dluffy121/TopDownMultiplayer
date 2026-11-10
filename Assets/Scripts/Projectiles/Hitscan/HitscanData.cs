using Fusion;

namespace TDM
{
    public struct HitscanData : INetworkStruct
    {
        /// <summary>
        /// Type of the Projectile
        /// </summary>
        public byte ProjectileType;

        public Vector3Compressed FirePosition;
        public Vector3Compressed FireDirection;

        public Vector3Compressed ImpactPosition;
        public Vector3Compressed ImpactNormal;
    }
}