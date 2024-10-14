public interface IMovable : IPosition
{
    float CurrentSpeed { get; }
    bool IsGoingIn { get; }
}