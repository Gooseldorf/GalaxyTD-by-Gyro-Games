public interface IUpdatable
{
    bool IsEnabled { get; set; }

    void Tick(float deltaTime);

}