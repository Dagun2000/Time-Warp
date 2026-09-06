using UnityEngine;

// 플레이어 체력. 데미지 수치는 공격 주체(적 타입/무기)마다 다르게 넘겨받는 구조 —
// 난이도별/업그레이드별로 달라질 값은 여기가 아니라 호출부(AimAndShoot 등)에서 관리.
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [SerializeField] private float maxHealth = 100f;
    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        Debug.Log($"플레이어 피격: -{amount} (남은 체력 {CurrentHealth}/{maxHealth})");

        if (CurrentHealth <= 0f) Die();
    }

    void Die()
    {
        IsDead = true;
        Debug.Log("플레이어 사망");
    }
}
