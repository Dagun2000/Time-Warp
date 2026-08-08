using UnityEngine;
using System.Collections.Generic;

public class EnemyBrain : MonoBehaviour
{
    public enum State { Peace, Combat }

    [Header("상태")]
    public State CurrentState = State.Peace;

    [Header("2배속 예약 실행 중 적 시간 배율 (플레이어의 절반=0.5). 1배속 자유 실시간에서는 적용 안 됨")]
    [SerializeField] private float enemyTimeScale = 0.5f;

    public Vector3 LastKnownPosition { get; private set; }
    public bool HasKnownPosition { get; private set; } = false;

    // 적이 돌아갈 원래 위치 (복귀/순찰 기준점) - 미리 심어둠
    public Vector3 HomePosition { get; private set; }

    // 지금 이 순간 실제로 보고 있는가 (빨강이면 true)
    public bool SeeingNow { get; private set; } = false;

    private readonly List<ISensor> sensors = new List<ISensor>();
    private readonly List<IEnemyAction> actions = new List<IEnemyAction>();

    private Renderer rend;
    private Color peaceColor;

    void Start()
    {
        HomePosition = transform.position;

        var sBuf = new List<ISensor>();
        GetComponentsInChildren<ISensor>(true, sBuf);
        sensors.AddRange(sBuf);

        var aBuf = new List<IEnemyAction>();
        GetComponentsInChildren<IEnemyAction>(true, aBuf);
        actions.AddRange(aBuf);

        rend = GetComponentInChildren<Renderer>();
        if (rend != null) peaceColor = rend.material.color;
    }

    void Update()
    {
        // 1배속(자유 실시간)에서는 정상 속도로 계속 작동하되, 플레이어가 멈춰 있으면 시간도 정지(SUPERHOT).
        // 2배속 예약 실행 중일 때만 TurnClock 시간 기준 + enemyTimeScale(절반 속도) 적용.
        bool inReservation = TurnClock.Instance != null && TurnClock.Instance.IsExecuting;
        if (!inReservation && PlayerMovement.IsTimeFrozen)
            return;   // 시간 정지: 아무것도 안 함. OnIdle도 안 부름(조준 등 상태 보존).

        float rawDt = inReservation ? TurnClock.Instance.GameDeltaTime : Time.deltaTime;
        float dt = inReservation ? rawDt * enemyTimeScale : rawDt;

        // 1) 감지: 지금 보이나?
        SeeingNow = false;
        Vector3 seenPos = Vector3.zero;
        foreach (var s in sensors)
        {
            if (s.Detected) { SeeingNow = true; seenPos = s.LastDetectedPosition; break; }
        }

        // 2) 보이면 전투 진입 + LKP 갱신
        if (SeeingNow)
        {
            LastKnownPosition = seenPos;
            HasKnownPosition = true;
            if (CurrentState != State.Combat)
            {
                CurrentState = State.Combat;

                // 발견 순간: 플레이어 쪽으로 시선을 딱 맞춤 (사선 고정의 시작)
                Vector3 toPlayer = seenPos - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(toPlayer);

                Debug.Log($"{name}: 전투 진입");
            }
        }

        // 3) 상황에 맞는 행동만 Tick, 나머진 Idle
        //    빨강(SeeingNow) → WhenVisible 행동 작동
        //    주황(Combat & !SeeingNow) → WhenLost 행동 작동
        foreach (var a in actions)
        {
            bool active =
                (a.ActionType == EnemyActionType.WhenVisible && SeeingNow) ||
                (a.ActionType == EnemyActionType.WhenLost && CurrentState == State.Combat && !SeeingNow);

            if (active) a.Tick(this, dt);
            else a.OnIdle();
        }

        // 4) 디버그 색
        if (rend != null)
        {
            if (SeeingNow) rend.material.color = Color.red;
            else if (CurrentState == State.Combat) rend.material.color = new Color(1f, 0.6f, 0f);
            else rend.material.color = peaceColor;
        }
    }

    public void ForcePeace()
    {
        CurrentState = State.Peace;
        HasKnownPosition = false;
    }
    // 행동 부품이 "수색 끝, 플레이어 없음" 판단했을 때 호출
    public void ReturnToPeace()
    {
        CurrentState = State.Peace;
        HasKnownPosition = false;
        SeeingNow = false;
    }
}