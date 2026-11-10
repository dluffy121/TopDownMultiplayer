using Fusion;
using UnityEngine;

namespace TDM
{
    internal interface IWeapon
    {
        GameObject gameObject { get; }

        void Fire(NetworkRunner runner);
        bool CanSpawnProjectile(NetworkRunner runner, PlayerRef player, NPlayerInputData inputData);
    }
}