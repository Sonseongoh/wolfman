using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Tooltip("발사할 투사체 프리팹")]
    public GameObject projectilePrefab;

    [Tooltip("몇 초마다 발사할지")]
    public float fireInterval = 0.8f;

    [Tooltip("이 거리 안의 적만 조준")]
    public float range = 8f;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= fireInterval)
        {
            EnemyHealth target = FindNearestEnemy();
            if (target != null)
            {
                timer = 0f;
                Fire(target.transform.position);
            }
        }
    }

    EnemyHealth FindNearestEnemy()
    {
        EnemyHealth[] enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        EnemyHealth nearest = null;
        float best = range;

        foreach (EnemyHealth e in enemies)
        {
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < best)
            {
                best = dist;
                nearest = e;
            }
        }
        return nearest;
    }

    void Fire(Vector3 targetPos)
    {
        GameObject go = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        go.GetComponent<Projectile>().SetDirection(targetPos - transform.position);
    }
}
