using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    [Tooltip("추적 속도 (플레이어보다 느리게)")]
    public float moveSpeed = 2.5f;

    [Tooltip("0 = 근접형(끝까지 붙는다). 0보다 크면 이 거리를 유지하는 원거리형 — 너무 가까우면 물러난다 (#28)")]
    public float keepDistance = 0f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Transform player;
    float staggerTimer; // 넉백으로 밀려나는 동안 추적 정지

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // 걷기 흔들림 연출 자동 장착
        if (GetComponent<WalkWobble>() == null) gameObject.AddComponent<WalkWobble>();
    }

    /// <summary>피격 시 밀려남. duration 동안 추적을 멈추고 넉백 속도를 유지한다.</summary>
    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        staggerTimer = duration;
        rb.linearVelocity = direction.normalized * force;
    }

    void Start()
    {
        // "Player" 태그가 붙은 오브젝트를 찾아 추적 대상으로 삼는다
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        // 넉백 중에는 추적으로 속도를 덮어쓰지 않는다
        if (staggerTimer > 0f)
        {
            staggerTimer -= Time.fixedDeltaTime;
            return;
        }

        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = player.position - transform.position;
        Vector2 dir = toPlayer.normalized;

        if (keepDistance > 0f)
        {
            // 원거리형: 멀면 접근, 너무 가까우면 후퇴, 적정 거리면 정지 (#28)
            float dist = toPlayer.magnitude;
            if (dist > keepDistance * 1.15f) rb.linearVelocity = dir * moveSpeed;
            else if (dist < keepDistance * 0.7f) rb.linearVelocity = -dir * moveSpeed;
            else rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.linearVelocity = dir * moveSpeed;
        }

        // 항상 플레이어 쪽을 본다 — 후퇴 중에도 (원본이 오른쪽을 봄)
        if (sr != null && dir.x != 0f)
            sr.flipX = dir.x < 0f;
    }
}
