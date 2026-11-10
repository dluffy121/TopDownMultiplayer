namespace TDM
{
    public interface IHitTarget
    {
        bool IsAlive { get; }
        bool TryTakeHit(ref HitData hit);

        // TODO : Impact Type
    }
}