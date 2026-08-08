using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TimelineUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayerTimeline timeline;
    [SerializeField] private RectTransform blockContainer; // 블록들이 들어갈 곳
    [SerializeField] private GameObject blockPrefab;       // ActionBlock 프리팹

    // 예약 바 배경째로 숨길 대상. 비워두면 blockContainer의 부모(TimelineBar)를 자동으로 씀.
    [SerializeField] private GameObject panelRoot;

    [Header("설정")]
    [SerializeField] private float turnLength = 3f;

    [Header("행동별 색")]
    [SerializeField] private Color moveColor = new Color(0.3f, 0.5f, 1f, 0.9f);
    [SerializeField] private Color shootColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0.9f);

    private readonly List<GameObject> spawned = new List<GameObject>();

    void Awake()
    {
        if (panelRoot == null && blockContainer != null && blockContainer.parent != null)
            panelRoot = blockContainer.parent.gameObject;
    }

    void Update()
    {
        // FreeRoam(1배속 자유 이동) 중엔 예약 타임라인 바를 완전히 숨김 — 게이지 바와 헷갈리지 않도록.
        bool show = PlayerPlanner.Instance == null || !PlayerPlanner.Instance.IsFreeRoam;

        if (panelRoot != null) panelRoot.SetActive(show);

        if (!show)
        {
            foreach (var go in spawned) Destroy(go);
            spawned.Clear();
            return;
        }

        Redraw();
    }

    void Redraw()
    {
        // 기존 블록 제거
        foreach (var go in spawned) Destroy(go);
        spawned.Clear();

        if (timeline == null || blockContainer == null || blockPrefab == null) return;

        float barWidth = blockContainer.rect.width;
        var actions = timeline.GetActions();   // 아래에서 PlayerTimeline에 추가

        float cursorTime = 0f;
        foreach (var a in actions)
        {
            float startFrac = cursorTime / turnLength;
            float widthFrac = a.Duration / turnLength;

            GameObject block = Instantiate(blockPrefab, blockContainer);
            block.SetActive(true);
            var rt = block.GetComponent<RectTransform>();

            // 왼쪽부터 시간 비율로 배치
            float x = startFrac * barWidth;
            float w = widthFrac * barWidth;
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);

            // 행동 종류별 색
            var img = block.GetComponent<Image>();
            if (img != null) img.color = ColorFor(a);

            spawned.Add(block);
            cursorTime += a.Duration;
        }
    }

    Color ColorFor(IPlayerAction a)
    {
        if (a is MoveAction) return moveColor;
        if (a is ShootAction) return shootColor;
        if (a is SlashAction) return new Color(0.7f, 0.3f, 1f, 0.9f);  // 보라
        return defaultColor;
    }
}