using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    [Tooltip("추적 속도 (플레이어보다 느리게)")]
    public float moveSpeed = 2.5f;

    [Tooltip("0 = 근접형(끝까지 붙는다). 0보다 크면 이 거리를 유지하는 원거리형 — 너무 가까우면 물러난다 (#28)")]
    public float keepDistance = 0f;

    [Tooltip("넉백 면역 시간(초) — 한 번 밀린 뒤 이 시간 동안은 다시 안 밀린다. 공속 스택으로 무한 밀어내기(스턴락) 방지")]
    public float knockbackImmunity = 0.5f;

    [Tooltip("장애물 감지 거리 — 앞이 막혀 있으면 벽면을 따라 미끄러져 우회 (끼임 방지)")]
    public float obstacleProbe = 0.9f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Transform player;
    float staggerTimer;  // 넉백으로 밀려나는 동안 추적 정지
    float immunityTimer; // 넉백 면역 남은 시간

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // 걷기 흔들림 연출 자동 장착
        if (GetComponent<WalkWobble>() == null) gameObject.AddComponent<WalkWobble>();
    }

    /// <summary>피격 시 밀려남. duration 동안 추적을 멈추고 넉백 속도를 유지한다.
    /// 면역 시간 중이면 무시 — 데미지는 그대로 들어가되 밀리지만 않는다.</summary>
    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (immunityTimer > 0f) return;

        immunityTimer = knockbackImmunity;
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
        if (immunityTimer > 0f) immunityTimer -= Time.fixedDeltaTime;

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
        Vector2 dir = SteerAroundObstacles(toPlayer.normalized);

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
        if (sr != null && toPlayer.x != 0f)
            sr.flipX = toPlayer.x < 0f;
    }

    /// <summary>
    /// 진행 방향 앞에 장애물(타일맵 콜라이더)이 있으면 벽면을 따라 미끄러지는 방향으로 바꾼다.
    /// 본격 길찾기 대신 쓰는 가벼운 우회 — 뭉쳐오는 적 무리엔 이걸로 충분하다 (끼임 방지).
    /// </summary>
    Vector2 SteerAroundObstacles(Vector2 dir)
    {
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(transform.position, dir, obstacleProbe))
        {
            if (!(hit.collider is TilemapCollider2D)) continue; // 적·플레이어끼리는 무시

            // 벽의 접선 방향 중 원래 가려던 쪽에 가까운 쪽으로 미끄러진다
            Vector2 tangent = Vector2.Perpendicular(hit.normal);
            if (Vector2.Dot(tangent, dir) < 0f) tangent = -tangent;
            return tangent;
        }
        return dir;
    }
}
