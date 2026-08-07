using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Tooltip("발사할 투사체 프리팹")]
    public GameObject projectilePrefab;

    [Tooltip("몇 초마다 발사할지")]
    public float fireInterval = 0.8f;

    [Tooltip("이 거리 안의 적만 조준")]
    public float range = 8f;

    [Header("스킬로 강화되는 값")]
    [Tooltip("투사체 기본 데미지에 더해지는 보너스")]
    public int bonusDamage = 0;

    [Tooltip("한 번에 발사하는 투사체 수 (부채꼴로 퍼짐)")]
    public int projectilesPerShot = 1;

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
        Vector2 baseDir = targetPos - transform.position;

        // 여러 발이면 12도 간격 부채꼴로 퍼뜨림
        for (int i = 0; i < projectilesPerShot; i++)
        {
            float angleOffset = (i - (projectilesPerShot - 1) * 0.5f) * 12f;
            Vector2 dir = Quaternion.Euler(0f, 0f, angleOffset) * baseDir;

            GameObject go = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Projectile proj = go.GetComponent<Projectile>();
            proj.SetDirection(dir);

            // 달의 플레이어 강화 배율 + 스킬 공격력 %보너스 적용 (근접과 같은 공식)
            float power = 1f;
            if (GameManager.Instance != null && GameManager.Instance.CurrentMoon != null)
                power = GameManager.Instance.CurrentMoon.playerPowerMultiplier;

            float skillMult = 1f + (SkillSystem.Instance != null ? SkillSystem.Instance.damageBonus : 0f);
            proj.damage = Mathf.Max(1, Mathf.RoundToInt((proj.damage + bonusDamage) * skillMult * power));
        }
    }
}
