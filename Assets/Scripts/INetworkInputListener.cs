using Fusion;

namespace TDM
{
    public interface INetworkInputListener
    {
        void OnInput(NetworkRunner runner, ref NPlayerInputData input);
    }
}