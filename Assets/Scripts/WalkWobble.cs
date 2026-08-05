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
    float punch;      // 공격 순간 몸이 부풀었다 돌아오는 연출 (#38)
    float attackTilt; // 공격 순간 방향으로 기울었다 돌아오는 연출 (도)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
    }

    /// <summary>공격 등 임팩트 순간 호출 — 몸이 순간 부풀었다 빠르게 원래대로</summary>
    public void Punch(float amount = 0.25f)
    {
        punch = Mathf.Max(punch, amount);
    }

    /// <summary>공격 순간 몸 기울이기 — 지정 각도로 확 기울었다 빠르게 복귀</summary>
    public void SwingTilt(float degrees)
    {
        attackTilt = degrees;
    }

    void Update()
    {
        bool moving = rb != null && rb.linearVelocity.sqrMagnitude > 0.05f;

        // 펀치·기울임 감쇠 (0.15초쯤에 걸쳐 빠르게 복귀)
        if (punch > 0f) punch = Mathf.Max(0f, punch - Time.deltaTime * 1.8f);
        attackTilt = Mathf.MoveTowards(attackTilt, 0f, 130f * Time.deltaTime);
        float punchScale = 1f + punch;

        if (moving)
        {
            phase += Time.deltaTime * wobbleSpeed;

            float tilt = Mathf.Sin(phase) * tiltAngle;
            float squash = 1f + Mathf.Abs(Mathf.Sin(phase)) * squashAmount;

            transform.rotation = Quaternion.Euler(0f, 0f, tilt + attackTilt);
            transform.localScale = new Vector3(
                baseScale.x * punchScale,
                baseScale.y * squash * punchScale,
                baseScale.z);
        }
        else
        {
            // 멈추면 부드럽게 원위치 (펀치·기울임은 유지)
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.Euler(0f, 0f, attackTilt), 14f * Time.deltaTime);
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                new Vector3(baseScale.x * punchScale, baseScale.y * punchScale, baseScale.z),
                12f * Time.deltaTime);
            phase = 0f;
        }
    }
}
