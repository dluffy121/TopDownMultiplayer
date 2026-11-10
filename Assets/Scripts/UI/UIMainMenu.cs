using System;
using UnityEngine;
using Fusion;
using System.Threading.Tasks;
using UnityEngine.UI;
using Fusion.Editor;

namespace TDM
{
    public class UIMainMenu : MonoBehaviour
    {
        [SerializeField] Canvas _canvas;
        [SerializeField] GraphicRaycaster _raycaster;
        [SerializeField] CanvasGroup _canvasGroup;

        GameMode _selectedMode;

        public async void OnClick_StartGame()
        {
            try
            {
                _canvasGroup.interactable = false;

                await GameManager.SwitchGameState(EGameState.Game);

#if UNITY_EDITOR
                if (!FusionEditorUtils.IsMultiPeerEnabled)
                    return;

                _canvasGroup.interactable = true;
                _canvas.enabled = _raycaster.enabled = false;
#endif
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void OnValueChange_ChangeMode(int mode)
        {
            _selectedMode = (GameMode)mode;
        }
    }
}