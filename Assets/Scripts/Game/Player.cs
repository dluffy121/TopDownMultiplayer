using Fusion;
using UnityEngine;

namespace TDM
{
    public class Player : NetworkBehaviour, INetworkInputListener, IHitTarget, IHitInstigator
    {
        public static Player LocalPlayer { get; private set; }

        [Networked, OnChangedRender(nameof(OnMasterClientOrHostChange)), Tooltip("Is this player the shared mode master client?")]
        public NetworkBool IsSharedMasterClient { get; set; }

        [Networked, OnChangedRender(nameof(OnMasterClientOrHostChange)), Tooltip("Is this player Player Host?")]
        public NetworkBool IsHostClient { get; set; }

        [Networked, Tooltip("The Remote Client's Connection Token Hash.")]
        public long Token { get; set; }

        [SerializeField] private float _despawnDelay = 5;

        [SerializeField] private NetworkCharacterController _charController;
        [SerializeField] private float _speed;
        [SerializeField] private ProjectileSpawner _bulletSpawner;

        [Header("Health")]
        [Networked, OnChangedRender(nameof(OnHealthUpdate))]
        private byte Health { get; set; } = 0;
        [SerializeField] private UIOverheadHealth _UIhealth;

        [Header("Visual")]
        [Networked, OnChangedRender(nameof(OnPlayerColorChanged))]
        public byte PlayerMatIndex { get; set; } = 0;
        [SerializeField] private Material[] _playerMaterials;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Renderer _crownRenderer;

        private PlayerSpawner _spawnerRef;
        private float _despawnTime;
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

        void OnDestroy()
        {
            _renderer.material = null;
        }

        public override void Spawned()
        {
            base.Spawned();

            CheckLocalPlayer();

            OnPlayerColorChanged();

            Health = 100;
            OnHealthUpdate();

            OnMasterClientOrHostChange();
        }

        public void CheckLocalPlayer()
        {
            if (Runner.GameMode == GameMode.Shared)
            {
                if (HasStateAuthority)
                {
                    LocalPlayer = this;
                    IsSharedMasterClient = Runner.IsSharedModeMasterClient;
                }
            }
            else
            {
                if (HasInputAuthority)
                {
                    LocalPlayer = this;
                    IsHostClient = Runner.IsServer;
                }
            }
        }

        public void OnMasterClientOrHostChange()
        {
            bool isSMCOrHost = IsSharedMasterClient || IsHostClient;
            _crownRenderer.gameObject.SetActive(isSMCOrHost);
        }

        private void OnPlayerColorChanged()
        {
            _renderer.material = _playerMaterials[PlayerMatIndex];
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (GetInput(out NPlayerInputData inputData))
                UpdateInput(inputData);

            // This is only done in Client Host Mode, but gives players an period of time to try and reconnect before being despawned.
            if (Runner.IsPlayerValid(Object.InputAuthority))
                return;

            IsHostClient = false;

            _despawnTime += Runner.DeltaTime;
            if (_despawnTime >= _despawnDelay)
                Runner.Despawn(Object);
        }

        private void UpdateInput(NPlayerInputData inputData)
        {
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
                RPC_RequestPickup(pickup.Object.Id);
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
        internal void RPC_RequestPickup(NetworkId pickupId, RpcInfo info = default)
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
            if (WeaponIndex >= 0
                && _weapons[WeaponIndex].CanSpawnProjectile(Runner, Object.InputAuthority, inputData))
                _weapons[WeaponIndex].Fire(Runner);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        internal void RPC_AssignWeapon(int weaponIndex)
        {
            if (WeaponIndex >= 0)
                _weapons[WeaponIndex].gameObject.SetActive(false);

            WeaponIndex = weaponIndex;

            _weapons[WeaponIndex].gameObject.SetActive(true);
        }

        #endregion

        #region IHitTarget

        public bool IsAlive => Health > 0;

        bool IHitTarget.TryTakeHit(ref HitData hit)
        {
            if (!IsAlive)
                return false;

            if (Object.HasStateAuthority)
                Health -= hit.Damage;
            else if (Object.IsProxy)
                RPC_TakeHit(hit.Damage);

            return true;
        }

        /// <summary>
        /// RPC call to update state authority of this objects Health
        /// At this point InputAuthority is the one who is inflicting hit, could be host or client
        /// Also the StateAuthority doesn't know of this interaction and hence is nowhere in the scope 
        /// Hence it is assumed that the Hit taker is a Proxy
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="info"></param>
        [Rpc(RpcSources.Proxies, RpcTargets.StateAuthority)]
        void RPC_TakeHit(byte damage, RpcInfo info = default)
        {
            Health -= damage;
        }

        #endregion

        #region IHitInstigator

        public void HitPerformed(HitData hit)
        {

        }

        #endregion

        #region Health

        /// <summary>
        /// Updates health UI of the player
        /// </summary>
        private void OnHealthUpdate()
        {
            _UIhealth.Set(Health);
        }

        #endregion
    }
}