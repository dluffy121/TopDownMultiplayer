using UnityEngine;
using Fusion;
using System;
using System.Collections.Generic;

namespace TDM
{
    /// <summary>
    /// Handles Spawning, is added to a weapon instance
    /// </summary>
    public class KinematicProjectilesHandler : NetworkBehaviour
    {
        class VisualEntry
        {
            public KinematicProjectileVisual Visual;
            public KinematicData LastData;
        }

        [SerializeField] protected KinematicProjectile[] _projectiles;

        /// <summary>
        /// Projectile Data array needs to be replicated
        /// </summary>
        /// <value></value>
        [Networked, Capacity(64)]
        protected NetworkArray<KinematicData> DataBuffer { get; }

        /// <summary>
        /// Count of ProjectileData in <see cref="DataBuffer"/>
        /// </summary>
        /// <value></value>
        [Networked]
        protected int BufferCount { get; set; }

        /// <summary>
        /// Reader to read from the <see cref="DataBuffer"/> from snapshots
        /// </summary>
        private ArrayReader<KinematicData> _bufferReader;

        /// <summary>
        /// Reader to read from the <see cref="BufferCount"/> from snapshots
        /// </summary>
        private PropertyReader<int> _bufferCountReader;

        private Dictionary<int, VisualEntry> _spawnedVisuals = new();

        /// <summary>
        /// Count of spawned data on the client side
        /// </summary>
        private int _spawnedDataCount;

        public ProjectileContext Context => new()
        {
            Runner = Runner,
            Owner = Object.InputAuthority
        };

        public void SpawnProjectile(KinematicProjectile projectile, Vector3 startPosition, Vector3 direction)
        {
            int projectileType = Array.IndexOf(_projectiles, projectile);

            if (projectileType < 0)
            {
                Debug.LogError($"{nameof(KinematicProjectilesHandler)} :: Added projectile type is not added to list");
                return;
            }

            KinematicData data = projectile.GetKinematicData(Context, startPosition, direction);

            data.FireTick = Runner.Tick;
            data.ProjectileType = (byte)projectileType;
            data.IsFinished = false;

            AddDataToBuffer(data);
        }

        public override void Spawned()
        {
            _spawnedDataCount = BufferCount;

            // create databuffer and its count reader
            _bufferReader = GetArrayReader<KinematicData>(nameof(DataBuffer));
            _bufferCountReader = GetPropertyReader<int>(nameof(BufferCount));
        }

        public override void FixedUpdateNetwork()
        {
            for (int i = 0; i < DataBuffer.Length; i++)
            {
                KinematicData data = DataBuffer[i];
                KinematicProjectile projectile = _projectiles[data.ProjectileType];

                // Ignore if Projectile has hit/dead
                if (data.IsFinished) continue;
                if (data.FireTick == 0) continue;

                projectile.OnFixedUpdate(Context, ref data);

                DataBuffer.Set(i, data);
            }
        }

        public override void Render()
        {
            // Visuals are not processed on dedicated server at all
            if (Runner.Mode == SimulationModes.Server)
                return;

            if (!TryGetSnapshotsBuffers(out NetworkBehaviourBuffer current,
                                        out NetworkBehaviourBuffer predict,
                                        out float delta))
            {
                Debug.LogWarning("Failed to get snapshots");
                return;
            }


            // NOTE :
            // Ideally it should not be defined 'Current' and 'Predict' as the Snapshots may not represent the current tick of the game i.e. frame
            // In terms of slow connection many ticks may have to be processed, to keep the local systems and state updated
            // Current - Current State of Network, buffer and count is what server CURRENTLY HAS
            // Predict - Predicted State of Network, buffer and count could be what server MAY HAVE by the end of this tick
            // We say 'MAY' because as per client side it is certain that a new data will be created incase of Fire Action
            // But if the network fails for some reason, they data may not be received by the server
            // In any case we CHOOSE to keep the client visual updated

            NetworkArrayReadOnly<KinematicData> currentBuffer = _bufferReader.Read(current);
            int currentCount = _bufferCountReader.Read(current);
            NetworkArrayReadOnly<KinematicData> predictBuffer = _bufferReader.Read(predict);
            int predictCount = _bufferCountReader.Read(predict);

            int bufferLength = DataBuffer.Length;

            SyncClientState(currentBuffer, currentCount, bufferLength);

            Render(currentBuffer,
                   predictBuffer,
                   predictCount,
                   delta,
                   bufferLength);

            RemoveFinishedVisuals();

            _spawnedDataCount = currentCount;
        }

