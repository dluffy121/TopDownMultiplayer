using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace TDM
{
    public class PickupSpawner : NetworkBehaviour
    {
        [SerializeField] private Pickup[] _pickupPrefabs;
        [SerializeField] private float _minDistance = 2f;
        [SerializeField] private Vector3 _areaCenter = Vector3.up * 0.5f;
        [SerializeField] private Vector3 _areaSize = new(10, 1, 10);

        private List<Vector3> _spawnedPositions = new(20);

        float _minDstSqr;

        public override void Spawned()
        {
            base.Spawned();

            _minDstSqr = _minDistance * _minDistance;

            if (Object.HasStateAuthority)
            {
                _spawnedPositions.Clear();
                SpawnPickups(5);
            }
        }

        public void SpawnPickups(int count)
        {
            int attempts = 0;
            while (_spawnedPositions.Count < count && attempts < count * 10)
            {
                Vector3 spawnPos = new(
                    _areaCenter.x + Random.Range(-_areaSize.x / 2, _areaSize.x / 2),
                    _areaCenter.y,
                    _areaCenter.z + Random.Range(-_areaSize.z / 2, _areaSize.z / 2)
                );

                bool tooClose = false;
                foreach (Vector3 pos in _spawnedPositions)
                {
                    if (Vector3.SqrMagnitude(pos - spawnPos) < _minDstSqr)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    _spawnedPositions.Add(spawnPos);
                    int i = Random.Range(0, _pickupPrefabs.Length);
                    Runner.Spawn(_pickupPrefabs[i], spawnPos, Quaternion.identity);
                }
                else
                    attempts++;
            }

            if (_spawnedPositions.Count < count)
                Debug.LogWarning($"Could only spawn {_spawnedPositions.Count}/{count} pickups.");
        }
    }
}
