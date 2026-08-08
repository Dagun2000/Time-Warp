using UnityEngine;

// v3: 게임 전체 시간이 아니라 "2배속 발동 예약 창"이 실행되는 동안의 타이머.
// 1배속 자유 실시간은 이 클래스와 무관하게 흐름 (SightSensor/EnemyBrain 참고).
public class TurnClock : MonoBehaviour
{
    public static TurnClock Instance { get; private set; }

    [SerializeField] private float turnLength = 3f;

    public bool IsExecuting { get; private set; } = false;
    public float TurnTime { get; private set; } = 0f;   // 이번 턴 경과 시간
    public float TurnLength => turnLength;

    // 이번 프레임에 흐른 "게임 시간". 실행 중이 아니면 0.
    public float GameDeltaTime { get; private set; } = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (IsExecuting)
        {
            GameDeltaTime = Time.deltaTime;
            TurnTime += GameDeltaTime;
            if (TurnTime >= turnLength)
                StopTurn();
        }
        else
        {
            GameDeltaTime = 0f;   // 멈춰 있을 땐 시간이 안 흐름
        }
    }

    public void StartTurn()
    {
        IsExecuting = true;
        TurnTime = 0f;
    }

    public void StopTurn()
    {
        IsExecuting = false;
        TurnTime = 0f;
        GameDeltaTime = 0f;
    }

}