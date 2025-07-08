using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TDM
{
    public class PoolObjectProvider : Fusion.Behaviour, INetworkObjectProvider
    {
        [SerializeField] List<NetworkPrefabRef> prefabsToIgnore = null;
        [SerializeField, Range(0, 100)] int _maxPoolCount = 0;
        [SerializeField] List<NetworkPrefabId> prefabIdsToIgnore;
        private readonly Dictionary<NetworkPrefabId, Queue<NetworkObject>> _pools = new();

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying) return;
            prefabIdsToIgnore = prefabsToIgnore.Select(x => x.GetId()).ToList();
            EditorUtility.SetDirty(this);
        }
#endif

        public NetworkObject InstantiatePrefab(NetworkObject prefab, NetworkPrefabId prefabId)
        {
            // Get Pool and if not empty return the pooled instance
            if (_pools.TryGetValue(prefabId, out Queue<NetworkObject> pool))
            {
                if (pool.Count > 0)
                {
                    NetworkObject result = pool.Dequeue();
                    result.gameObject.SetActive(true);

                    return result;
                }
            }
            else
                // prepare empty pool to accommodate for when released
                _pools.Add(prefabId, new());

            // Since no pool, create a new prefab   
            return Instantiate(prefab);
        }

        public void DestroyPrefabInstance(NetworkPrefabId prefabId, NetworkObject instance)
        {
            // if pool is filled, destroy
            if (!_pools.TryGetValue(prefabId, out Queue<NetworkObject> pool)
                || (_maxPoolCount > 0 && pool.Count >= _maxPoolCount))
            {
                Destroy(instance.gameObject);
                return;
            }

            pool.Enqueue(instance);

            instance.gameObject.SetActive(false);
        }

        NetworkObjectAcquireResult INetworkObjectProvider.AcquirePrefabInstance(NetworkRunner runner,
                                                                                in NetworkPrefabAcquireContext context,
                                                                                out NetworkObject instance)
        {
            instance = null;

            if (prefabIdsToIgnore.Contains(context.PrefabId))
                return NetworkObjectAcquireResult.Ignore;

            NetworkObject prefab;
            try
            {
                prefab = runner.Prefabs.Load(context.PrefabId, isSynchronous: context.IsSynchronous);
            }
            catch (Exception ex)
            {
                Log.Error($"{nameof(PoolObjectProvider)}::Failed to load prefab: {ex}");
                return NetworkObjectAcquireResult.Failed;
            }

            if (!prefab)
            {
                // this is ok, as long as Fusion does not require the prefab to be loaded immediately;
                // if an instance for this prefab is still needed, this method will be called again next update
                // could happen if context.IsSynchronous is set to true
                return NetworkObjectAcquireResult.Retry;
            }

            instance = InstantiatePrefab(prefab, context.PrefabId);
            Assert.Check(instance);

            if (context.DontDestroyOnLoad)
                runner.MakeDontDestroyOnLoad(instance.gameObject);
            else
                runner.MoveToRunnerScene(instance.gameObject);

            runner.Prefabs.AddInstance(context.PrefabId);
            return NetworkObjectAcquireResult.Success;
        }

        NetworkPrefabId INetworkObjectProvider.GetPrefabId(NetworkRunner runner, NetworkObjectGuid prefabGuid)
            => runner.Prefabs.GetId(prefabGuid);

        void INetworkObjectProvider.ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
        {
            NetworkObject instance = context.Object;

            // Only pool prefabs.
            if (!context.IsBeingDestroyed)
                if (context.TypeId.IsPrefab)
                    DestroyPrefabInstance(context.TypeId.AsPrefabId, instance);
                else
                    Destroy(instance.gameObject);

            if (context.TypeId.IsPrefab)
                runner.Prefabs.RemoveInstance(context.TypeId.AsPrefabId);
        }
    }
}