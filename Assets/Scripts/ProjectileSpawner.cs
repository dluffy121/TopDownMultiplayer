using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace TDM
{
    public class ProjectileSpawner : MonoBehaviour, INetworkInputListener
    {
        class PlayerProjectileData
        {
            public List<NetworkObject> Projectiles;
            public TickTimer NextSpawnTime;
        }

        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private KeyCode _inputKey = KeyCode.Mouse0;
        [SerializeField] private float _spawnRate = 5f / 1f;
        [SerializeField] private float _lifeSpan = 5f;

        private Dictionary<PlayerRef, PlayerProjectileData> _projectilesData = new();
        private bool _keyState_Fire = false;

        void OnEnable()
        {
            NetworkManager.RegisterForInput(this);
        }

        void OnDisable()
        {
            NetworkManager.UnregisterFromInput(this);
        }

        private void Update()
        {
            _keyState_Fire |= Input.GetKeyDown(_inputKey);
        }

        #region Spawning

        #endregion

        #region Input


        void INetworkInputListener.OnInput(NetworkRunner runner, ref NPlayerInputData input)
        {
            input.buttons.Set(NPlayerInputData.MOUSE_BUTTON_0, _keyState_Fire);
            _keyState_Fire = false;
        }

        #endregion

        public bool CanSpawnProjectile(NetworkRunner runner, PlayerRef player, NPlayerInputData inputData)
        {
            if (!runner.IsServer) return false;

            if (!inputData.buttons.IsSet(NPlayerInputData.MOUSE_BUTTON_0))
                return false;

            if (!_projectilesData.TryGetValue(player, out PlayerProjectileData playerData))
            {
                playerData = new()
                {
                    NextSpawnTime = TickTimer.CreateFromSeconds(runner, 1f / _spawnRate)
                };
                _projectilesData[player] = playerData;
            }

            return playerData.NextSpawnTime.ExpiredOrNotRunning(runner);
        }

        public T SpawnProjectile<T>(NetworkRunner runner, PlayerRef player, Vector3 position, Quaternion rotation)
            where T : Projectile
        {
            if (!runner.IsServer) return default;

            if (!_projectilesData.TryGetValue(player, out PlayerProjectileData playerData))
            {
                Debug.LogError($"No projectile data found for player {player}, Please call {nameof(CanSpawnProjectile)} first.");
                return default;
            }

            playerData.Projectiles ??= new();
            NetworkSpawnStatus projectileSpawnStatus =
                runner.TrySpawn(_projectilePrefab as T,
                                out T projectile,
                                position,
                                rotation,
                                player,
                                OnProjectileSpawn);

            return projectileSpawnStatus switch
            {
                NetworkSpawnStatus.Spawned => projectile,
                NetworkSpawnStatus.Queued => projectile,
                _ => l_DefaultProjectile()
            };

            T l_DefaultProjectile()
            {
                Debug.LogError($"Failed to spawn projectile for player {player}: {projectileSpawnStatus}");
                return default;
            }
        }

        public void OnProjectileSpawn(NetworkRunner runner, NetworkObject obj)
        {
            if (!_projectilesData.TryGetValue(obj.InputAuthority, out PlayerProjectileData playerData))
            {
                Debug.LogError($"No projectile data found for player {obj.InputAuthority}, Dat must exist before spawning a projectile.");
                return;
            }

            playerData.Projectiles.Add(obj);
        }

    }
}