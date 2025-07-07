using Fusion;
using UnityEngine;

namespace TDM
{
    public class Player : NetworkBehaviour, INetworkInputListener
    {
        [SerializeField] private NetworkCharacterController _charController;
        [SerializeField] private float _speed;
        [SerializeField] private ProjectileSpawner _bulletSpawner;

        [Header("Visual")]
        [SerializeField] private Material _localMaterial;
        [SerializeField] private Renderer _renderer;

        private IWeapon[] _weapons;
        private Vector3 _forward = Vector3.forward;

        void Awake()
        {
            _forward = transform.forward;

            _weapons = GetComponentsInChildren<IWeapon>(true);
        }

        void OnEnable()
        {
            NetworkManager.RegisterForInput(this);
        }

        void OnDisable()
        {
            NetworkManager.UnregisterFromInput(this);
        }

        public override void Spawned()
        {
            base.Spawned();

            if (Runner.LocalPlayer == Object.InputAuthority)
                _renderer.material = _localMaterial;
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

            // Make sure that Host only controls firing
            if (HasStateAuthority)
                FireWeapon(inputData);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!Object.HasInputAuthority) return;

            if (other.CompareTag("Pickup")
                && other.TryGetComponent(out Pickup pickup))
                Rpc_RequestPickup(pickup.Object.Id);
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

        #region Pickup

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        internal void Rpc_RequestPickup(NetworkId pickupId, RpcInfo info = default)
        {
            NetworkObject networkObject = Runner.FindObject(pickupId);
            if (networkObject == null) return;
            if (!networkObject.TryGetComponent(out Pickup pickup)) return;
            if (!pickup.TryGetWeapon(out int weaponIndex)) return;

            RPC_AssignWeapon(weaponIndex);
        }

        #endregion

        #region Weapon

        [Networked] int WeaponIndex { get; set; } = -1;

        private void FireWeapon(NPlayerInputData inputData)
        {
            if (WeaponIndex > 0
                && _weapons[WeaponIndex].CanSpawnProjectile(Runner, Object.InputAuthority, inputData))
                _weapons[WeaponIndex].Fire(Runner);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        internal void RPC_AssignWeapon(int weaponIndex)
        {
            if (WeaponIndex > 0)
                _weapons[WeaponIndex].gameObject.SetActive(false);

            WeaponIndex = weaponIndex;

            _weapons[WeaponIndex].gameObject.SetActive(true);
        }

        #endregion
    }
}