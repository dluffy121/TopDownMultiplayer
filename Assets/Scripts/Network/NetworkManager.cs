using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using FStartGameResult = Fusion.StartGameResult;

namespace TDM
{
    public record StartGameResult(FStartGameResult Result, NetworkRunner NewRunner);

    public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        static NetworkManager s_Instance;

        void Awake()
        {
            s_Instance = this;
        }

        void OnDestroy()
        {
            s_Instance = null;
        }

        #region Runner Handling

        class RunnerData
        {
            public NetworkRunner runner;
            public SessionID sessionID;
        }

        [SerializeField] private NetworkRunner _networkRunnerPrefab;

        private readonly Dictionary<NetworkRunner, SessionID> _runners = new();
        private NetworkRunner _hostRunner;

        public static async Task<StartGameResult> StartGameAsync(GameMode mode,
                                                                 SessionID sessionID = null,
                                                                 HostMigrationToken hostMigrationToken = null,
                                                                 Action<NetworkRunner> onHostMigrationResume = null)
        {
            NetworkRunner runner = CreateNetworkRunner(mode);
            s_Instance._runners[runner] = sessionID ??= new();
            return new(
                await runner.StartGame(new()
                {
                    GameMode = mode,
                    SessionName = "TopDownShooter",
                    PlayerCount = 8,
                    SceneManager = runner.gameObject.GetComponent<NetworkSceneManagerDefault>(),
                    ObjectProvider = runner.GetComponent<PoolObjectProvider>(),
                    ConnectionToken = sessionID.ByteID,
                    OnGameStarted = OnGameStarted,
                    HostMigrationToken = hostMigrationToken,
                    HostMigrationResume = onHostMigrationResume,
                }),
                runner);
        }

        public static void StopGame(NetworkRunner runner)
        {
            if (runner == s_Instance._hostRunner)
                s_Instance._hostRunner = null;
            s_Instance._runners.Remove(runner);
            runner.Shutdown(true);
        }

        private static NetworkRunner CreateNetworkRunner(GameMode mode)
        {
            NetworkRunner runner = Instantiate(s_Instance._networkRunnerPrefab, s_Instance.transform);
            runner.name = s_Instance.GetRunnerName(mode);
            runner.ProvideInput = true;
            runner.AddCallbacks(s_Instance);
            return runner;
        }

        private static void OnGameStarted(NetworkRunner runner)
        {
            if (runner.GameMode == GameMode.Host ||
                (runner.GameMode == GameMode.AutoHostOrClient && s_Instance._hostRunner == null))
                s_Instance._hostRunner = runner;

            GameManager.SwitchGameState(EGameState.Game, runner);
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

        #endregion

        #region Input

        // As we know when we have multiple OnInput implementations, Fusion will call all of them but only the last input set will be honoured, therefore losing all previous inputs.
        // This forces us to have a single OnInput implementation that handles all input logic, which can become messy and hard to maintain.
        // Following implementation allows us to have multiple input listeners that can handle their own input logic separately.
        // Although this has a disadvantage of potentially having multiple listeners overriding same inputs,
        // But in this simple project this should be fine.
        // Otherwise a proper more complex input handling system can be implemented
        // where listeners can be sorted by priority or some other criteria.

        private readonly HashSet<INetworkInputListener> _inputListeners = new();

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

            s_Instance._inputListeners.Remove(listener);
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

        #region Host Migration

        private readonly HashSet<IHostMigrationListener> _hostMigrationListeners = new();

        public static void RegisterHostMigrationListener(IHostMigrationListener listener)
        {
            if (s_Instance == null)
            {
                Debug.LogError("NetworkManager instance is not initialized.");
                return;
            }

            s_Instance._hostMigrationListeners.Add(listener);
        }

        public static void UnregisterHostMigrationListener(IHostMigrationListener listener)
        {
            if (s_Instance == null)
            {
                Debug.LogError("NetworkManager instance is not initialized.");
                return;
            }

            s_Instance._hostMigrationListeners.Remove(listener);
        }

        async void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            SessionID sessionID = s_Instance._runners[runner];

            // Shutdown old Runner
            await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

            StartGameResult result = await StartGameAsync(hostMigrationToken.GameMode,
                                                          sessionID,
                                                          hostMigrationToken,
                                                          HostMigrationResume);

            if (result.Result.Ok)
                await result.NewRunner.PushHostMigrationSnapshot();
        }

        private void HostMigrationResume(NetworkRunner runnerMigration)
        {
            // Get a temporary reference for each NO from the old Host
            foreach (NetworkObject resumeNO in runnerMigration.GetResumeSnapshotNetworkObjects())
            {
                bool hasTRSP = resumeNO.TryGetBehaviour<NetworkTRSP>(out var trsp);
                Vector3 position = hasTRSP ? trsp.Data.Position : Vector3.zero;
                Quaternion rotation = hasTRSP ? trsp.Data.Rotation : Quaternion.identity;

                // Spawn a new object based on the previous objects
                NetworkObject newNO = runnerMigration.Spawn(resumeNO,
                                                            position,
                                                            rotation,
                                                            onBeforeSpawned: onBeforeSpawned);

                foreach (IHostMigrationListener listener in _hostMigrationListeners)
                    listener?.OnResumeNetworkObject(resumeNO, newNO);

                void onBeforeSpawned(NetworkRunner networkRunner, NetworkObject newNO)
                {
                    newNO.CopyStateFrom(resumeNO);

                    foreach (IHostMigrationListener listener in _hostMigrationListeners)
                        listener?.OnSpawnNetworkObject(newNO);
                }
            }

            // Updates the state information of the scene objects loaded
            // For static or baked scene objects
            foreach (var sceneObject in runnerMigration.GetResumeSnapshotNetworkSceneObjects())
            {
                sceneObject.Item1.CopyStateFrom(sceneObject.Item2);

                foreach (IHostMigrationListener listener in _hostMigrationListeners)
                    listener?.OnResumeSceneNetworkObject(sceneObject);
            }

            foreach (IHostMigrationListener listener in _hostMigrationListeners)
                listener?.OnHostMigrationResume(runnerMigration);
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
            if (Application.isPlaying)
            {
#if UNITY_EDITOR
                if (FusionEditorUtils.IsMultiPeerEnabled
                    && (s_Instance._runners.Count == 0 || shutdownReason == ShutdownReason.DisconnectedByPluginLogic))
#endif
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

                IHostMigrationListener[] hostMigrationListeners = GetComponentsInChildren<IHostMigrationListener>(true);
                Array.ForEach(hostMigrationListeners, x => RegisterHostMigrationListener(x));
            }
        }
    }
}