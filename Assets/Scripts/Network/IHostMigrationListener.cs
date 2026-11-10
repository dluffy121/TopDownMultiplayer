using Fusion;

namespace TDM
{
    public interface IHostMigrationListener
    {
        void OnHostMigrationResume(NetworkRunner runnerMigration) { }
        void OnResumeNetworkObject(NetworkRunner runnerMigration, NetworkObject resumeNO, NetworkObject newNO) { }
        void OnResumeSceneNetworkObject((NetworkObject, NetworkObjectHeaderPtr) sceneObject) { }
        void OnSpawnNetworkObject(NetworkRunner runnerMigration, NetworkObject resumeNO, NetworkObject newNO) { }
    }
}