        private void AddDataToBuffer(KinematicData data)
        {
            // Index to be set must never exceed Buffer length, hence the %
            // Incase the data overriding is visible, should increase the Buffer "Capacity"
            DataBuffer.Set(BufferCount % DataBuffer.Length, data);

            BufferCount++;
        }

        private void Render(NetworkArrayReadOnly<KinematicData> currentBuffer,
                            NetworkArrayReadOnly<KinematicData> predictBuffer,
                            int predictCount,
                            float delta,
                            int bufferLength)
        {
            // Render those projectiles' data visuals

            // Defines an index from which the visuals should be rendered based on predicted data
            // This improves the visuals and keeps it more accurate for the next tick
            int minVisualIdx = predictCount - bufferLength;

            foreach (KeyValuePair<int, VisualEntry> idxVisualPair in _spawnedVisuals)
            {
                int index = idxVisualPair.Key;
                KinematicProjectileVisual visual = idxVisualPair.Value.Visual;

                // for data which MAY be spawned, we can provide the predicted data
                if (index >= minVisualIdx)
                {
                    int bufferIndex = index % bufferLength;
                    KinematicData currentData = currentBuffer[bufferIndex];
                    KinematicData predictData = predictBuffer[bufferIndex];
                    visual.Render(Context, ref currentData, ref predictData, delta);
                    idxVisualPair.Value.LastData = predictData;
                }
                else
                    visual.Render(Context, ref idxVisualPair.Value.LastData, ref idxVisualPair.Value.LastData, 0f);
            }
        }

        private void SyncClientState(NetworkArrayReadOnly<KinematicData> fromBuffer, int fromCount, int bufferLength)
        {
            // Assume that in the current tick the spawned projectile visuals are not the same as server controlled visuals

            RemoveExtraVisuals(fromCount);
            SpawnNewVisuals(fromBuffer, fromCount, bufferLength);
        }

        private void SpawnNewVisuals(NetworkArrayReadOnly<KinematicData> fromBuffer, int fromCount, int bufferLength)
        {
            // If the visuals on client side are less than the ones on the server we spawn more
            // Read the buffer from point that has locally un-spawned data
            for (int i = _spawnedDataCount; i < fromCount; i++)
            {
                // Again making sure the index doesn't exceed the buffer length
                KinematicData data = fromBuffer[i % bufferLength];

                KinematicProjectileVisual projectileVisual = GetProjectileVisual(data);

                _spawnedVisuals[i] = new() { Visual = projectileVisual };
            }
        }

        private void RemoveExtraVisuals(int fromCount)
        {
            // If the visuals on client side are more than the ones on the server we remove them
            for (int i = fromCount; i < _spawnedDataCount; i++)
            {
                if (!_spawnedVisuals.TryGetValue(i, out VisualEntry visualEntry))
                    continue;

                RemoveProjectileVisual(visualEntry.Visual);

                _spawnedVisuals.Remove(i);
            }
        }

        private void RemoveFinishedVisuals()
        {
            List<int> finishedVisual = new();

            foreach (KeyValuePair<int, VisualEntry> idxVisualPair in _spawnedVisuals)
                if (idxVisualPair.Value.LastData.IsFinished)
                    finishedVisual.Add(idxVisualPair.Key);

            foreach (int index in finishedVisual)
            {
                RemoveProjectileVisual(_spawnedVisuals[index].Visual);
                _spawnedVisuals.Remove(index);
            }
        }

        private KinematicProjectileVisual GetProjectileVisual(KinematicData data)
        {
            var projectileVisual = _projectiles[data.ProjectileType].GetVisualInstance();

            Runner.MoveToRunnerScene(projectileVisual);
            if (Runner.Config.PeerMode == NetworkProjectConfig.PeerModes.Multiple)
                Runner.AddVisibilityNodes(projectileVisual.gameObject);

            projectileVisual.Activate(ref data);

            return projectileVisual;
        }

        private void RemoveProjectileVisual(KinematicProjectileVisual visual)
        {
            visual.Deactivate();

            _projectiles[visual._projectileType].RemoveVisualInstance(visual);
        }
    }
}