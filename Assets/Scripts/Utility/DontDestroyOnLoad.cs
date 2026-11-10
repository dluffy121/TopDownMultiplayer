using UnityEngine;

namespace TDM
{
    [DisallowMultipleComponent]
    public class DontDestroyOnLoad : MonoBehaviour
    {
        public void Start() =>
            DontDestroyOnLoad(gameObject);
    }
}
