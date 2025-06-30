using Fusion;

namespace TDM
{
    internal interface IWeapon
    {
        void Fire(NetworkRunner runner);
        bool CanSpawnProjectile(NetworkRunner runner, PlayerRef player, NPlayerInputData inputData);
    }
}