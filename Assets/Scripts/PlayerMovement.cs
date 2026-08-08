using UnityEngine;
using UnityEngine.InputSystem;

// v3 1배속: 자유 실시간 이동. 움직이거나 무기를 쓰면 시간이 흐르고, 완전히 멈추면 시간도 정지(SUPERHOT 모델).
// 실제 "시간 정지" 적용은 SightSensor/EnemyBrain이 IsTimeFrozen을 읽어서 수행한다.
public class PlayerMovement : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 720f;
    [SerializeField] private float stopThreshold = 0.05f; // 이 이하 입력 세기는 "멈춤"으로 간주

    [Header("조준 (항상 마우스 방향을 봄)")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask groundMask;

    // 지금 게임 시간이 정지해야 하는가 (플레이어가 이동도 무기 사용도 안 하고 있음).
    // 2배속 예약 실행 중에는 이 스크립트가 관여하지 않으므로 true로 둬도 무해함(TurnClock이 담당).
    public static bool IsTimeFrozen { get; private set; } = true;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        // 예약 구성/실행 중에는 자유 이동 비활성 (PlayerPlanner가 그 구간을 담당)
        if (PlayerPlanner.Instance != null && !PlayerPlanner.Instance.IsFreeRoam)
        {
            IsTimeFrozen = true;
            return;
        }

        // 조준: 사격/검 다 마우스 방향 기준이므로, 이동 여부와 무관하게 항상 마우스를 보도록 회전
        FaceMouse();

        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }

        bool moving = input.sqrMagnitude > stopThreshold * stopThreshold;

        // 검 차징 중엔 시간이 계속 흘러야 노출 리스크가 성립하므로(§5), 멈춰 있어도 시간 정지 아님.
        IsTimeFrozen = !moving && !PlayerCombat.IsChargingSword;

        // 차징 중엔 제자리 고정 (조준 회전은 위에서 이미 처리됨)
        if (PlayerCombat.IsChargingSword) return;

        if (!moving) return;

        Vector3 dir = new Vector3(input.x, 0f, input.y).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    void FaceMouse()
    {
        if (cam == null || Mouse.current == null) return;
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask)) return;

        Vector3 dir = hit.point - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }
}
