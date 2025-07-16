using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TDM
{
    public class UIPause : MonoBehaviour
    {
        [SerializeField] Canvas _canvas;
        [SerializeField] GraphicRaycaster _raycaster;
        [SerializeField] GameObject[] _goToToggle;

        [SerializeField] TextMeshProUGUI _playerInfo;

        NetworkRunner _runner;
        NetworkRunner Runner => _runner ?? NetworkRunner.GetRunnerForGameObject(gameObject);

        public void ShowUI()
        {
            _canvas.enabled = _raycaster.enabled = true;
            _playerInfo.text = Runner.LocalPlayer.PlayerId + " | " + (Runner.IsServer ? "Host" : "Client");
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
            GameManager.SwitchGameState(EGameState.MainMenu, Runner);

#if UNITY_EDITOR
            if (FusionEditorUtils.IsMultiPeerEnabled)
                _canvas.enabled = _raycaster.enabled = false;
#endif
        }
    }
}