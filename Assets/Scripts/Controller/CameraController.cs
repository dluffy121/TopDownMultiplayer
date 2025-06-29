using Unity.Cinemachine;
using UnityEngine;

namespace TDM
{
    public class CameraController : CinemachineTargetGroup
    {
        static CameraController s_instance;

        [SerializeField] float _playerTargetRadius = .75f;
        [SerializeField] float _playerDetectRadius = 50f;
        [SerializeField] AnimationCurve _playerDetectWeightCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        Transform _playerTarget;

        void Awake()
        {
            s_instance = this;
        }

        void OnDestroy()
        {
            s_instance = null;
        }

        public static void AddTarget(Transform target, bool isPlayer)
        {
            if (s_instance == null) return;

            if (isPlayer)
                s_instance._playerTarget = target;

            s_instance.AddMember(target,
                                 1f,
                                 isPlayer ? s_instance._playerTargetRadius : .5f);
        }

        public void RemoveTarget(Transform target)
        {
            if (s_instance == null) return;

            s_instance.RemoveMember(target);
        }

        public void LateUpdate()
        {
            if (Targets.Count == 0) return;

            Target playerTarget = Targets[0];

            for (int i = 1; i < Targets.Count; i++)
            {
                Target target = Targets[i];
                float dist = Vector3.Distance(playerTarget.Object.position, target.Object.position);
                float time = Mathf.InverseLerp(0, _playerDetectRadius, dist);
                target.Weight = _playerDetectWeightCurve.Evaluate(time);
            }
        }
    }
}