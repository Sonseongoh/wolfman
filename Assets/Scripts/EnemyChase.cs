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

        // 몸 크기를 발밑 콜라이더에서 계산 (탱커처럼 큰 적은 더 두껍게 감지).
        // 콜라이더가 둘로 나뉜 뒤로(#143) 반드시 솔리드 쪽을 골라야 한다 — 장애물 회피는
        // "실제로 막히는 폭이 얼마인가"의 문제라서, 피격용 몸통 트리거를 재면 과하게 피한다.
        Collider2D foot = null;
        foreach (Collider2D c in GetComponents<Collider2D>())
            if (!c.isTrigger) { foot = c; break; }
        if (foot != null)
            bodyRadius = Mathf.Max(foot.bounds.extents.x, foot.bounds.extents.y) * 0.9f;

        // 걷기 흔들림 연출·Y 정렬 자동 장착
        if (GetComponent<WalkWobble>() == null) gameObject.AddComponent<WalkWobble>();
        if (GetComponent<YSort>() == null) gameObject.AddComponent<YSort>();
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

    float steerSign; // 최근에 돌기로 한 우회 방향 (+1/-1)
    float steerHold; // 그 방향을 유지할 남은 시간 — 좌우로 매 프레임 번갈아 떠는 것 방지
    float bodyRadius = 0.45f; // 몸 크기 — Awake에서 콜라이더로부터 계산

    /// <summary>
    /// 진행 방향 앞에 장애물(타일맵 콜라이더)이 있으면 벽면을 따라 미끄러지는 방향으로 바꾼다.
    /// 몸 두께만큼의 CircleCast라 모서리 스침도 잡고, 안쪽 모서리(ㄱ자)에선 반대쪽으로 돌며,
    /// 한번 정한 우회 방향은 잠시 유지해 제자리 떨림을 막는다. 본격 길찾기 대신 쓰는 가벼운 우회.
    /// </summary>
    Vector2 SteerAroundObstacles(Vector2 dir)
    {
        if (steerHold > 0f) steerHold -= Time.fixedDeltaTime;

        RaycastHit2D blocked = CastObstacle(dir);
        if (blocked.collider == null)
        {
            steerHold = 0f;
            return dir;
        }

        // 이미 벽에 파묻힌 상태(넉백 등)면 미끄러지기 전에 벽 바깥으로 빠져나온다
        if (blocked.distance < 0.05f)
        {
            steerHold = 0f;
            return blocked.normal;
        }

        Vector2 tangent = Vector2.Perpendicular(blocked.normal);

        // 이미 돌던 방향이 있으면 유지, 없으면 원래 가려던 쪽에 가까운 쪽으로
        float sign = steerHold > 0f ? steerSign
            : (Vector2.Dot(tangent, dir) >= 0f ? 1f : -1f);
        Vector2 slide = tangent * sign;

        // 그 접선마저 막혀 있으면(안쪽 모서리) 반대로 돈다
        if (CastObstacle(slide).collider != null)
        {
            sign = -sign;
            slide = tangent * sign;
        }

        steerSign = sign;
        steerHold = 0.35f;
        return slide;
    }

    /// <summary>몸 두께만큼의 원으로 앞을 살핀다 — 타일맵(합쳐진 것 포함)과 큰 장애물 프리팹만 장애물로 친다</summary>
    RaycastHit2D CastObstacle(Vector2 dir)
    {
        foreach (RaycastHit2D h in Physics2D.CircleCastAll(transform.position, bodyRadius, dir, obstacleProbe))
        {
            if (h.collider is TilemapCollider2D || h.collider is CompositeCollider2D) return h;
            if (h.collider.GetComponent<ObstacleProp>() != null) return h;
        }
        return default;
    }
}
