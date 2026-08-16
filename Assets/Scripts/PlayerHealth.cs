using UnityEngine;
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
    int lostGoldOnDeath;
    int earnedGoldOnDeath;
    SpriteRenderer sr;

    /// <summary>게임오버 상태인지 (HitStop 등이 시간 정지 유지 판단에 사용)</summary>
    public bool IsDead => isDead;

    /// <summary>
    /// 지금 체력. 읽기 전용이다 — 깎고 채우는 길은 <see cref="TakeEnemyHit"/> 와 <see cref="Heal"/> 뿐이어야
    /// 무적 시간·사망 판정·굶주림 정산을 건너뛰는 경로가 생기지 않는다.
    ///
    /// #117 을 검증할 때 이게 없어서 리플렉션으로 사설 필드를 읽어야 했다. 폭주(#12)도
    /// "지금 얼마나 남았나"를 물을 것이다.
    /// </summary>
    public int CurrentHp => hp;

    /// <summary>
    /// 굶주림 페널티까지 반영한 지금의 최대 체력 (#117).
    ///
    /// <c>maxHp</c> 자체는 건드리지 않는다 — 스킬 <c>IncreaseMaxHp</c> 가 같은 필드를 올리고 있어서
    /// 거기서 빼면 굶주림이 풀렸을 때 무엇을 얼마나 돌려줘야 하는지 알 수 없게 된다.
    /// 뺄셈을 여기서 하면 단계가 풀리는 순간 페널티도 저절로 걷힌다.
    /// </summary>
    public int EffectiveMaxHp => Mathf.Max(1, maxHp - WildAxisManager.Instance.MaxHpPenalty);

    /// <summary>굶주림이 눌러둔 체력 (#117). 페널티가 걷히면 이만큼 돌려준다 — 맡아둔 것이지 잃은 게 아니다.</summary>
    int hungerHeld;

    void Awake()
    {
        hp = maxHp;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 굶주림이 최대치를 누르면 현재 체력도 따라 내려가고, 풀리면 눌렸던 만큼 돌아온다 (#117).
        // 안 깎으면 페널티가 다음 피격까지 체감되지 않고, 안 돌려주면 단계가 오르내릴 때마다
        // 맞지도 않은 체력이 계단식으로 사라진다. 판정은 HungerHealthRule 이 한다.
        //
        // 죽은 뒤에는 정산하지 않는다. Update 는 isDead 와 무관하게 계속 돌기 때문에,
        // 굶주려 죽으면 눌러둔 체력이 그대로 돌아와 게임오버 화면에 "HP: 2 / 3" 이 뜬다.
        if (!isDead)
        {
            HungerHealthRule.Settled settled = HungerHealthRule.Settle(hp, hungerHeld, EffectiveMaxHp);
            hp = settled.Hp;
            hungerHeld = settled.Held;
        }

        // 무적 시간 동안 깜빡여서 시각적으로 표시
        if (invincibleTimer > 0f)
        {
            invincibleTimer -= Time.deltaTime;
            if (sr != null)
                sr.enabled = Mathf.FloorToInt(invincibleTimer * 10f) % 2 == 0;

            if (invincibleTimer <= 0f && sr != null) sr.enabled = true;
        }
        // 일시정지(ESC·⏸)는 전역 PauseSystem이 모든 씬에서 처리
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
        int missing = EffectiveMaxHp - hp;
        if (missing > 0) Heal(missing);
    }

    /// <summary>회복 오브 등으로 체력 회복 (#32) — 최대치를 넘지 않음</summary>
    public void Heal(int amount)
    {
        if (isDead) return;

        hp = Mathf.Min(EffectiveMaxHp, hp + amount);
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
            SoundManager.Instance?.PlayGameOver();
            Time.timeScale = 0f;
        }
        else
        {
            SoundManager.Instance?.PlayDamaged();
        }
    }

    /// <summary>
    /// 죽음 = 밤 소모 (#113): 그 밤의 임시 골드·스킬만 잃고 다음 밤의 마을로 돌아간다.
    /// 금고 골드는 남는다. 정산 허브를 거치지 않으므로 클리어 보너스는 없다 —
    /// 죽은 밤에 보너스가 나오면 안 되기 때문이다.
    /// </summary>
    static void ReturnToVillage()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNextRound(); // 밤은 소모된다 — 같은 밤 무한 재시도 불가
            GameManager.Instance.SetPhase(RoundPhase.Village);
        }
        SceneManager.LoadScene("VillageScene");
    }

    // 임시 UI (나중에 제대로 된 UI로 교체 예정)
    void OnGUI()
    {
        GUIStyle hpStyle = new GUIStyle { fontSize = 28, normal = { textColor = Color.white } };
        GUI.Label(new Rect(20, 20, 300, 40), $"HP: {hp} / {EffectiveMaxHp}", hpStyle);

        // 일시정지 버튼·오버레이는 전역 PauseSystem(#33)이 모든 씬에서 그린다

        // 게임오버 화면
        if (isDead)
        {
            // 거의 검정에 가까운 짙은 오버레이
            GUI.color = new Color(0.04f, 0.03f, 0.06f, 0.93f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            // 상하 라인 (붉은기 줄임)
            GUI.color = new Color(0.45f, 0.04f, 0.08f, 0.6f);
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
                ReturnToVillage();
        }
    }
}
