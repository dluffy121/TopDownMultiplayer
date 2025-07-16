using Fusion;

namespace TDM
{
    public interface IHostMigrationListener
    {
        void OnHostMigrationResume(NetworkRunner runnerMigration) { }
        void OnResumeNetworkObject(NetworkObject resumeNO, NetworkObject newNO) { }
        void OnResumeSceneNetworkObject((NetworkObject, NetworkObjectHeaderPtr) sceneObject) { }
        void OnSpawnNetworkObject(NetworkObject newNO) { }
    }
}