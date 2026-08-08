using UnityEngine;
using UnityEngine.InputSystem;

// v3 §4: 2배속 발동 게이지. 최대 2스택, 1스택 = 짧은 예약 창 1회.
// 충전 기준(처치 vs 명중, 참격 처치 포함 여부)은 기획서 §8 열린 과제 — 현재는 권총 처치 기준으로 연결.
public class WarpGauge : MonoBehaviour
{
    public static WarpGauge Instance { get; private set; }

    [SerializeField] private int maxStacks = 2;
    public int MaxStacks => maxStacks;
    public int Stacks { get; private set; } = 0;

    // TODO(임시/디버그): 1배속 실시간 권총이 아직 없어 게이지를 채울 방법이 없는 동안의 테스트용.
    // 1x 권총 충전이 구현되면 이 키와 Update()를 통째로 제거할 것.
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

    public void Charge()
    {
        if (Stacks >= maxStacks) return;
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
