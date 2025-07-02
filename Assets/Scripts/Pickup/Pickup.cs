using Fusion;
using UnityEngine;

namespace TDM
{
    public class Pickup : NetworkBehaviour
    {
        [SerializeField] private int _weaponIndex;

        [SerializeField] string _playerTag = "Player";

        [Networked] bool IsPickedUp { get; set; }

        public bool TryGetWeapon(out int weaponIndex)
        {
            weaponIndex = -1;
            if (Object == null) return false;
            if (!Runner.Exists(Object)) return false;
            if (IsPickedUp) return false;

            IsPickedUp = true;
            weaponIndex = _weaponIndex;

            Runner.Despawn(Object);

            return true;
        }
    }
}