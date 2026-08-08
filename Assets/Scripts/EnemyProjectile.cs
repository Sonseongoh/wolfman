using UnityEngine;

/// <summary>
/// 적이 쏘는 투사체 (#28). 플레이어에게 닿으면 데미지 (무적·달 배율은 PlayerHealth가 처리).
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 6f;
    public int damage = 1;

    [Tooltip("빗나갔을 때 몇 초 뒤 사라질지")]
    public float lifetime = 5f;

    Vector2 direction;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
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
        PlayerHealth player = other.GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeEnemyHit(damage);
            Destroy(gameObject);
        }
    }
}
