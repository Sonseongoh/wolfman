using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    [Tooltip("추적 속도 (플레이어보다 느리게)")]
    public float moveSpeed = 2.5f;

    Rigidbody2D rb;
    Transform player;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // "Player" 태그가 붙은 오브젝트를 찾아 추적 대상으로 삼는다
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
    }
}
