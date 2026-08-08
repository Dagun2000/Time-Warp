// 모든 행동 부품(사격, 이동...)이 따르는 규격.
// 두뇌는 매 틱 "지금 너 작동할 상황이야" 하고 Tick을 부를 뿐,
// 구체적으로 뭘 하는지(쏘는지 가는지)는 부품이 알아서 함.
public interface IEnemyAction
{
    // 이 행동이 언제 작동하는지 두뇌가 판단할 수 있도록 분류
    EnemyActionType ActionType { get; }

    // 두뇌가 매 틱 호출. dt는 이미 적 시간배율이 적용된 델타.
    void Tick(EnemyBrain brain, float dt);

    // 이 행동이 활성화되지 않는 틱에 호출(상태 리셋용)
    void OnIdle();
}

public enum EnemyActionType
{
    WhenVisible,   // 플레이어가 지금 보일 때 작동 (빨강) - 예: 사격
    WhenLost       // 전투 상태인데 놓쳤을 때 작동 (주황) - 예: 추적
}