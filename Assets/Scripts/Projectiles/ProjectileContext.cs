using Fusion;

namespace TDM
{
    /// <summary>
    /// Context for Projectiles
    /// </summary>
    // NOTE : Since a context should only be valid if during an operation carried out by the handler, it should not be cached locally
    public ref struct ProjectileContext
    {
        /// <summary>
        /// Reference to runner for getting tick data, performing physics casts, 
        /// </summary>
        /// <remarks>
        /// Every actions with this runner should be ReadOnly
        /// </remarks>
        // TODO : Maybe create an interface to perform required actions and avoid write operations
        public NetworkRunner Runner;

        /// <summary>
        /// Reference to Owner Input Authority, to perform physics casts
        /// </summary>
        public PlayerRef Owner;
    }
}