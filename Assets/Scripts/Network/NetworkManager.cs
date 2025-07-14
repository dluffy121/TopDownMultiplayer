using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;
using System.Threading.Tasks;

namespace TDM
{
    public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        static NetworkManager s_Instance;

        [SerializeField] private NetworkRunner _networkRunnerPrefab;

        HashSet<NetworkRunner> _runners = new();

        private NetworkRunner _hostRunner;


        void Awake()
        {
            s_Instance = this;
        }

        void OnDestroy()
        {
            s_Instance = null;
        }

        private static NetworkRunner CreateNetworkRunner(string name)
        {
            NetworkRunner runner = Instantiate(s_Instance._networkRunnerPrefab, s_Instance.transform);
            runner.name = name;
            runner.ProvideInput = true;
            runner.AddCallbacks(s_Instance);
            return runner;
        }

        public static async Task<bool> StartGameAsync(GameMode mode)
        {
            NetworkRunner runner = CreateNetworkRunner(s_Instance.GetRunnerName(mode));

            StartGameResult result = await runner.StartGame(new()
            {
                GameMode = mode,
                SessionName = "TopDownShooter" + SessionID.ID,
                PlayerCount = 8,
                SceneManager = runner.gameObject.GetComponent<NetworkSceneManagerDefault>(),
                ObjectProvider = runner.GetComponent<PoolObjectProvider>(),
                ConnectionToken = SessionID.ByteID,
                OnGameStarted = OnGameStarted
            });

            if (!result.Ok)
                return result.Ok;

            if (mode == GameMode.Host ||
                (mode == GameMode.AutoHostOrClient && s_Instance._hostRunner == null))
                s_Instance._hostRunner = runner;

            return result.Ok;
        }

        private static void OnGameStarted(NetworkRunner runner)
        {
            GameManager.SwitchGameState(EGameState.Game, runner);
            s_Instance._runners.Add(runner);
        }

        public static void StopGame(NetworkRunner runner)
        {
            runner.Shutdown();
            Destroy(runner.gameObject);
        }

        private string GetRunnerName(GameMode mode)
        {
            return mode switch
            {
                GameMode.Shared => $"Shared_{L_RunnerCount()}",

                GameMode.Host
                or
                GameMode.AutoHostOrClient when _hostRunner == null => "Host",

                GameMode.Client
                or
                GameMode.AutoHostOrClient => $"Client_{L_RunnerCount()}",

                _ => "NetworkRunner",
            };

            int L_RunnerCount() => _runners.Count;
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
            Debug.Log($"Player {player} joined the game. {player.IsMasterClient}");
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player {player} Left the game. {player.IsMasterClient}");
        }

        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            _runners.Remove(runner);
            if (Application.isPlaying && shutdownReason != ShutdownReason.HostMigration)
            {
#if UNITY_EDITOR
                if (FusionEditorUtils.IsMultiPeerEnabled
                    && (s_Instance._runners.Count == 0 || shutdownReason == ShutdownReason.DisconnectedByPluginLogic))
#endif
                    // This is a repeated call but this time only changes state locally
                    GameManager.SwitchGameState(EGameState.MainMenu);
            }
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