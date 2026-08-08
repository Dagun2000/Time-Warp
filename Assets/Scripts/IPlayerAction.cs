public interface IPlayerAction
{
    float Duration { get; }
    void OnStart();
    void Tick(float localTime, float dt);
    void OnEnd();
}