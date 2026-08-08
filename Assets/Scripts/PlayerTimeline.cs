using UnityEngine;
using System.Collections.Generic;

public class PlayerTimeline : MonoBehaviour
{
    private readonly List<IPlayerAction> actions = new List<IPlayerAction>();
    private readonly HashSet<IPlayerAction> started = new HashSet<IPlayerAction>();
    private readonly HashSet<IPlayerAction> ended = new HashSet<IPlayerAction>();

    [SerializeField] private float turnLength = 3f;

    // 순차: 각 행동의 시작 시점 = 앞 행동들의 Duration 합
    private float StartTimeOf(int index)
    {
        float t = 0f;
        for (int i = 0; i < index; i++) t += actions[i].Duration;
        return t;
    }

    public float TotalDuration
    {
        get { float t = 0f; foreach (var a in actions) t += a.Duration; return t; }
    }

    // 추가 시도. 3초 넘으면 false 반환하고 추가 안 함.
    public bool TryAddAction(IPlayerAction a)
    {
        if (TotalDuration + a.Duration > turnLength + 0.0001f)
        {
            Debug.Log($"타임라인 초과: 현재 {TotalDuration:F2} + {a.Duration:F2} > {turnLength}");
            return false;
        }
        actions.Add(a);
        return true;
    }
    public IReadOnlyList<IPlayerAction> GetActions() => actions;
    public void Clear() { actions.Clear(); started.Clear(); ended.Clear(); }
    public int Count => actions.Count;

    // 예약 구성(Planning) 중 마지막으로 추가한 행동 취소. 아직 Tick된 적 없으므로 순수 큐 제거만 하면 됨.
    public bool RemoveLastAction()
    {
        if (actions.Count == 0) return false;
        var last = actions[actions.Count - 1];
        actions.RemoveAt(actions.Count - 1);
        started.Remove(last);
        ended.Remove(last);
        return true;
    }
    // 실행 끝날 때 호출: 아직 안 끝난 행동들의 OnEnd를 보장(놓친 순간판정 처리)
    public void ForceFinishAll()
    {
        foreach (var a in actions)
        {
            if (!started.Contains(a)) { a.OnStart(); started.Add(a); }
            if (!ended.Contains(a)) { a.OnEnd(); ended.Add(a); }
        }
    }

    public void TickTimeline(float turnTime, float dt)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            var a = actions[i];
            float start = StartTimeOf(i);
            float end = start + a.Duration;

            if (turnTime < start) continue;

            if (turnTime <= end)
            {
                if (!started.Contains(a)) { a.OnStart(); started.Add(a); }
                a.Tick(turnTime - start, dt);
            }
            else
            {
                if (!started.Contains(a)) { a.OnStart(); started.Add(a); }
                if (!ended.Contains(a)) { a.OnEnd(); ended.Add(a); }
            }
        }
    }
}