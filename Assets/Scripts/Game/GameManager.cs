using UnityEngine;
using Fusion;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace TDM
{
    public enum EGameState : byte
    {
        MainMenu,
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