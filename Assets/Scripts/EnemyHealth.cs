using UnityEngine;

// 적 체력. 총(권총)은 데미지 누적 — TakeDamage(amount)로 깎임, 수치는 적 타입/난이도/업그레이드별로
// 프리팹마다 다르게 설정하면 됨.
// 검(일섬)은 기획서 §5 원칙대로 체력과 무관하게 긋기만 들어가면 즉사 — Kill()로 바로 처치.
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 25f;
    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        if (CurrentHealth <= 0f) Kill();
    }

    // 체력 무관 즉시 처치 (§5: 일섬 긋기는 체력 무관 즉사, 방패병 등 예외는 이 메서드를 오버라이드/차단하면 됨)
    public void Kill()
    {
        if (IsDead) return;
        IsDead = true;
        gameObject.SetActive(false);
    }
}
