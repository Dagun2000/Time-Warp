using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPlanner : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Camera cam;
    [SerializeField] private PlayerTimeline timeline;

    [Header("설정")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float aimTime = 0.4f;
    [SerializeField] private float shootRange = 20f;

    [Header("고스트")]
    [SerializeField] private Transform ghost;

    [Header("검")]
    [SerializeField] private float slashMinCharge = 2.2f;   // 최소 차지(일반병사 발사시간+α, 플레이어시간 기준)
    [SerializeField] private float slashMaxCharge = 3.0f;    // 최대 차지
    [SerializeField] private float slashRangePerSecond = 4f; // 차지 1초당 거리
    [SerializeField] private float slashMinRange = 2f;       // 최소 차지 때 거리
    [SerializeField] private float slashPathWidth = 0.6f;    // 경로 폭(반경)

    // FreeRoam: 1배속 자유 이동(게이지 발동 대기) / Planning: 2배속 예약 구성 / Executing: 예약 재생
    private enum State { FreeRoam, Planning, Executing }
    private State state = State.FreeRoam;

    public static PlayerPlanner Instance { get; private set; }
    public bool IsFreeRoam => state == State.FreeRoam;
    public bool IsPlanning => state == State.Planning;

    // 설계 중 "고스트 위치" — 행동을 쌓을수록 예상 끝 위치가 갱신됨
    private Vector3 ghostPos;

    // 행동을 추가하기 직전의 ghostPos 기록. 취소(Backspace) 시 여기서 복원.
    private readonly System.Collections.Generic.List<Vector3> ghostHistory = new System.Collections.Generic.List<Vector3>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (cam == null) cam = Camera.main;
        if (timeline == null) timeline = GetComponent<PlayerTimeline>();
    }

    void OnEnable() { ghostPos = transform.position; }

    void Update()
    {
        if (state == State.FreeRoam) FreeRoamUpdate();
        else if (state == State.Planning) PlanningUpdate();
        else ExecutingUpdate();

        // 설계 중엔 고스트를 예상 끝 위치에, 실행 중엔 숨김
        if (ghost != null)
        {
            if (state == State.Planning)
            {
                ghost.gameObject.SetActive(true);
                Vector3 g = ghostPos; g.y = transform.position.y;
                ghost.position = g;
            }
            else ghost.gameObject.SetActive(false);
        }
    }

    // 1배속 자유 이동 중: 게이지가 있으면 Space로 발동 → 2배속 예약 구성(Planning)에 진입
    void FreeRoamUpdate()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (WarpGauge.Instance != null && WarpGauge.Instance.TryConsume())
            {
                ghostPos = transform.position;
                timeline.Clear();
                ghostHistory.Clear();
                state = State.Planning;
            }
        }
    }

    void PlanningUpdate()
    {
        // 백스페이스: 마지막에 추가한 행동 취소 (고스트 위치도 그 전 상태로 복원)
        if (Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            if (timeline.RemoveLastAction() && ghostHistory.Count > 0)
            {
                ghostPos = ghostHistory[ghostHistory.Count - 1];
                ghostHistory.RemoveAt(ghostHistory.Count - 1);
            }
        }

        // E 키: 검 베기 추가 (고스트 위치 → 마우스 지점)
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (RaycastGround(out Vector3 slashEnd))
            {
                Vector3 dir = slashEnd - ghostPos; dir.y = 0f;
                float wantDist = dir.magnitude;

                // 거리 → 필요 차지 시간 계산
                // 최소거리까진 최소차지, 그 이상은 거리에 비례해 차지 증가, 최대차지에서 상한
                float charge;
                if (wantDist <= slashMinRange)
                    charge = slashMinCharge;
                else
                    charge = slashMinCharge + (wantDist - slashMinRange) / slashRangePerSecond;
                charge = Mathf.Min(charge, slashMaxCharge);

                // 그 차지로 실제 도달 가능한 거리 (상한 걸리면 거리도 제한)
                float reachDist = slashMinRange + (charge - slashMinCharge) * slashRangePerSecond;
                reachDist = Mathf.Min(reachDist, wantDist);

                Vector3 actualEnd = ghostPos + dir.normalized * reachDist;

                var slash = new SlashAction(transform, ghostPos, actualEnd, charge,
                                            slashPathWidth, enemyMask, obstacleMask);
                if (timeline.TryAddAction(slash))
                {
                    ghostHistory.Add(ghostPos);
                    ghostPos = actualEnd;   // 검은 이동을 동반하므로 고스트도 끝점으로
                }
            }
        }

        // 우클릭: 이동 추가 (고스트 위치 → 클릭 지점)
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (RaycastGround(out Vector3 dest))
            {
                var move = new MoveAction(transform, ghostPos, dest, moveSpeed);
                if (timeline.TryAddAction(move))
                {
                    ghostHistory.Add(ghostPos);
                    ghostPos = dest;   // 고스트를 이동 끝점으로
                }
            }
        }

        // 좌클릭: 사격 추가 (고스트 위치에서 클릭 방향으로)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (RaycastGround(out Vector3 aimPoint))
            {
                Vector3 dir = aimPoint - ghostPos; dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    var shoot = new ShootAction(transform, ghostPos, dir,
                                                aimTime, shootRange, enemyMask, obstacleMask);
                    if (timeline.TryAddAction(shoot))
                        ghostHistory.Add(ghostPos);   // 사격은 고스트 위치 안 바뀜(제자리), 취소 정렬용으로만 기록
                }
            }
        }

        // 스페이스: 실행
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame
            && timeline.Count > 0)
        {
            TurnClock.Instance.StartTurn();
            state = State.Executing;
        }
    }

    bool RaycastGround(out Vector3 point)
    {
        point = Vector3.zero;
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
        {
            point = hit.point;
            return true;
        }
        return false;
    }

    void ExecutingUpdate()
    {
        var clock = TurnClock.Instance;
        timeline.TickTimeline(clock.TurnTime, clock.GameDeltaTime);

        if (clock.TurnTime >= timeline.TotalDuration || !clock.IsExecuting)
            EndTurn();
    }

    void EndTurn()
    {
        if (TurnClock.Instance.IsExecuting)
            TurnClock.Instance.StopTurn();

        timeline.ForceFinishAll();   // ← Clear 전에 놓친 행동 마무리(참격/발사 보장)
        timeline.Clear();
        ghostHistory.Clear();
        ghostPos = transform.position;
        state = State.FreeRoam;   // 실행 종료 → 1배속 자유 이동으로 복귀 (스택 남으면 즉시 재발동 가능)
    }
}