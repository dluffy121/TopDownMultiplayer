using Fusion;
using UnityEngine;

namespace TDM
{
    public class KinematicWeapon : MonoBehaviour, INetworkInputListener
    {
        [SerializeField] KinematicProjectile _projectile;
        [SerializeField] Transform _fireOffset;
        [SerializeField] KeyCode _inputKey = KeyCode.Mouse0;
        [SerializeField] float _fireRate = 5f / 1f;
        [SerializeField] KinematicProjectilesHandler _projectileHandler;

        TickTimer NextSpawnTime;
        private bool _keyState_Fire = false;

        void OnEnable()
        {
            NetworkManager.RegisterForInput(this);
        }

        void OnDisable()
        {
            NetworkManager.UnregisterFromInput(this);
        }

        private void Update()
        {
            _keyState_Fire |= Input.GetKeyDown(_inputKey);
        }

        #region Input

        void INetworkInputListener.OnInput(NetworkRunner runner, ref NPlayerInputData input)
        {
            input.buttons.Set(NPlayerInputData.MOUSE_BUTTON_0, _keyState_Fire);
            _keyState_Fire = false;
        }

        #endregion

        public void Fire(NetworkRunner runner)
        {
            _projectileHandler.SpawnProjectile(_projectile, _fireOffset.position, _fireOffset.forward);
            NextSpawnTime = TickTimer.CreateFromSeconds(runner, 1f / _fireRate);
        }

        public bool CanSpawnProjectile(NetworkRunner runner, PlayerRef player, NPlayerInputData inputData)
        {
            if (!runner.IsServer) return false;

            if (!inputData.buttons.IsSet(NPlayerInputData.MOUSE_BUTTON_0))
                return false;

            return NextSpawnTime.ExpiredOrNotRunning(runner);
        }
    }
}