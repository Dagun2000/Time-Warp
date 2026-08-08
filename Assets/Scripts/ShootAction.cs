using UnityEngine;

public class ShootAction : IPlayerAction
{
    private readonly Transform shooter;
    private readonly Vector3 origin;     // 발사 시작 위치(설계 시점의 플레이어 위치)
    private readonly Vector3 direction;  // 고정 발사 방향(설계 때 지정)
    private readonly float aimTime;      // 조준 시간 = Duration
    private readonly float range;
    private readonly LayerMask enemyMask;
    private readonly LayerMask obstacleMask;

    public float Duration => aimTime;

    private bool fired = false;

    public ShootAction(Transform shooter, Vector3 origin, Vector3 direction,
                       float aimTime, float range, LayerMask enemyMask, LayerMask obstacleMask)
    {
        this.shooter = shooter;
        this.origin = origin;
        this.direction = direction.normalized;
        this.aimTime = aimTime;
        this.range = range;
        this.enemyMask = enemyMask;
        this.obstacleMask = obstacleMask;
    }

    public void OnStart() { fired = false; }

    public void Tick(float localTime, float dt)
    {
        // 조준 완료 순간 1회 발사
        if (!fired && localTime >= aimTime - 0.0001f)
            Fire();
    }

    public void OnEnd()
    {
        // 혹시 Tick에서 못 쐈으면(짧은 구간 건너뜀 대비) 여기서 보장
        if (!fired) Fire();
    }

    void Fire()
    {
        fired = true;

        // 발사 위치는 origin이 아니라 "지금 shooter 위치"가 맞음(이동 후 쏠 수 있으니)
        Vector3 start = shooter.position;

        // 벽이 먼저 막으면 빗나감. 적이 먼저 맞으면 처치.
        // 적과 벽을 합친 마스크로 가장 가까운 것 검사.
        int combined = enemyMask | obstacleMask;
        if (Physics.Raycast(start, direction, out RaycastHit hit, range, combined))
        {
            // 맞은 게 적 레이어인지 확인
            if (((1 << hit.collider.gameObject.layer) & enemyMask) != 0)
            {
                Debug.Log($"명중! {hit.collider.name} 처치");
                hit.collider.gameObject.SetActive(false);  // 일단 비활성화로 처치
                WarpGauge.Instance?.Charge();  // §4: 게이지 충전 수단 = 권총 처치
            }
            else
            {
                Debug.Log("벽에 막힘 (빗나감)");
            }
        }
        else
        {
            Debug.Log("허공 (빗나감)");
        }
    }
}