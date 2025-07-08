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

        [SerializeField] private NetworkRunner _networkRunnerPrefab;

        private NetworkRunner _runner;
        private NetworkRunner _lastLoadedRunner;
#if UNITY_EDITOR
        private NetworkRunner _runner2;
#endif

        void Awake()
        {
            s_Instance = this;
        }

        void OnDestroy()
        {
            s_Instance = null;
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Host"))
                StartGame(GameMode.Host);
            if (s_Instance._runner && GUILayout.Button("Stop"))
                StopGame(GameMode.Host);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Join"))
                StartGame(GameMode.Client);
#if UNITY_EDITOR
            if (s_Instance._runner2 && GUILayout.Button("Stop"))
#else
            if (s_Instance._runner && GUILayout.Button("Stop"))
#endif
                StopGame(GameMode.Client);
            GUILayout.EndHorizontal();
        }

        private static NetworkRunner CreateNetworkRunner(string name)
        {
            NetworkRunner runner = Instantiate(s_Instance._networkRunnerPrefab, s_Instance.transform);
            runner.gameObject.name = name;
            return runner;
        }

        public static void StartGame(GameMode mode)
        {
            string runnerName = "NetworkRunner";
            ref NetworkRunner runner = ref s_Instance._runner;

#if UNITY_EDITOR
            runnerName = mode == GameMode.Host ? "NetworkRunner" : "NetworkRunner2";
            runner = mode == GameMode.Host ? ref s_Instance._runner : ref s_Instance._runner2;
#endif
            runner = CreateNetworkRunner(runnerName);

            SceneRef scene = SceneRef.FromIndex(1);
            NetworkSceneInfo sceneInfo = new();
            if (scene.IsValid)
                sceneInfo.AddSceneRef(scene, LoadSceneMode.Single);

            NetworkSceneManagerDefault sceneManager = runner.gameObject.GetComponent<NetworkSceneManagerDefault>();
            runner.StartGame(new()
            {
                GameMode = mode,
                SessionName = "Gameplay",
                Scene = scene,
                SceneManager = sceneManager,
                ObjectProvider = runner.GetComponent<PoolObjectProvider>()
            });

            runner.AddCallbacks(s_Instance);
        }

        private void StopGame(GameMode mode)
        {
            ref NetworkRunner runner = ref _runner;

#if UNITY_EDITOR
            runner = mode == GameMode.Host ? ref s_Instance._runner : ref s_Instance._runner2;
#endif
            if (runner == null)
                return;

            runner.Shutdown();
            Destroy(runner.gameObject);
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

        public static void RegisterForCallbacks(INetworkRunnerCallbacks callbacks)
        {
            s_Instance._lastLoadedRunner.AddCallbacks(callbacks);
        }

        public static void UnRegisterForCallbacks(INetworkRunnerCallbacks callbacks)
        {
            s_Instance._lastLoadedRunner.RemoveCallbacks(callbacks);
        }

        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        List<PlayerRef> _joinedPlayers = new();

        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player {player} joined the game. {player.IsMasterClient}");
            _joinedPlayers.Add(player);
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player {player} Left the game. {player.IsMasterClient}");
            _joinedPlayers.Remove(player);
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
            _lastLoadedRunner = runner;
            SceneManager.sceneLoaded += l_sceneLoadedCallback;

            void l_sceneLoadedCallback(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= l_sceneLoadedCallback;

                INetworkRunnerCallbacks[] networkRunnerCallbacks = GetComponentsInChildren<INetworkRunnerCallbacks>(true);
                Array.ForEach(networkRunnerCallbacks, x => runner.AddCallbacks(x));
            }
        }
    }
}