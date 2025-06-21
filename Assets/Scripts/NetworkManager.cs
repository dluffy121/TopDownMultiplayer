using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;

namespace TDM
{
    public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        static NetworkManager s_Instance;

        private NetworkRunner _runner;

        void Awake()
        {
            s_Instance = this;
        }

        void OnDestroy()
        {
            s_Instance = null;

            if (_runner != null)
            {
                _runner.Shutdown();
                Destroy(_runner);
            }
        }

        private void OnGUI()
        {
            if (_runner != null) return;

            if (GUILayout.Button("Host"))
                StartGame(GameMode.Host);
            if (GUILayout.Button("Join"))
                StartGame(GameMode.Client);
        }

        private void Initialize()
        {
            if (_runner != null) return;

            _runner = gameObject.AddComponent<NetworkRunner>();
            s_Instance._runner.ProvideInput = true;
            DontDestroyOnLoad(gameObject);
        }

        public static void StartGame(GameMode mode)
        {
            s_Instance.Initialize();

            SceneRef scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            NetworkSceneInfo sceneInfo = new();
            if (scene.IsValid)
                sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);

            s_Instance._runner.StartGame(new()
            {
                GameMode = mode,
                SessionName = "Gameplay",
                Scene = scene,
                SceneManager = s_Instance.gameObject.AddComponent<NetworkSceneManagerDefault>()
            });
        }

        #region Input

        // As we know when we have multiple OnInput implementations, Fusion will call all of them but only the last input set will be honoured, therefore losing all previous inputs.
        // This forces us to have a single OnInput implementation that handles all input logic, which can become messy and hard to maintain.
        // Following implementation allows us to have multiple input listeners that can handle their own input logic separately.
        // Although this has a disadvantage of potentially having multiple listeners overriding same inputs,
        // But in this simple project this should be fine.
        // Otherwise a proper more complex input handling system can be implemented
        // where listeners can be sorted by priority or some other criteria.

        private HashSet<INetworkInputListener> _inputListeners = new();

        public static void RegisterForInput(INetworkInputListener listener)
        {
            if (s_Instance == null)
            {
                Debug.LogError("NetworkManager instance is not initialized.");
                return;
            }

            s_Instance._inputListeners.Add(listener);
        }

        public static void UnregisterFromInput(INetworkInputListener listener)
        {
            if (s_Instance == null)
            {
                Debug.LogError("NetworkManager instance is not initialized.");
                return;
            }

            s_Instance._inputListeners.Add(listener);
        }

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {
            NPlayerInputData playerInputData = new();

            foreach (INetworkInputListener listener in _inputListeners)
                listener.OnInput(runner, ref playerInputData);

            input.Set(playerInputData);
        }

        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            Debug.LogWarning($"Input missing for player {player}. This can happen if the player is not sending input or if the input is not being processed correctly.");
        }

        #endregion

        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
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