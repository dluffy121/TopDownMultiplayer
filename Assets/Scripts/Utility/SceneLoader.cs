using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

namespace TDM
{

    public static class SceneLoader
    {
        public static Task LoadSceneAsync(int sceneIndex, LoadSceneMode loadSceneMode)
        {
            TaskCompletionSource<object> tcs = new();
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneIndex, loadSceneMode);
            op.allowSceneActivation = true;
            op.completed += _ => tcs.TrySetResult(null);
            return tcs.Task;
        }

        public static Task LoadNetworkSceneAsync(NetworkRunner runner, int sceneIndex, LoadSceneMode loadSceneMode)
        {
            if (!runner.IsSceneAuthority) return Task.CompletedTask;
            TaskCompletionSource<object> tcs = new();
            NetworkSceneAsyncOp op = runner.LoadScene(SceneRef.FromIndex(sceneIndex), loadSceneMode, setActiveOnLoad: true);
            op.AddOnCompleted(_ => tcs.TrySetResult(null));
            return tcs.Task;
        }
    }
}