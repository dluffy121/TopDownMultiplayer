using Fusion;
using UnityEngine;

namespace TDM
{
    public class Player : NetworkBehaviour, INetworkInputListener
    {
        [SerializeField] private NetworkCharacterController _charController;

        [SerializeField] private float _speed;

        [SerializeField] private ProjectileSpawner _bulletSpawner;

        private Vector3 _forward = Vector3.forward;

        void Awake()
        {
            _forward = transform.forward;
        }

        void OnEnable()
        {
            NetworkManager.RegisterForInput(this);
        }

        void OnDisable()
        {
            NetworkManager.UnregisterFromInput(this);
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (!GetInput(out NPlayerInputData inputData))
                return;

            inputData.direction.Normalize();
            _charController.Move(_speed * inputData.direction);

            if (inputData.direction.sqrMagnitude > 0)
                _forward = inputData.direction;

            if (HasStateAuthority
                && _bulletSpawner.CanSpawnProjectile(Runner, Object.InputAuthority, inputData))
            {
                _bulletSpawner.SpawnProjectile<SimpleBullet>(Runner,
                                                             Object.InputAuthority,
                                                             transform.position + _forward,
                                                             Quaternion.LookRotation(_forward));
            }
        }

        #region Input

        void INetworkInputListener.OnInput(NetworkRunner runner, ref NPlayerInputData input)
        {
            if (Input.GetKey(KeyCode.W))
                input.direction += Vector3.forward;

            if (Input.GetKey(KeyCode.S))
                input.direction += Vector3.back;

            if (Input.GetKey(KeyCode.A))
                input.direction += Vector3.left;

            if (Input.GetKey(KeyCode.D))
                input.direction += Vector3.right;
        }

        #endregion
    }
}