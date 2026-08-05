using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Tooltip("최대 체력")]
    public int maxHp = 5;

    [Tooltip("피격 후 무적 시간(초) — 닿아있는 동안 연속으로 깎이는 것 방지")]
    public float invincibleTime = 1f;

    int hp;
    float invincibleTimer;
    bool isDead;
    SpriteRenderer sr;

    /// <summary>게임오버 상태인지 (HitStop 등이 시간 정지 유지 판단에 사용)</summary>
    public bool IsDead => isDead;

    void Awake()
    {
        hp = maxHp;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 무적 시간 동안 깜빡여서 시각적으로 표시
        if (invincibleTimer > 0f)
        {
            invincibleTimer -= Time.deltaTime;
            if (sr != null)
                sr.enabled = Mathf.FloorToInt(invincibleTimer * 10f) % 2 == 0;

            if (invincibleTimer <= 0f && sr != null) sr.enabled = true;
        }

        // 게임오버 상태에서 R 키로 재시작
        if (isDead && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // 적과 닿아 있으면 (무적 시간이 아닐 때) 데미지 — 달의 적 공격력 배율 적용
        if (!isDead && invincibleTimer <= 0f && collision.gameObject.GetComponent<EnemyChase>() != null)
        {
            int dmg = 1;
            if (GameManager.Instance != null && GameManager.Instance.CurrentMoon != null)
                dmg = Mathf.Max(1, Mathf.RoundToInt(GameManager.Instance.CurrentMoon.enemyDamageMultiplier));

            TakeDamage(dmg);
        }
    }

    // 레벨업 강화: 최대체력 +amount, 전체 회복
    public void IncreaseMaxHp(int amount)
    {
        maxHp += amount;
        hp = maxHp;
    }

    /// <summary>회복 오브 등으로 체력 회복 (#32) — 최대치를 넘지 않음</summary>
    public void Heal(int amount)
    {
        if (isDead) return;

        hp = Mathf.Min(maxHp, hp + amount);
        DamageNumber.Spawn(transform.position, $"+{amount}", new Color(0.4f, 1f, 0.5f), 1.1f);
    }

    void TakeDamage(int amount)
    {
        hp -= amount;
        invincibleTimer = invincibleTime;

        // 피격 피드백 (#31): 붉은 데미지 숫자 + 강한 흔들림 + 히트스톱
        DamageNumber.Spawn(transform.position, $"-{amount}", new Color(1f, 0.35f, 0.3f), 1.25f);
        CameraFollow.Shake(0.22f, 0.25f);
        HitStop.Do(0.06f);

        if (hp <= 0)
        {
            isDead = true;
            Time.timeScale = 0f; // 게임 일시정지
        }
    }

    // 임시 UI (나중에 제대로 된 UI로 교체 예정)
    void OnGUI()
    {
        GUIStyle style = new GUIStyle { fontSize = 28, normal = { textColor = Color.white } };
        GUI.Label(new Rect(20, 20, 300, 40), $"HP: {hp} / {maxHp}", style);

        if (isDead)
        {
            int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;
            int level = GetComponent<PlayerLevel>() != null ? GetComponent<PlayerLevel>().level : 1;

            GUIStyle big = new GUIStyle
            {
                fontSize = 48,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.red }
            };
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height),
                $"GAME OVER\n\nWAVE {wave}까지 생존  ·  Lv.{level}\n\nR 키로 재시작", big);
        }
    }
}
