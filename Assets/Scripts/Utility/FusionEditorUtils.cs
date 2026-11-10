#if UNITY_EDITOR
using static Fusion.NetworkProjectConfig;

public static class FusionEditorUtils
{
    public static bool IsMultiPeerEnabled
        => Global.PeerMode == PeerModes.Multiple;
}
#endif