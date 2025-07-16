using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace TDM
{
    public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks, IHostMigrationListener
    {
        [SerializeField] private NetworkPrefabRef _playerPrefab;

        [Networked] public byte PlayerMatsApplied { get; set; } = 0;

        private readonly Dictionary<long, NetworkObject> _players = new();

        void OnEnable()
        {
            NetworkRunner runner = NetworkRunner.GetRunnerForGameObject(gameObject);
            runner?.AddCallbacks(this);
            NetworkManager.RegisterHostMigrationListener(this);
        }

        void OnDisable()
        {
            NetworkManager.UnregisterHostMigrationListener(this);
            NetworkRunner runner = NetworkRunner.GetRunnerForGameObject(gameObject);
            runner?.RemoveCallbacks(this);
        }

        #region Spawning

        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            // Shared
            if (runner.GameMode == GameMode.Shared)
            {
                if (runner.LocalPlayer == player)
                    SpawnPlayer(runner, player);

                return;
            }

            // Host-Client
            if (!runner.CanSpawn) return;

            byte[] playerToken = runner.GetPlayerConnectionToken(player);
            long playerID = SessionID.ConvertID(playerToken);

            if (!_players.TryGetValue(playerID, out NetworkObject playerObject)
                || playerObject == null)
            {
                playerObject = SpawnPlayer(runner,
                                            player,
                                            onBeforeSpawned);

                void onBeforeSpawned(NetworkRunner runner, NetworkObject no)
                {
                    Player playerObj = no.GetBehaviour<Player>();
                    playerObj.Token = SessionID.ConvertID(playerToken);
                    Log.Debug($"Spawn PlayerBehaviour: {playerObj.Token}");
                }
            }

            _players[playerID] = playerObject;
            playerObject.AssignInputAuthority(player);
            Player playerRef = playerObject.GetBehaviour<Player>();
            playerRef.CheckLocalPlayer();
            playerRef.OnMasterClientOrHostChange();

            PushNewSnapshot(runner);
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            DespawnPlayer(runner, player);
        }

        private NetworkObject SpawnPlayer(NetworkRunner runner, PlayerRef player, NetworkRunner.OnBeforeSpawned onBeforeSpawned = null)
        {
            if (!runner.IsServer) return null;

            onBeforeSpawned += (r, no) =>
            {
                if (r.GetPlayerObject(player) != no)
                    r.SetPlayerObject(player, no);
                Player playerRef = no.GetBehaviour<Player>();
                playerRef.PlayerMatIndex = PlayerMatsApplied++;
            };

            Vector3 spawnPos = new(player.RawEncoded % runner.Config.Simulation.PlayerCount * 3, 1, 0);
            NetworkObject networkObject = runner.Spawn(_playerPrefab,
                                                       spawnPos,
                                                       Quaternion.identity,
                                                       player,
                                                       onBeforeSpawned);

            if (runner.LocalPlayer == player)
                CameraController.AddTarget(networkObject.transform, runner.LocalPlayer == player);

            return networkObject;
        }

        public void DespawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            runner.Despawn(runner.GetPlayerObject(player));
        }

        private static async void PushNewSnapshot(NetworkRunner runner)
        {
            try
            {
                Debug.Log($"Pushing new Snapshot");
                bool result = await runner.PushHostMigrationSnapshot();
                Debug.Log($"New Snapshot: {result}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error Pushing New Snapshot");
                Debug.LogException(e);
            }
        }

        #endregion

        #region Host Migration

        void IHostMigrationListener.OnSpawnNetworkObject(NetworkObject newNO)
        {
            if (newNO.TryGetBehaviour(out Player playerBehaviour))
            {
                newNO.AssignInputAuthority(PlayerRef.None);

                // Store mapping between Token and NetworkObject
                _players[playerBehaviour.Token] = newNO;
            }
        }

        #endregion

        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }


        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
        }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
        }

        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
        }

        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
        }

        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
        }

        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
        }

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {
        }

        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {
        }

        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
        }

        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
        }

        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
        {
        }

        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
        {
        }
    }
}