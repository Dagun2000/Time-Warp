using UnityEngine;
using UnityEngine.InputSystem;

// v3 §5: 1배속 실시간 무기 사용.
// - 권총: 마우스를 적 위에 올리면 하이라이트(타겟 지정) → 클릭하면 그 적을 조준 대기 후 사격(즉발 아님).
//   탄창제, 치고빠지기용. 처치 시 게이지 충전.
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
    [SerializeField] private float pistolAimTime = 0.3f;  // 클릭 후 발사까지 조준 대기시간(초) — 조정은 이 값만 바꾸면 됨
    [SerializeField] private float fireCooldown = 0.25f;
    [SerializeField] private int magazineSize = 5;
    [SerializeField] private float hoverPickRadius = 0.4f; // 마우스 커서가 적 정중앙에서 이 반경 안이면 하이라이트(관대한 판정)
    [SerializeField] private float pistolDamage = 25f;     // 명중 시 데미지 — 적 체력과 별개로 여기서 조정

    [Header("검 (1배속 = 2배속 대비 차징 느림)")]
    [SerializeField] private float slashMinCharge = 4.0f;   // 최소 차징 시간(초) — 조정은 이 값만 바꾸면 됨
    [SerializeField] private float slashMaxCharge = 6.0f;   // 2배속 slashMaxCharge(3.0)의 2배
    [SerializeField] private float slashMinRange = 2f;
    [SerializeField] private float slashRangePerSecond = 2f; // 2배속(4)의 절반
    [SerializeField] private float slashPathWidth = 0.6f;

    [Header("일섬 도달거리 표시 (튜닝용 임시 표시 — 나중에 그래픽으로 교체 가능)")]
    [SerializeField] private LineRenderer slashRangeLine; // 비워두면 Player에 있는 LineRenderer를 그대로 씀

    public static bool IsChargingSword { get; private set; }
    public static bool IsAimingPistol { get; private set; }

    private int ammo;
    private float fireTimer;
    private bool charging;
    private float chargeTimer;
    private bool aiming;
    private float aimTimer;
    private EnemyBrain hoveredEnemy;   // 지금 마우스가 올라가 있는 적 (하이라이트 대상)
    private EnemyBrain aimTarget;      // 조준 대기 중 고정된 사격 대상

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        ammo = magazineSize;

        if (slashRangeLine == null) slashRangeLine = GetComponent<LineRenderer>();
        if (slashRangeLine != null) slashRangeLine.enabled = false;
    }

    void Update()
    {
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead) return;

        if (PlayerPlanner.Instance != null && !PlayerPlanner.Instance.IsFreeRoam)
        {
            CancelCharge();
            CancelAim();
            return;
        }

        if (fireTimer > 0f) fireTimer -= Time.deltaTime;

        HandlePistol();
        HandleSword();
    }

    void HandlePistol()
    {
        if (charging) return; // 검 차징 중엔 권총 못 씀

        if (!aiming)
        {
            UpdateHover();

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (fireTimer > 0f) return;
            if (ammo <= 0) { Debug.Log("[권총] 탄약 없음"); return; }
            if (hoveredEnemy == null) return; // 하이라이트된 적이 있어야 사격 가능(타겟 지정형)

            aimTarget = hoveredEnemy;
            aiming = true;
            aimTimer = 0f;
            IsAimingPistol = true;
            return;
        }

        // 조준 중: pistolAimTime만큼 대기 후 그 순간 타겟 위치 기준으로 발사(즉발 아님)
        aimTimer += Time.deltaTime;
        if (aimTimer < pistolAimTime) return;

        aiming = false;
        IsAimingPistol = false;

        ammo--;
        fireTimer = fireCooldown;
        FireAtTarget(aimTarget);
        aimTarget = null;
    }

    // 마우스 아래 적을 찾아 하이라이트. 실제로 사격 가능한(플레이어 기준 시야가 뚫린) 적만 대상.
    void UpdateHover()
    {
        EnemyBrain candidate = null;

        if (cam != null && Mouse.current != null)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            // 카메라 기준 거리 제한은 pistolRange를 쓰지 않음 — 카메라가 플레이어보다 높고 뒤에 있어서
            // (쿼터뷰 오프셋) 카메라→적 거리가 플레이어→적 거리보다 항상 더 길기 때문.
            // 여기서는 "마우스 아래 뭐가 있는지"만 넉넉하게 찾고, 실제 사격 가능 여부는 아래에서 따로 검증.
            const float hoverCastDistance = 1000f;

            // 픽셀 단위 Raycast 대신 SphereCast로 살짝 여유를 줘서, 커서가 적 중심에 딱 안 맞아도 잡히게 함.
            if (Physics.SphereCast(ray, hoverPickRadius, out RaycastHit hit, hoverCastDistance, enemyMask))
                candidate = hit.collider.GetComponentInParent<EnemyBrain>();
        }

        // 카메라는 플레이어보다 높고 뒤에 있어서 벽 너머를 볼 수 있지만 캐릭터는 벽 너머로 못 쏨.
        // 그래서 카메라 기준 가림 판정만으론 부족 — FireAtTarget과 똑같은 기준(플레이어 위치, 수평)으로
        // 다시 검증해야 "하이라이트되면 실제로 맞는다"가 보장됨.
        EnemyBrain newHover = (candidate != null && HasLineOfSightFromPlayer(candidate)) ? candidate : null;

        if (newHover == hoveredEnemy) return;

        if (hoveredEnemy != null) hoveredEnemy.IsHighlighted = false;
        hoveredEnemy = newHover;
        if (hoveredEnemy != null) hoveredEnemy.IsHighlighted = true;
    }

    // 플레이어 위치 기준 수평 시야가 뚫려있는지(벽에 안 가려지는지) 확인. 하이라이트/발사 둘 다 이 기준을 씀.
    bool HasLineOfSightFromPlayer(EnemyBrain target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        Vector3 start = transform.position;
        Vector3 dir = target.transform.position - start; dir.y = 0f;
        float dist = dir.magnitude;
        if (dist < 0.0001f) return true;
        dir /= dist;

        int combined = enemyMask | obstacleMask;
        return Physics.Raycast(start, dir, out RaycastHit hit, pistolRange, combined)
               && hit.collider.GetComponentInParent<EnemyBrain>() == target;
    }

    void FireAtTarget(EnemyBrain target)
    {
        if (!HasLineOfSightFromPlayer(target))
        {
            Debug.Log("[1x 권총] 가려짐/사거리 밖 (빗나감)");
            return;
        }

        var hp = target.GetComponent<EnemyHealth>();
        if (hp == null)
        {
            Debug.LogWarning($"[1x 권총] {target.name}에 EnemyHealth 없음");
            return;
        }

        hp.TakeDamage(pistolDamage);
        Debug.Log($"[1x 권총] 명중! {target.name} (남은 탄약 {ammo}/{magazineSize}, 적 체력 {hp.CurrentHealth}/{hp.MaxHealth})");

        if (hp.IsDead)
        {
            target.IsHighlighted = false;
            WarpGauge.Instance?.Charge(); // §4: 게이지 충전 수단 = 권총 처치(킬)
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
        UpdateSlashRangeLine(chargeTimer);

        bool released = Keyboard.current.eKey.wasReleasedThisFrame;
        bool maxedOut = chargeTimer >= slashMaxCharge;

        if (released || maxedOut)
            CommitSlash(Mathf.Min(chargeTimer, slashMaxCharge));
    }

    // 차징 중 실시간으로 "지금 릴리즈하면 어디까지 닿는지" 표시. 최소 차징 전엔 안 보임(그 전엔 참격 자체가 취소되니까).
    void UpdateSlashRangeLine(float charge)
    {
        if (slashRangeLine == null) return;

        float clamped = Mathf.Min(charge, slashMaxCharge);
        if (clamped < slashMinCharge)
        {
            slashRangeLine.enabled = false;
            return;
        }

        float reachDist = slashMinRange + (clamped - slashMinCharge) * slashRangePerSecond;
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * reachDist;

        slashRangeLine.enabled = true;
        slashRangeLine.positionCount = 2;
        slashRangeLine.SetPosition(0, start);
        slashRangeLine.SetPosition(1, end);
    }

    void CommitSlash(float actualCharge)
    {
        charging = false;
        IsChargingSword = false;
        if (slashRangeLine != null) slashRangeLine.enabled = false;

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
        if (slashRangeLine != null) slashRangeLine.enabled = false;
    }

    void CancelAim()
    {
        aiming = false;
        IsAimingPistol = false;
        aimTarget = null;

        if (hoveredEnemy != null) hoveredEnemy.IsHighlighted = false;
        hoveredEnemy = null;
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
