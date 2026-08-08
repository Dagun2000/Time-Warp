using UnityEngine;
using UnityEngine.InputSystem;

// FreeRoam/Executing: 쿼터뷰로 플레이어를 따라다님.
// Planning(예약 구성) 중에만 직각 탑다운으로 전환 — 실행 시작되면 다시 쿼터뷰로 풀리며 참격이 재생됨.
public class CameraRig : MonoBehaviour
{
    [SerializeField] private Transform target; // Player

    [Header("쿼터뷰 (1배속)")]
    [SerializeField] private Vector3 quarterOffset = new Vector3(0f, 12f, -9f);
    [SerializeField] private Vector3 quarterEuler = new Vector3(50f, 0f, 0f);

    [Header("탑다운 (예약 중)")]
    [SerializeField] private float topDownHeight = 15f;
    [SerializeField] private Vector3 topDownEuler = new Vector3(90f, 0f, 0f);

    [Header("탑다운 줌 (스크롤)")]
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float minTopDownHeight = 8f;
    [SerializeField] private float maxTopDownHeight = 25f;

    [Header("전환 속도")]
    [SerializeField] private float blendSpeed = 4f; // 클수록 빠르게 전환

    private float currentTopDownHeight;

    void Awake()
    {
        currentTopDownHeight = topDownHeight;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 예약 "구성" 중(Planning)에만 탑다운. 실행(Executing) 시작되면 쿼터뷰로 풀림 — 참격이 쿼터뷰로 재생.
        bool topDown = PlayerPlanner.Instance != null && PlayerPlanner.Instance.IsPlanning;

        if (topDown && Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                currentTopDownHeight = Mathf.Clamp(
                    currentTopDownHeight - scroll * zoomSpeed * 0.01f,
                    minTopDownHeight, maxTopDownHeight);
        }

        Vector3 desiredPos = topDown
            ? target.position + Vector3.up * currentTopDownHeight
            : target.position + quarterOffset;
        Quaternion desiredRot = Quaternion.Euler(topDown ? topDownEuler : quarterEuler);

        float t = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, t);
    }
}
