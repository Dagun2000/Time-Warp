using UnityEngine;
using UnityEngine.InputSystem;

// v3 §5: 1배속 실시간 무기 사용.
// - 권총: 즉발, 탄창제, 치고빠지기용. 처치 시 게이지 충전.
// - 검: 마우스 왼쪽... 이 아니라 E를 홀드해서 차징 → 릴리즈 시 그 순간 조준 지점으로 즉시 참격(SlashAction 재사용).
//   2배속 모드보다 차징이 2배 느림 → 적이 이미 인지한 상태면 버티기 힘듦(암살 전용, 조건부).
public class PlayerCombat : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private LayerMask obstacleMask;

    [Header("권총")]
    [SerializeField] private float pistolRange = 20f;
    [SerializeField] private float fireCooldown = 0.25f;
    [SerializeField] private int magazineSize = 5;

    [Header("검 (1배속 = 2배속 대비 차징 2배 느림)")]
    [SerializeField] private float slashMinCharge = 4.4f;   // 2배속 slashMinCharge(2.2)의 2배
    [SerializeField] private float slashMaxCharge = 6.0f;   // 2배속 slashMaxCharge(3.0)의 2배
    [SerializeField] private float slashMinRange = 2f;
    [SerializeField] private float slashRangePerSecond = 2f; // 2배속(4)의 절반
    [SerializeField] private float slashPathWidth = 0.6f;

    public static bool IsChargingSword { get; private set; }

    private int ammo;
    private float fireTimer;
    private bool charging;
    private float chargeTimer;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        ammo = magazineSize;
    }

    void Update()
    {
        if (PlayerPlanner.Instance != null && !PlayerPlanner.Instance.IsFreeRoam)
        {
            CancelCharge();
            return;
        }

        if (fireTimer > 0f) fireTimer -= Time.deltaTime;

        HandlePistol();
        HandleSword();
    }

    void HandlePistol()
    {
        if (charging) return; // 검 차징 중엔 권총 못 씀
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (fireTimer > 0f) return;
        if (ammo <= 0) { Debug.Log("[권총] 탄약 없음"); return; }
        if (!RaycastGround(out Vector3 aimPoint)) return;

        Vector3 dir = aimPoint - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        ammo--;
        fireTimer = fireCooldown;
        Fire(dir.normalized);
    }

    void Fire(Vector3 direction)
    {
        Vector3 start = transform.position;
        int combined = enemyMask | obstacleMask;

        if (Physics.Raycast(start, direction, out RaycastHit hit, pistolRange, combined))
        {
            if (((1 << hit.collider.gameObject.layer) & enemyMask) != 0)
            {
                Debug.Log($"[1x 권총] 명중! {hit.collider.name} 처치 (남은 탄약 {ammo}/{magazineSize})");
                hit.collider.gameObject.SetActive(false);
                WarpGauge.Instance?.Charge(); // §4: 게이지 충전 수단 = 권총 처치
            }
            else
            {
                Debug.Log("[1x 권총] 벽에 막힘");
            }
        }
    }

    void HandleSword()
    {
        if (Keyboard.current == null) return;

        if (!charging && Keyboard.current.eKey.wasPressedThisFrame)
        {
            charging = true;
            chargeTimer = 0f;
            IsChargingSword = true;
            return;
        }

        if (!charging) return;

        chargeTimer += Time.deltaTime;

        bool released = Keyboard.current.eKey.wasReleasedThisFrame;
        bool maxedOut = chargeTimer >= slashMaxCharge;

        if (released || maxedOut)
            CommitSlash(Mathf.Min(chargeTimer, slashMaxCharge));
    }

    void CommitSlash(float actualCharge)
    {
        charging = false;
        IsChargingSword = false;

        if (actualCharge < slashMinCharge)
        {
            Debug.Log($"[1x 검] 차징 부족({actualCharge:F2} < {slashMinCharge}), 취소");
            return;
        }

        if (!RaycastGround(out Vector3 aimPoint)) return;

        Vector3 dir = aimPoint - transform.position; dir.y = 0f;
        float wantDist = dir.magnitude;
        if (wantDist < 0.001f) return;

        float reachDist = slashMinRange + (actualCharge - slashMinCharge) * slashRangePerSecond;
        reachDist = Mathf.Min(reachDist, wantDist);

        Vector3 end = transform.position + dir.normalized * reachDist;

        // 차징은 이미 실시간으로 다 끝났으니, 여기서는 즉시 실행만 (OnStart+OnEnd로 강제 커밋)
        var slash = new SlashAction(transform, transform.position, end, actualCharge,
                                     slashPathWidth, enemyMask, obstacleMask);
        slash.OnStart();
        slash.OnEnd();
    }

    void CancelCharge()
    {
        charging = false;
        IsChargingSword = false;
    }

    bool RaycastGround(out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null || Mouse.current == null) return false;
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
        {
            point = hit.point;
            return true;
        }
        return false;
    }
}
