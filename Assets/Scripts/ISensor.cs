// 모든 감지 부품(시야, 청각...)이 따르는 규격.
// 두뇌는 이 규격만 알면 되고, 구체적으로 시야인지 청각인지는 몰라도 됨.
public interface ISensor
{
    // 이번 틱에 플레이어를 감지했는가?
    bool Detected { get; }

    // 감지했다면 "플레이어가 여기 있더라" 하는 위치. 못 했으면 의미 없음.
    UnityEngine.Vector3 LastDetectedPosition { get; }
}