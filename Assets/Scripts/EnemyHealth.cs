using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Tooltip("맞을 수 있는 횟수")]
    public int maxHp = 2;

    [Tooltip("죽을 때 떨어뜨릴 경험치 보석 프리팹")]
    public GameObject gemPrefab;

    int hp;

    void Awake()
    {
        hp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0)
        {
            if (gemPrefab != null)
                Instantiate(gemPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
