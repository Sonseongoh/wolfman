using UnityEngine;

/// <summary>
/// 프로시저럴 걷기 연출: 이동 중일 때 몸을 살짝 기울이고(틸트) 위아래로 통통(스쿼시).
/// 스프라이트 한 장으로 걷는 느낌을 내는 기법 — 애니메이션 에셋 확보 전까지의 대체재.
/// PlayerMovement / EnemyChase가 자동으로 붙여준다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WalkWobble : MonoBehaviour
{
    [Tooltip("흔들림 빠르기 (걸음 속도감)")]
    public float wobbleSpeed = 9f;

    [Tooltip("좌우로 기우는 최대 각도(도)")]
    public float tiltAngle = 5f;

    [Tooltip("위아래 통통거리는 정도 (0.06 = 6%)")]
    public float squashAmount = 0.06f;

    Rigidbody2D rb;
    Vector3 baseScale;
    float phase;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        bool moving = rb != null && rb.linearVelocity.sqrMagnitude > 0.05f;

        if (moving)
        {
            phase += Time.deltaTime * wobbleSpeed;

            float tilt = Mathf.Sin(phase) * tiltAngle;
            float squash = 1f + Mathf.Abs(Mathf.Sin(phase)) * squashAmount;

            transform.rotation = Quaternion.Euler(0f, 0f, tilt);
            transform.localScale = new Vector3(baseScale.x, baseScale.y * squash, baseScale.z);
        }
        else
        {
            // 멈추면 부드럽게 원위치
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, 12f * Time.deltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 12f * Time.deltaTime);
            phase = 0f;
        }
    }
}
