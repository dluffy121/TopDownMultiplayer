using UnityEngine;
using Fusion;

namespace TDM
{
    public class SimpleWeapon : MonoBehaviour
    {
        [SerializeField] StandaloneProjectile _projectilePrefab;

        [SerializeField] Transform _muzzlePos;

        public void Fire(NetworkRunner runner)
        {
            // runner.Spawn()
        }
    }
}