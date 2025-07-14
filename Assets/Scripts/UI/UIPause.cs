using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace TDM
{
    public class UIPause : MonoBehaviour
    {
        [SerializeField] Canvas _canvas;
        [SerializeField] GraphicRaycaster _raycaster;
        [SerializeField] GameObject[] _goToToggle;

        public void ShowUI()
        {
            _canvas.enabled = _raycaster.enabled = true;
        }

        public void HideUI()
        {
            _canvas.enabled = _raycaster.enabled = false;
        }

        public void OnClick_Pause()
        {
            foreach (GameObject go in _goToToggle)
                go.SetActive(false);
            ShowUI();
        }

        public void OnClick_Resume()
        {
            HideUI();
            foreach (GameObject go in _goToToggle)
                go.SetActive(true);
        }

        public void OnClick_GoToMainMenu()
        {
            NetworkRunner runner = NetworkRunner.GetRunnerForGameObject(gameObject);
            GameManager.SwitchGameState(EGameState.MainMenu, runner);

#if UNITY_EDITOR
            if (FusionEditorUtils.IsMultiPeerEnabled)
                _canvas.enabled = _raycaster.enabled = false;
#endif
        }
    }
}