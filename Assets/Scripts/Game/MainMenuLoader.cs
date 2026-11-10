using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TDM
{
    public class MainMenuLoader : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return null;

            SceneLoader.LoadSceneAsync(GameConstants.MAIN_MENU_SCENE_INDEX, LoadSceneMode.Single);
        }
    }
}