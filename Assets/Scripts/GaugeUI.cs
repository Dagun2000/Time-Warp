using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// v3 §4: 2배속 발동 게이지 스택 표시. maxStacks만큼 아이콘을 나열하고, 채워진 스택만 밝게 표시.
public class GaugeUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RectTransform stackContainer; // 아이콘들이 들어갈 곳
    [SerializeField] private GameObject stackPrefab;        // RectTransform + Image가 붙은 아이콘 1칸 프리팹

    [Header("설정")]
    [SerializeField] private float spacing = 10f;
    [SerializeField] private Color filledColor = new Color(0.3f, 0.8f, 1f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.12f, 0.12f, 0.12f, 1f);

    private readonly List<Image> spawned = new List<Image>();
    private int builtForMax = -1;

    void Update()
    {
        if (WarpGauge.Instance == null || stackContainer == null || stackPrefab == null) return;

        int max = WarpGauge.Instance.MaxStacks;
        if (max != builtForMax) Rebuild(max);

        int current = WarpGauge.Instance.Stacks;
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                spawned[i].color = (i < current) ? filledColor : emptyColor;
        }
    }

    void Rebuild(int max)
    {
        foreach (var img in spawned)
            if (img != null) Destroy(img.gameObject);
        spawned.Clear();

        float w = ((RectTransform)stackPrefab.transform).sizeDelta.x;

        for (int i = 0; i < max; i++)
        {
            GameObject go = Instantiate(stackPrefab, stackContainer);
            go.SetActive(true);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(i * (w + spacing), 0f);
            spawned.Add(go.GetComponent<Image>());
        }

        builtForMax = max;
    }
}
