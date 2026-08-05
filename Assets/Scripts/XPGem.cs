using UnityEngine;

public class XPGem : MonoBehaviour
{
    [Tooltip("주는 경험치")]
    public int xpValue = 1;

    [Tooltip("이 거리 안에 오면 플레이어에게 끌려감")]
    public float magnetRange = 1.1f;

    public float magnetSpeed = 7f;

    Transform player;

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        // 스킬(달의 인력)로 늘어난 획득 범위 반영
        float range = magnetRange + (SkillSystem.Instance != null ? SkillSystem.Instance.magnetBonus : 0f);

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist < range)
        {
            transform.position = Vector2.MoveTowards(
                transform.position, player.position, magnetSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerLevel lvl = other.GetComponent<PlayerLevel>();
            if (lvl != null) lvl.AddXP(xpValue);
            SoundManager.Instance?.PlayPickup();
            Destroy(gameObject);
        }
    }
}
