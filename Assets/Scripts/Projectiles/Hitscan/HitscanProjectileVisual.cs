using UnityEngine;

namespace TDM
{
    public class HitscanProjectileVisual : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _impactEffect;
        [SerializeField] private float _lifetime = 1f;

        internal int _projectileType;

        public bool IsFinished { get; private set; }

        internal void Activate(ref HitscanData data)
        {
            // TODO : Implement activation logic
            IsFinished = false;

            Fusion.Vector3Compressed impactPosition = data.ImpactPosition == default ? data.FirePosition : data.ImpactPosition;
            transform.SetPositionAndRotation(impactPosition,
                                             Quaternion.LookRotation(data.FireDirection));

            // Spawn impact
        }

        internal void Deactivate()
        {
            gameObject.SetActive(false);
        }

        internal void Render(ProjectileContext context, ref HitscanData current, ref HitscanData predict, float delta)
        {
            // TODO : Implement rendering logic, spawn some particles, trail

            // Render 1 frame at least
            if (_lifetime <= 0f)
            {
                IsFinished = true;
                return;
            }

            // Its not wise to use 'delta' argument here, as it can get called multiple times per frame
            _lifetime -= Time.deltaTime;
        }
    }
}