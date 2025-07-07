using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TDM
{
    // TODO : make more appealing
    public class UIOverheadHealth : UIBehaviour
    {
        [SerializeField] Canvas _rootCanvas;
        [SerializeField] Slider _healthSlider;

        protected override void Awake()
        {
            _rootCanvas.worldCamera = Camera.main;
        }

        internal void Set(int health)
        {
            _healthSlider.value = health;
        }
    }
}