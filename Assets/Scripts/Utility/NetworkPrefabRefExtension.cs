#if UNITY_EDITOR
using Fusion;
using System;

public static class NetworkPrefabRefExtensions
{
    public static NetworkPrefabId GetId(this NetworkPrefabRef prefabRef)
    {
        NetworkProjectConfig cfg = NetworkProjectConfig.Global;
        var guid = (NetworkObjectGuid)(Guid)prefabRef;
        return cfg.PrefabTable.GetId(guid);
    }
}
#endif
