using System.Collections;
using UnityEngine;

/// <summary>
/// 돌진형 적 (#28). 평소엔 EnemyChase로 추적하다가, 사거리 안에 들어오면
/// 붉게 달아오르는 예비 동작(이때 피해야 함) 후 직선으로 돌진한다.
/// 돌진 방향은 예비 동작이 끝나는 순간 고정 — 옆으로 흘리면 피할 수 있다.
/// </summary>
[RequireComponent(typeof(EnemyChase))]
public class EnemyCharge : MonoBehaviour
{
    [Tooltip("플레이어가 이 거리 안이면 돌진 준비 시작")]
    public float triggerRange = 6f;

    [Tooltip("예비 동작(조준) 시간 — 이 동안 붉게 변하며 멈춘다")]
    public float windupTime = 0.6f;

    [Tooltip("돌진 속도 (플레이어 이속 5보다 훨씬 빠르게)")]
    public float chargeSpeed = 11f;

    [Tooltip("돌진 지속 시간(초)")]
    public float chargeTime = 0.5f;

    [Tooltip("돌진 후 다음 돌진까지 대기 — 그동안은 일반 추적")]
    public float cooldown = 3.5f;

    Rigidbody2D rb;
    EnemyChase chase;
    SpriteRenderer sr;
    Transform player;
    float cooldownTimer;
    bool charging;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        chase = GetComponent<EnemyChase>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        cooldownTimer = 1.2f; // 스폰 직후 바로 달려들지 않게
    }

    void Update()
    {
        if (charging || player == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        if (Vector2.Distance(player.position, transform.position) <= triggerRange)
            StartCoroutine(ChargeRoutine());
    }

    IEnumerator ChargeRoutine()
    {
        charging = true;
        chase.enabled = false; // 조준·돌진 동안 일반 추적 정지

        // 1) 예비 동작: 멈춰서 점점 붉게 달아오른다
        Color prev = sr != null ? sr.color : Color.white;
        float t = 0f;
        while (t < windupTime)
        {
            t += Time.deltaTime;
            rb.linearVelocity = Vector2.zero;
            if (sr != null)
                sr.color = Color.Lerp(prev, new Color(1f, 0.35f, 0.3f), t / windupTime);
            yield return null;
        }
        if (sr != null) sr.color = prev;

        // 2) 이 순간의 플레이어 방향으로 고정하고 돌진
        if (player != null)
        {
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            if (sr != null && dir.x != 0f) sr.flipX = dir.x < 0f;

            t = 0f;
            while (t < chargeTime)
            {
                t += Time.fixedDeltaTime;
                rb.linearVelocity = dir * chargeSpeed; // 매 물리 스텝 유지 (돌진 중 넉백 무시)
                yield return new WaitForFixedUpdate();
            }
        }

        // 3) 회복: 일반 추적으로 복귀
        rb.linearVelocity = Vector2.zero;
        chase.enabled = true;
        cooldownTimer = cooldown;
        charging = false;
    }
}
