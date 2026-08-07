using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;

    [Tooltip("빗나갔을 때 몇 초 뒤 사라질지")]
    public float lifetime = 3f;

    Vector2 direction;

    // 발사할 때 PlayerAttack이 방향을 정해준다
    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;

        // 진행 방향으로 회전 (참격 등 방향 있는 스프라이트용).
        // 왼쪽으로 갈 땐 위아래가 뒤집혀 보이므로 flipY로 호 방향을 되살린다
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipY = direction.x < 0f;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            // 총알 진행 방향으로 밀려나게 넉백 방향 전달
            enemy.TakeDamage(damage, direction);
            Destroy(gameObject);
        }
    }
}
