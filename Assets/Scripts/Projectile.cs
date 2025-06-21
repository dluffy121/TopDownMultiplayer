using System;
using Fusion;
using UnityEngine;

namespace TDM
{
    public class Projectile : NetworkBehaviour
    {
        [SerializeField] private float _speed = 5;

        [Networked] private TickTimer _life { get; set; }

        public event Action OnDespawn;

        public override void Spawned()
        {
            base.Spawned();

            _life = TickTimer.CreateFromSeconds(Runner, 5.0f);
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

            if (_life.Expired(Runner))
                Runner.Despawn(Object);
            else
                transform.position += _speed * Runner.DeltaTime * transform.forward;
        }
    }
}