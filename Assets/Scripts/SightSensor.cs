using UnityEngine;

public class SightSensor : MonoBehaviour, ISensor
{
    [Header("시야 설정")]
    [SerializeField] private float viewRadius = 6f;
    [SerializeField, Range(0, 360)] private float viewAngle = 90f;
    [SerializeField] private Transform target;
    [SerializeField] private LayerMask obstacleMask;

    [Header("떨림 방지")]
    [SerializeField] private float sightMemory = 0.35f;  // 놓쳐도 이 시간 동안은 '보임' 유지

    public bool Detected { get; private set; }
    public Vector3 LastDetectedPosition { get; private set; }

    private float lostTimer = 0f;

    void Start()
    {
        if (target == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    void Update()
    {
        // 1배속(자유 실시간)에서도 계속 작동하되, 플레이어가 멈춰 있으면 시간도 정지(SUPERHOT).
        // 2배속 예약 실행 중일 때만 TurnClock의 시간을 따름.
        bool inReservation = TurnClock.Instance != null && TurnClock.Instance.IsExecuting;
        if (!inReservation && PlayerMovement.IsTimeFrozen)
            return;   // 멈춤: 상태 보존

        float dt = inReservation ? TurnClock.Instance.GameDeltaTime : Time.deltaTime;

        bool raw = CheckView();

        if (raw)
        {
            Detected = true;
            LastDetectedPosition = target.position;  // 보이는 동안 LKP 갱신
            lostTimer = 0f;
        }
        else
        {
            // 방금까지 보였다면 유예 시간 동안은 Detected 유지
            lostTimer += dt;
            if (lostTimer >= sightMemory)
                Detected = false;
            // 유예 중엔 Detected와 LastDetectedPosition을 직전 값 그대로 둠
        }
    }

    bool CheckView()
    {
        if (target == null) return false;
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;
        if (dist > viewRadius) return false;
        Vector3 dir = toTarget.normalized;
        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f) return false;
        if (Physics.Raycast(transform.position, dir, dist, obstacleMask)) return false;
        return true;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 left = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + right * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * viewRadius);
    }
}