using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Tooltip("최대 체력")]
    public int maxHp = 5;

    [Tooltip("피격 후 무적 시간(초) — 닿아있는 동안 연속으로 깎이는 것 방지")]
    public float invincibleTime = 1f;

    [Tooltip("일시정지 버튼 아이콘 (Assets/Art/PauseBtn.png 할당)")]
    public Texture2D pauseButtonIcon;

    int hp;
    float invincibleTimer;
    bool isDead;
    bool isPaused;
    int lostGoldOnDeath;
    int earnedGoldOnDeath;
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

        // 테스트용: K 키로 즉시 게임오버
        if (!isDead && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
            TakeDamage(9999);

        // ESC 일시정지 토글 (레벨업·스킬 선택 중엔 무시)
        if (!isDead && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool skillChoosing = SkillSystem.Instance?.IsChoosing ?? false;
            if (!skillChoosing)
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
            earnedGoldOnDeath = CurrencyManager.Instance?.ConfirmedGold ?? 0;
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

        // ⏸ 버튼 — 우상단 작게, 사냥 중에만 (달 선택·스킬 선택 중엔 숨김)
        if (!isDead && !isPaused)
        {
            bool skillChoosing = SkillSystem.Instance?.IsChoosing ?? false;
            bool inReveal = WaveManager.Instance?.IsInMoonReveal ?? true;
            if (!skillChoosing && !inReveal)
            {
                // 금고 패널(width=190, x=Screen.width-206, y=56, height=72) 바로 왼쪽에 정렬
                float bSize = 26f;
                float bx = Screen.width - 190f - 16f - bSize - 24f;
                float by = 12f;
                GUIContent pauseContent = pauseButtonIcon != null
                    ? new GUIContent(pauseButtonIcon)
                    : new GUIContent("⏸");
                if (GUI.Button(new Rect(bx, by, bSize, bSize), pauseContent, GUIStyle.none))
                {
                    isPaused = true;
                    Time.timeScale = 0f;
                }
            }
        }

        // 일시정지 오버레이
        if (isPaused)
        {
            // 거의 검정 오버레이
            GUI.color = new Color(0.04f, 0.04f, 0.06f, 0.9f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            // 상하 차가운 회색 라인
            GUI.color = new Color(0.5f, 0.5f, 0.55f, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, 3), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - 3, Screen.width, 3), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 버튼 — 옵션 | 계속하기 (화면 비례로 모바일 대응)
            float btnW = Screen.width * 0.28f, btnH = Screen.height * 0.1f, gap = Screen.width * 0.04f;
            float totalW = btnW * 2 + gap;
            float bx = (Screen.width - totalW) * 0.5f;
            float by = (Screen.height - btnH) * 0.5f;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(btnH * 0.38f),
                fontStyle = FontStyle.Bold
            };

            GUI.Button(new Rect(bx, by, btnW, btnH), "옵션", btnStyle);
            if (GUI.Button(new Rect(bx + btnW + gap, by, btnW, btnH), "계속하기", btnStyle))
            {
                isPaused = false;
                Time.timeScale = 1f;
            }

            return;
        }

        // 게임오버 화면
        if (isDead)
        {
            // 짙은 붉은 보라 오버레이
            GUI.color = new Color(0.07f, 0f, 0.04f, 0.93f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            // 상하 혈색 라인
            GUI.color = new Color(0.72f, 0.04f, 0.1f, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, 5), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - 5, Screen.width, 5), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int wave  = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;
            int kills = WaveManager.Instance != null ? WaveManager.Instance.KillCount   : 0;

            // 달 장식
            GUIStyle deco = new GUIStyle
            {
                fontSize = 32,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.88f, 0.38f, 0.85f) }
            };
            GUI.Label(new Rect(0, Screen.height * 0.05f, Screen.width, 44), "◐  ✦  ◑", deco);

            // GAME OVER
            GUIStyle bigRed = new GUIStyle
            {
                fontSize = 62,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.92f, 0.07f, 0.07f) }
            };
            GUI.Label(new Rect(0, Screen.height * 0.13f, Screen.width, 78), "GAME OVER", bigRed);

            // 귀여운 부제목
            GUIStyle sub = new GUIStyle
            {
                fontSize = 19,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.88f, 0.62f, 0.72f) }
            };
            GUI.Label(new Rect(0, Screen.height * 0.24f, Screen.width, 28), "오늘 밤의 사냥은 여기까지...", sub);

            // 구분선
            float lw = Screen.width * 0.55f;
            GUI.color = new Color(0.65f, 0.08f, 0.12f, 0.5f);
            GUI.DrawTexture(new Rect((Screen.width - lw) * 0.5f, Screen.height * 0.305f, lw, 2), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 통계
            GUIStyle stat = new GUIStyle
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.88f, 0.88f) }
            };
            float sy = Screen.height * 0.33f;
            GUI.Label(new Rect(0, sy, Screen.width, 30),
                $"WAVE {wave} 생존   ✦   처치 {kills} 마리", stat);
            sy += 32f;

            if (earnedGoldOnDeath > 0)
            {
                GUIStyle goldEarned = new GUIStyle(stat)
                    { normal = { textColor = new Color(1f, 0.82f, 0.22f) } };
                GUI.Label(new Rect(0, sy, Screen.width, 28), $"◈ 금고 획득  {earnedGoldOnDeath} G", goldEarned);
                sy += 30f;
            }

            if (lostGoldOnDeath > 0)
            {
                GUIStyle goldLost = new GUIStyle(stat)
                    { normal = { textColor = new Color(1f, 0.45f, 0.25f) } };
                GUI.Label(new Rect(0, sy, Screen.width, 28), $"◈ 잃은 골드  {lostGoldOnDeath} G", goldLost);
                sy += 30f;
            }

            // 구분선
            GUI.color = new Color(0.65f, 0.08f, 0.12f, 0.35f);
            GUI.DrawTexture(new Rect((Screen.width - lw) * 0.5f, sy + 8f, lw, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            sy += 22f;

            // 획득한 스킬 목록
            if (SkillSystem.Instance != null && SkillSystem.Instance.acquired.Count > 0)
            {
                GUIStyle skillHeader = new GUIStyle
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.82f, 0.22f) }
                };
                GUIStyle skillEntry = new GUIStyle
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.86f, 0.80f, 0.88f) }
                };

                GUI.Label(new Rect(0, sy, Screen.width, 24), "✦  이번 런 획득 스킬  ✦", skillHeader);
                sy += 28f;
                foreach (var s in SkillSystem.Instance.acquired)
                {
                    GUI.Label(new Rect(0, sy, Screen.width, 22), $"◈ {s.skillName}  —  {s.description}", skillEntry);
                    sy += 22f;
                }
            }

            // 재시작 버튼
            float btnW = 220f, btnH = 56f;
            float bx = (Screen.width - btnW) * 0.5f;
            float by = Screen.height * 0.84f;

            GUI.color = new Color(0.65f, 0.05f, 0.08f, 0.85f);
            GUI.DrawTexture(new Rect(bx - 3, by - 3, btnW + 6, btnH + 6), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (GUI.Button(new Rect(bx, by, btnW, btnH), "마을로 돌아가기"))
                Restart();
        }
    }
}
