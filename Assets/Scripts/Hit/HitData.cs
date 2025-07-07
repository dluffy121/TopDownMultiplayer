using UnityEngine;

namespace TDM
{
    public ref struct HitData
    {
        public IHitInstigator Instigator;
        public IHitTarget Target;

        public Vector3 Point;
        public Vector3 Direction;
        public Vector3 Normal;

        public byte Damage;
        public EHitType HitType;
    }
}