using UnityEngine;
using UnityEngine.InputSystem;

// v3 §4: 2배속 발동 게이지. 최대 2스택, 1스택 = 짧은 예약 창 1회.
// §8 열린 과제 결론: 처치 기반 충전, 무기 종류(권총/검) 무관하게 적을 killsPerStack명 처치하면 1스택.
public class WarpGauge : MonoBehaviour
{
    public static WarpGauge Instance { get; private set; }

    [SerializeField] private int maxStacks = 2;
    public int MaxStacks => maxStacks;
    public int Stacks { get; private set; } = 0;

    [Header("충전 (처치 기반, §8)")]
    [SerializeField] private int killsPerStack = 3; // 이 명수를 처치할 때마다 1스택 충전 — 조정은 이 값만 바꾸면 됨
    private int killProgress = 0;

    // TODO(임시/디버그): 테스트용 강제 충전 키.
    [SerializeField] private Key debugChargeKey = Key.G;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[debugChargeKey].wasPressedThisFrame)
            Charge();
    }

    // 적 처치 시 호출 (권총/검 무관). killsPerStack명 모이면 1스택 충전.
    public void Charge()
    {
        if (Stacks >= maxStacks) return;

        killProgress++;
        if (killProgress < killsPerStack)
        {
            Debug.Log($"처치 진행도: {killProgress}/{killsPerStack}");
            return;
        }

        killProgress = 0;
        Stacks++;
        Debug.Log($"게이지 충전: {Stacks}/{maxStacks}");
    }

    public bool TryConsume()
    {
        if (Stacks <= 0) return false;
        Stacks--;
        Debug.Log($"게이지 소모(발동): {Stacks}/{maxStacks} 남음");
        return true;
    }
}
