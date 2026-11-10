using System;
using UnityEngine;
using Fusion;

namespace TDM
{
    public class StandaloneProjectile : NetworkBehaviour
    {
        [SerializeField] protected float _speed = 5;

        [Networked] protected TickTimer LifeTick { get; set; }

        [SerializeField] protected Vector3 _barrelPosition;

        public event Action OnDespawn;

        public override void Spawned()
        {
            base.Spawned();

            LifeTick = TickTimer.CreateFromSeconds(Runner, 5.0f);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            OnDespawn?.Invoke();
            OnDespawn = null;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (LifeTick.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }

            transform.position += _speed * Runner.DeltaTime * transform.forward;
        }
    }
}