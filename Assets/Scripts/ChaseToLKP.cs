using UnityEngine;

public class ChaseToLKP : MonoBehaviour, IEnemyAction
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 2f;      // 플레이어(4)의 절반
    [SerializeField] private float arriveDist = 0.1f;

    [Header("도착 후")]
    [SerializeField] private float lookAroundTime = 1.5f; // 두리번거리는 시간(적 시간 기준)

    private enum Phase { Chasing, LookingAround, Returning }
    private Phase phase = Phase.Chasing;
    private float lookTimer = 0f;

    public EnemyActionType ActionType => EnemyActionType.WhenLost;

    public void Tick(EnemyBrain brain, float dt)
    {
        switch (phase)
        {
            case Phase.Chasing: TickChase(brain, dt); break;
            case Phase.LookingAround: TickLook(brain, dt); break;
            case Phase.Returning: TickReturn(brain, dt); break;
        }
    }

    // LKP로 이동
    void TickChase(EnemyBrain brain, float dt)
    {
        if (!brain.HasKnownPosition) { phase = Phase.Returning; return; }

        Vector3 target = brain.LastKnownPosition;
        if (MoveToward(target, dt))   // 도착하면 true
        {
            phase = Phase.LookingAround;
            lookTimer = 0f;
        }
    }

    // 도착해서 두리번 (제자리 회전)
    void TickLook(EnemyBrain brain, float dt)
    {
        lookTimer += dt;
        Debug.Log($"두리번 중: lookTimer {lookTimer:F2} / {lookAroundTime}, dt {dt:F4}");
        transform.Rotate(0f, 90f * dt, 0f);

        if (lookTimer >= lookAroundTime)
            phase = Phase.Returning;
    }

    // home으로 복귀
    void TickReturn(EnemyBrain brain, float dt)
    {
        Debug.Log($"복귀 중: 현재 {transform.position}, home {brain.HomePosition}, 거리 {Vector3.Distance(transform.position, brain.HomePosition):F2}");
        if (MoveToward(brain.HomePosition, dt))
        {
            phase = Phase.Chasing;
            brain.ReturnToPeace();
        }
    }

    // 공통 이동 헬퍼. 도착하면 true 반환.
    [SerializeField] private float turnSpeed = 360f;  // 초당 회전 각도

    bool MoveToward(Vector3 target, float dt)
    {
        Vector3 pos = transform.position;
        target.y = pos.y;
        Vector3 to = target - pos;
        float dist = to.magnitude;

        if (dist <= arriveDist) return true;

        float step = moveSpeed * dt;
        transform.position = pos + to.normalized * Mathf.Min(step, dist);

        // 즉시 스냅 대신 부드럽게 회전
        if (to.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(new Vector3(to.x, 0, to.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * dt);
        }
        return false;
    }

    // 빨강 전환(플레이어 다시 보임) 등으로 이 행동이 꺼질 때
    public void OnIdle()
    {
        // 비워둠 — phase는 Tick 안에서만 바뀜
    }
}