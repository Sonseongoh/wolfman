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
    bool isPaused;
    int lostGoldOnDeath;
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

        // ESC 일시정지 토글 (레벨업·스킬 선택 중엔 무시)
        if (!isDead && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool levelChoosing = GetComponent<PlayerLevel>()?.IsChoosing ?? false;
            bool skillChoosing = SkillSystem.Instance?.IsChoosing ?? false;
            if (!levelChoosing && !skillChoosing)
            {
                isPaused = !isPaused;
                Time.timeScale = isPaused ? 0f : 1f;
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // 적과 닿아 있으면 데미지
        if (collision.gameObject.GetComponent<EnemyChase>() != null)
            TakeEnemyHit(1);
    }

    /// <summary>적 공격 공통 진입점 (접촉·투사체 #28) — 무적/사망 체크 + 달의 적 공격력 배율 적용</summary>
    public void TakeEnemyHit(int baseDamage)
    {
        if (isDead || invincibleTimer > 0f) return;

        int dmg = baseDamage;
        if (GameManager.Instance != null && GameManager.Instance.CurrentMoon != null)
            dmg = Mathf.Max(1, Mathf.RoundToInt(baseDamage * GameManager.Instance.CurrentMoon.enemyDamageMultiplier));

        TakeDamage(dmg);
    }

    /// <summary>최대 체력만 +amount (회복 없음 — 회복은 Heal/FullHeal로 따로)</summary>
    public void IncreaseMaxHp(int amount)
    {
        maxHp += amount;
    }

    /// <summary>전체 회복 (에픽 스킬 등)</summary>
    public void FullHeal()
    {
        if (isDead) return;
        int missing = maxHp - hp;
        if (missing > 0) Heal(missing);
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
            lostGoldOnDeath = CurrencyManager.Instance?.TempGold ?? 0;
            CurrencyManager.Instance?.LoseTempGold();
            Time.timeScale = 0f;
        }
    }

    static void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 임시 UI (나중에 제대로 된 UI로 교체 예정)
    void OnGUI()
    {
        GUIStyle hpStyle = new GUIStyle { fontSize = 28, normal = { textColor = Color.white } };
        GUI.Label(new Rect(20, 20, 300, 40), $"HP: {hp} / {maxHp}", hpStyle);

        // 일시정지·설정 버튼 (우상단) — 게임 중, 죽지 않았을 때
        if (!isDead && !isPaused)
        {
            bool levelChoosing = GetComponent<PlayerLevel>()?.IsChoosing ?? false;
            bool skillChoosing = SkillSystem.Instance?.IsChoosing ?? false;
            if (!levelChoosing && !skillChoosing)
            {
                float bSize = 48f, margin = 8f;
                // ⏸ 왼쪽, ⚙ 맨 오른쪽
                if (GUI.Button(new Rect(Screen.width - bSize * 2 - margin * 2, margin, bSize, bSize), "⏸"))
                {
                    isPaused = true;
                    Time.timeScale = 0f;
                }
                GUI.Button(new Rect(Screen.width - bSize - margin, margin, bSize, bSize), "⚙");
            }
        }

        // 일시정지 오버레이
        if (isPaused)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float pw = 320f, ph = 220f;
            float px = (Screen.width - pw) * 0.5f, py = (Screen.height - ph) * 0.5f;

            // 패널 테두리 + 배경
            GUI.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
            GUI.DrawTexture(new Rect(px - 3, py - 3, pw + 6, ph + 6), Texture2D.whiteTexture);
            GUI.color = new Color(0.08f, 0.07f, 0.14f, 1f);
            GUI.DrawTexture(new Rect(px, py, pw, ph), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle title = new GUIStyle
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(px, py + 14, pw, 50), "일시정지", title);

            if (GUI.Button(new Rect(px + 40, py + 82, pw - 80, 52), "계속하기"))
            {
                isPaused = false;
                Time.timeScale = 1f;
            }
            if (GUI.Button(new Rect(px + 40, py + 148, pw - 80, 52), "재시작"))
                Restart();

            return; // HP 라벨 아래 다른 UI 안 그리도록
        }

        // 게임오버 화면
        if (isDead)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int wave  = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;
            int kills = WaveManager.Instance != null ? WaveManager.Instance.KillCount   : 0;

            GUIStyle big = new GUIStyle
            {
                fontSize = 56,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.red }
            };
            GUI.Label(new Rect(0, Screen.height * 0.1f, Screen.width, 70), "GAME OVER", big);

            GUIStyle stat = new GUIStyle
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            string lostText = lostGoldOnDeath > 0 ? $"\n잃은 골드: {lostGoldOnDeath} G" : "";
            GUI.Label(new Rect(0, Screen.height * 0.28f, Screen.width, 120),
                $"WAVE {wave}까지 생존  ·  처치: {kills}마리{lostText}", stat);

            // 획득한 스킬 목록
            if (SkillSystem.Instance != null && SkillSystem.Instance.acquired.Count > 0)
            {
                GUIStyle skillHeader = new GUIStyle
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.82f, 0.2f) }
                };
                GUIStyle skillEntry = new GUIStyle
                {
                    fontSize = 19,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
                };

                float skillY = Screen.height * 0.48f;
                GUI.Label(new Rect(0, skillY, Screen.width, 28), "획득한 스킬", skillHeader);
                skillY += 32f;
                foreach (var s in SkillSystem.Instance.acquired)
                {
                    GUI.Label(new Rect(0, skillY, Screen.width, 26), $"· {s.skillName}  {s.description}", skillEntry);
                    skillY += 26f;
                }
            }

            float btnW = 240f, btnH = 60f;
            if (GUI.Button(new Rect((Screen.width - btnW) * 0.5f, Screen.height * 0.82f, btnW, btnH), "재시작"))
                Restart();
        }
    }
}
