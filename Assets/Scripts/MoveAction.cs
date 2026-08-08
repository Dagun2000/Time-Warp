using UnityEngine;

public class MoveAction : IPlayerAction
{
    private readonly Transform mover;
    private readonly Vector3 from;
    private readonly Vector3 to;
    public float Duration { get; private set; }

    public MoveAction(Transform mover, Vector3 from, Vector3 to, float moveSpeed)
    {
        this.mover = mover;
        this.from = from;
        this.to = to;
        Duration = Vector3.Distance(from, to) / moveSpeed;
    }

    public void OnStart() { }

    public void Tick(float localTime, float dt)
    {
        float t = (Duration > 0.0001f) ? Mathf.Clamp01(localTime / Duration) : 1f;
        Vector3 p = Vector3.Lerp(from, to, t);
        p.y = mover.position.y;
        mover.position = p;
    }

    public void OnEnd()
    {
        Vector3 p = to; p.y = mover.position.y;
        mover.position = p;
    }
}