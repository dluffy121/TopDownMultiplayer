using UnityEngine;

namespace TDM
{
    public class LockTransform : MonoBehaviour
    {
        public Vector3 _initialPosOffset;
        public Quaternion _initialRot;

        void Awake()
        {
            _initialPosOffset = transform.localPosition;
            _initialRot = transform.rotation;
        }

        void LateUpdate()
        {
            transform.SetPositionAndRotation(transform.parent.position + _initialPosOffset,
                                             _initialRot);
        }
    }
}