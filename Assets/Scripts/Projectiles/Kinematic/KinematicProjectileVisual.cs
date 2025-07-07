using Fusion;
using UnityEngine;

namespace TDM
{
    public class KinematicProjectileVisual : MonoBehaviour
    {
        internal int _projectileType;

        internal void Activate(ref KinematicData data)
        {
            transform.SetPositionAndRotation(data.Position,
                                             Quaternion.LookRotation(data.Velocity));

            data.IsFinished = false;
        }

        internal void Deactivate()
        {
            gameObject.SetActive(false);
        }

        internal void Render(ProjectileContext context, ref KinematicData current, ref KinematicData predict, float delta)
        {
            NetworkRunner runner = context.Runner;

            // TODO : Understand this
            // NOTE : For player of current client need not rely on the network time to render, thats why LocalRenderTime
            float renderTime = context.Owner == runner.LocalPlayer ? runner.LocalRenderTime : runner.RemoteRenderTime;
            transform.position = KinematicData.GetMovePosition(current, runner, renderTime / runner.DeltaTime);
        }
    }
}