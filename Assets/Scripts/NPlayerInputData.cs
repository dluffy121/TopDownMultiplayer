using Fusion;
using UnityEngine;

namespace TDM
{
    public struct NPlayerInputData : INetworkInput
    {
        public const byte MOUSE_BUTTON_0 = 1;

        public NetworkButtons buttons;
        public Vector3 direction;
    }
}