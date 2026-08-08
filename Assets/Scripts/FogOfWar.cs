using UnityEngine;

// 플레이어 시야 기반 Fog of War.
// - 정면 기준 부채꼴(visionAngle)만 보임 — SightSensor의 적 시야와 같은 개념을 플레이어 쪽에 적용.
// - 부채꼴 안이라도 obstacleMask에 걸리면(벽) 그 뒤는 안 보임 — 각도별 레이캐스트로 가려짐 거리를 구함.
// - 한 번 본 곳은 흐리게(exploredDimAlpha) 계속 남고, 현재 보이는 곳만 완전히 걷힘.
// 텍스처 알파 채널에 안개 농도를 구워서 Custom/FogOfWar 셰이더(월드 XZ 기준 UV)에 입힌다.
public class FogOfWar : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform player;
    [SerializeField] private Renderer fogPlaneRenderer;
    [SerializeField] private LayerMask obstacleMask;

    [Header("월드 범위 (안개가 덮는 XZ 영역, 좌하단 기준)")]
    [SerializeField] private Vector2 worldOrigin = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 worldSize = new Vector2(100f, 100f);

    [Header("시야")]
    [SerializeField] private float visionRadius = 10f;
    [SerializeField] private float visionAngle = 100f; // 정면 기준 좌우 합산 각도
    [SerializeField] private int rayCount = 90;         // 각도 샘플 수(많을수록 벽 경계 매끈함)
    [SerializeField] private int resolution = 512;

    [Header("경계 페더링")]
    [SerializeField] private float edgeFeatherWorld = 0.4f;  // 벽/시야끝 경계
    [SerializeField] private float angleFeatherDeg = 3f;     // 콘 좌우 경계

    [Header("안개 농도")]
    [SerializeField, Range(0f, 1f)] private float unexploredAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float exploredDimAlpha = 0.55f;

    private static readonly int FogTexId = Shader.PropertyToID("_FogTex");
    private static readonly int WorldOriginId = Shader.PropertyToID("_WorldOrigin");
    private static readonly int WorldSizeId = Shader.PropertyToID("_WorldSize");

    private Texture2D fogTexture;
    private byte[] explored; // resolution*resolution, 0 또는 1. 한 번 켜지면 계속 유지.
    private float[] rayDistances; // rayCount개. 각도별로 "벽 또는 시야 끝까지"의 거리(월드 단위).
    private RectInt prevRect;
    private bool hasPrevRect = false;

    void Start()
    {
        fogTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        fogTexture.filterMode = FilterMode.Bilinear;

        explored = new byte[resolution * resolution];
        rayDistances = new float[rayCount];

        byte a0 = (byte)(unexploredAlpha * 255);
        var initColors = new Color32[resolution * resolution];
        for (int i = 0; i < initColors.Length; i++) initColors[i] = new Color32(0, 0, 0, a0);
        fogTexture.SetPixels32(initColors);
        fogTexture.Apply(false);

        if (fogPlaneRenderer != null)
        {
            var mat = fogPlaneRenderer.material;
            mat.SetTexture(FogTexId, fogTexture);
            mat.SetVector(WorldOriginId, new Vector4(worldOrigin.x, worldOrigin.y, 0f, 0f));
            mat.SetVector(WorldSizeId, new Vector4(worldSize.x, worldSize.y, 0f, 0f));
        }
    }

    void Update()
    {
        if (player == null || fogTexture == null) return;

        CastVisionRays();
        BakeFog();
    }

    // 정면 기준 -half~+half 각도로 rayCount개 레이를 쏴서, 각도별 "벽에 막히는 거리"를 구한다.
    void CastVisionRays()
    {
        float half = visionAngle * 0.5f;
        Vector3 origin = player.position;
        Vector3 forward = player.forward; forward.y = 0f; forward.Normalize();

        for (int i = 0; i < rayCount; i++)
        {
            float t = (rayCount == 1) ? 0f : (float)i / (rayCount - 1);
            float angle = Mathf.Lerp(-half, half, t);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * forward;

            float dist = visionRadius;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, visionRadius, obstacleMask))
                dist = hit.distance;

            rayDistances[i] = dist;
        }
    }

    void BakeFog()
    {
        float half = visionAngle * 0.5f;
        Vector3 origin = player.position;
        Vector3 forward = player.forward; forward.y = 0f; forward.Normalize();

        Vector2Int center = WorldToPixel(new Vector2(origin.x, origin.z));
        int boundRadius = Mathf.CeilToInt((visionRadius / worldSize.x) * resolution) + 2;

        RectInt curRect = new RectInt(center.x - boundRadius, center.y - boundRadius,
                                       boundRadius * 2 + 1, boundRadius * 2 + 1);
        RectInt unionRect = hasPrevRect ? Union(prevRect, curRect) : curRect;
        unionRect = ClampToTexture(unionRect, resolution);

        if (unionRect.width <= 0 || unionRect.height <= 0)
        {
            prevRect = curRect;
            hasPrevRect = true;
            return;
        }

        byte unexploredA = (byte)(unexploredAlpha * 255);
        byte dimA = (byte)(exploredDimAlpha * 255);

        var block = new Color32[unionRect.width * unionRect.height];
        for (int y = 0; y < unionRect.height; y++)
        {
            int py = unionRect.yMin + y;
            for (int x = 0; x < unionRect.width; x++)
            {
                int px = unionRect.xMin + x;
                int idx = py * resolution + px;

                Vector2 worldXZ = PixelToWorld(px, py);
                Vector3 toPixel = new Vector3(worldXZ.x - origin.x, 0f, worldXZ.y - origin.z);
                float dist = toPixel.magnitude;

                float signedAngle = Vector3.SignedAngle(forward, toPixel, Vector3.up);
                float absAngle = Mathf.Abs(signedAngle);

                float outside; // 0 = 완전히 보임, 1 = 완전히 안 보임(경계 밖)
                if (absAngle > half + angleFeatherDeg || dist > visionRadius + edgeFeatherWorld)
                {
                    outside = 1f;
                }
                else
                {
                    float allowedDist = SampleRayDistance(signedAngle, half);
                    float radialOutside = Mathf.Clamp01((dist - (allowedDist - edgeFeatherWorld)) / Mathf.Max(edgeFeatherWorld, 0.001f));
                    float angularOutside = Mathf.Clamp01((absAngle - (half - angleFeatherDeg)) / Mathf.Max(angleFeatherDeg, 0.001f));
                    outside = Mathf.Max(radialOutside, angularOutside);
                }

                if (outside < 1f) explored[idx] = 1;

                byte a = (explored[idx] == 0) ? unexploredA : (byte)Mathf.Lerp(0f, dimA, outside);
                block[y * unionRect.width + x] = new Color32(0, 0, 0, a);
            }
        }

        fogTexture.SetPixels32(unionRect.xMin, unionRect.yMin, unionRect.width, unionRect.height, block);
        fogTexture.Apply(false);

        prevRect = curRect;
        hasPrevRect = true;
    }

    // signedAngle(-half~+half)에 해당하는 rayDistances를 선형보간해서 "그 방향으로 얼마나 멀리까지 보이는지" 반환.
    float SampleRayDistance(float signedAngle, float half)
    {
        float clamped = Mathf.Clamp(signedAngle, -half, half);
        float t = Mathf.InverseLerp(-half, half, clamped) * (rayCount - 1);
        int i0 = Mathf.FloorToInt(t);
        int i1 = Mathf.Min(i0 + 1, rayCount - 1);
        float frac = t - i0;
        return Mathf.Lerp(rayDistances[i0], rayDistances[i1], frac);
    }

    Vector2Int WorldToPixel(Vector2 worldXZ)
    {
        float u = (worldXZ.x - worldOrigin.x) / worldSize.x;
        float v = (worldXZ.y - worldOrigin.y) / worldSize.y;
        return new Vector2Int(Mathf.RoundToInt(u * resolution), Mathf.RoundToInt(v * resolution));
    }

    Vector2 PixelToWorld(int px, int py)
    {
        float x = worldOrigin.x + ((px + 0.5f) / resolution) * worldSize.x;
        float z = worldOrigin.y + ((py + 0.5f) / resolution) * worldSize.y;
        return new Vector2(x, z);
    }

    static RectInt Union(RectInt a, RectInt b)
    {
        int xMin = Mathf.Min(a.xMin, b.xMin);
        int yMin = Mathf.Min(a.yMin, b.yMin);
        int xMax = Mathf.Max(a.xMax, b.xMax);
        int yMax = Mathf.Max(a.yMax, b.yMax);
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    static RectInt ClampToTexture(RectInt r, int size)
    {
        int xMin = Mathf.Clamp(r.xMin, 0, size - 1);
        int yMin = Mathf.Clamp(r.yMin, 0, size - 1);
        int xMax = Mathf.Clamp(r.xMax, 0, size);
        int yMax = Mathf.Clamp(r.yMax, 0, size);
        return new RectInt(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
    }
}
