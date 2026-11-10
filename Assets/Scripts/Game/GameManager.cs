using UnityEngine;
using Fusion;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace TDM
{
    public enum EGameState : byte
    {
        MainMenu,
        Lobby,
        Game,
    }

    public class GameManager : MonoBehaviour
    {
        public static Task SwitchGameState(EGameState state, NetworkRunner runner = null)
        {
            return state switch
            {
                EGameState.MainMenu when runner
                    => Disconnect(runner),
                EGameState.MainMenu
                    => SceneLoader.LoadSceneAsync(GameConstants.MAIN_MENU_SCENE_INDEX, LoadSceneMode.Single),

                // Lobby uses Shared topology - all players connect to the same shared session
                EGameState.Lobby when runner
                    => NetworkManager.StartGameAsync(GameMode.Shared),
                EGameState.Lobby
                    => SceneLoader.LoadSceneAsync(GameConstants.LOBBY_SCENE_INDEX, LoadSceneMode.Single),

                // Game (Dungeon) uses Host-Client topology - dedicated host per dungeon instance
                EGameState.Game when !runner
                    => NetworkManager.StartGameAsync(GameMode.AutoHostOrClient),
                EGameState.Game
                    => SceneLoader.LoadNetworkSceneAsync(runner, GameConstants.GAME_SCENE_INDEX, LoadSceneMode.Single),

                _ => Task.CompletedTask
            };
        }

        private static Task Disconnect(NetworkRunner runner)
        {
            NetworkManager.StopGame(runner);
            return Task.CompletedTask;
        }
    }
}