using UnityEngine;
using System.Collections.Generic;

public class SlashAction : IPlayerAction
{
    private readonly Transform slasher;
    private readonly Vector3 from;
    private readonly Vector3 to;
    private readonly float chargeTime;   // 차지 시간 = Duration
    private readonly float pathWidth;
    private readonly LayerMask enemyMask;
    private readonly LayerMask obstacleMask;

    public float Duration => chargeTime;

    private bool slashed = false;

    public SlashAction(Transform slasher, Vector3 from, Vector3 to, float chargeTime,
                       float pathWidth, LayerMask enemyMask, LayerMask obstacleMask)
    {
        this.slasher = slasher;
        this.from = from;
        this.to = to;
        this.chargeTime = chargeTime;
        this.pathWidth = pathWidth;
        this.enemyMask = enemyMask;
        this.obstacleMask = obstacleMask;
    }

    public void OnStart() { slashed = false; }

    public void Tick(float localTime, float dt)
    {
        // 차지 동안엔 제자리(발도 자세). 차지 완료 순간 참격.
        if (!slashed && localTime >= chargeTime - 0.0001f)
            Slash();
    }

    public void OnEnd()
    {
        if (!slashed) Slash();
    }

    void Slash()
    {
        slashed = true;

        Vector3 start = from;
        Vector3 end = to;
        start.y = slasher.position.y;
        end.y = slasher.position.y;

        Vector3 dir = (end - start);
        float dist = dir.magnitude;
        if (dist < 0.001f) { TeleportTo(end); return; }
        dir /= dist;

        // 경로가 벽에 막히면 거기까지만
        if (Physics.Raycast(start, dir, out RaycastHit wallHit, dist, obstacleMask))
        {
            dist = wallHit.distance;
            end = start + dir * dist;
        }

        // 경로(캡슐 모양)상의 모든 적 처치
        // start~end 선분을 pathWidth 반경의 캡슐로 훑어서 적 검출 (OverlapCapsule이 선분 전체를 정확히 덮음)
        Collider[] cols = Physics.OverlapCapsule(start, end, pathWidth, enemyMask);
        foreach (var c in cols)
        {
            var hp = c.GetComponent<EnemyHealth>();
            if (hp == null || hp.IsDead) continue;

            Debug.Log($"참격! {c.name} 처치");
            hp.Kill(); // §5: 일섬 긋기는 체력 무관 즉사
            WarpGauge.Instance?.Charge(); // §4/§8: 게이지 충전은 처치 기반, 검도 포함
        }

        // 플레이어를 경로 끝으로 순간이동 (빛 경로 느낌)
        TeleportTo(end);
    }

    void TeleportTo(Vector3 pos)
    {
        pos.y = slasher.position.y;
        slasher.position = pos;
    }
}