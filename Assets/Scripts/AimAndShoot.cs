using UnityEngine;

public class AimAndShoot : MonoBehaviour, IEnemyAction
{
    [SerializeField] private float aimTime = 1.0f;   // 조준에 걸리는 시간(적 시간 기준)
    [SerializeField] private float damage = 25f;     // 명중 시 데미지. 적 타입/난이도/업그레이드별로 값만 다르게 설정

    private float aimProgress = 0f;
    private bool hasFired = false;

    public EnemyActionType ActionType => EnemyActionType.WhenVisible;

    public void Tick(EnemyBrain brain, float dt)
    {
        if (hasFired) return;

        aimProgress += dt;
       

        if (aimProgress >= aimTime)
        {
            hasFired = true;
            Debug.Log($"{brain.name}: 발사! 플레이어 피격");
            PlayerHealth.Instance?.TakeDamage(damage);
        }
    }

    public void OnIdle()
    {
        if (aimProgress > 0f)
            Debug.Log($"조준 리셋됨! 직전 진행도 {aimProgress:F2}, IsExecuting={TurnClock.Instance.IsExecuting}");
        aimProgress = 0f;
        hasFired = false;
    }
}