using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 사냥 모드의 웨이브 진행 관리 (#9).
/// 웨이브 시작 → 적 스폰 → 전멸 시 클리어 → 휴식 → 다음 웨이브(규모 증가).
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [System.Serializable]
    public class EnemyOption
    {
        public GameObject prefab;

        [Tooltip("이 웨이브부터 등장")]
        public int minWave = 1;

        [Tooltip("등장 비중 (높을수록 자주 나옴)")]
        public float weight = 1f;
    }

    [Header("적 구성 (타입별 등장 웨이브·비중)")]
    public List<EnemyOption> enemyTypes = new List<EnemyOption>();

    [Tooltip("플레이어로부터 이 거리(화면 밖)에서 스폰 — 카메라 시야(ortho size 8, 화면 모서리까지 약 16.3)보다 커야 함")]
    public float spawnRadius = 18f;

    [Header("웨이브 규모")]
    [Tooltip("1웨이브의 적 수")]
    public int baseEnemyCount = 10;

    [Tooltip("웨이브마다 적이 몇 마리씩 늘어날지")]
    public int enemyCountGrowth = 3;

    [Header("타이밍")]
    [Tooltip("웨이브 길이(초) — 이 시간 동안 적이 꾸준히 흘러들어오고, 끝나면 다음 단계로 (뱀서식 연속 유입)")]
    public float waveDuration = 15f;

    [Tooltip("웨이브 사이 휴식 시간(초)")]
    public float timeBetweenWaves = 4f;

    public int CurrentWave { get; private set; }
    public int AliveCount { get; private set; }
    public int KillCount { get; private set; }

    [Header("탈출 포탈 (#46)")]
    [Tooltip("탈출 포탈 프리팹")]
    public GameObject portalPrefab;

    [Tooltip("웨이브 시작 시 포탈이 열릴 확률 (0.1 = 10%). 달별 조정은 회의 후 MoonData로 이관 예정")]
    [Range(0f, 1f)] public float portalChance = 0.1f;

    [Tooltip("이 웨이브부터 포탈 판정 — 초반엔 못 나가고, 버텨야 탈출 기회가 열린다")]
    public int portalMinWave = 5;

    [Tooltip("포탈이 열리는 최소 거리 (플레이어 기준 — 항상 화면 밖에서 뜬다)")]
    public float portalDistanceMin = 20f;

    [Tooltip("포탈이 열리는 최대 거리 (플레이어 기준). 화살표 보고 적을 뚫으며 찾아가야 한다")]
    public float portalDistanceMax = 60f;

    [Tooltip("포탈에서 이 거리 이상 멀어지면 포탈이 닫혀버린다 (무한 맵 대비 최대 범위). 반드시 포탈 최대 거리보다 커야 함 — 아니면 뜨자마자 닫힌다")]
    public float portalMaxRange = 80f;

    [Header("중간보스 (#122)")]
    [Tooltip("중간보스 프리팹 (무리의 우두머리)")]
    public GameObject bossPrefab;

    [Tooltip("몇 웨이브마다 보스가 나올지")]
    public int bossEveryWaves = 5;

    Transform player;
    PlayerHealth playerHealth; // 먹은 만큼 회복시켜 줄 대상 (#117)
    bool spawning;    // 이번 웨이브 스폰이 아직 진행 중인가
    bool resting;     // 웨이브 사이 휴식 중인가
    float restTimer;
    float waveTimer;  // 이번 웨이브의 남은 시간

    /// <summary>첫 웨이브 전인지 — 이 때는 일시정지 버튼 숨김.
    /// 달 공개·행동 선택은 마을(MoonRevealUI, #103)로 옮겨져 사냥 씬엔 연출이 없다</summary>
    public bool IsInMoonReveal => CurrentWave == 0;

    EscapePortal activePortal;  // 이번 웨이브에 열린 탈출 포탈 (#46)
    float portalBannerTimer;    // "포탈이 열렸다" 안내 표시 시간
    float portalLostTimer;      // "멀어져서 닫혔다" 안내 표시 시간
    float bossBannerTimer;      // "우두머리가 나타났다" 안내 표시 시간 (#122)

    /// <summary>GameManager가 씬에 있으면 현재 달, 없으면 null (달 없이도 동작)</summary>
    MoonData CurrentMoon => GameManager.Instance != null ? GameManager.Instance.CurrentMoon : null;

    void Awake()
    {
        Instance = this;

        // 사냥터 지형 관리 자동 장착 (#98 후처리 — 바닥 무한 채움 + 구조물 솎아내기)
        if (GetComponent<HuntTerrain>() == null) gameObject.AddComponent<HuntTerrain>();
    }

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }

        resting = true;
        restTimer = 2f; // 게임 시작 후 첫 웨이브까지 잠깐 여유

        SoundManager.Instance?.PlayBGM(SoundManager.Instance.bgmBattle);
    }

    void Update()
    {
        // 야성은 사냥하는 동안 그 밤의 달이 정한 속도로 찬다 (#117).
        // 축은 스스로 돌지 않는 셸이라 그 씬의 흐름 소유자가 굴린다 — 사냥에서는 여기다.
        // Time.deltaTime 이라 일시정지·스킬 3택(timeScale 0)에서는 자동으로 멈춘다.
        WildAxisManager.Instance.Advance(Time.deltaTime);

        if (portalBannerTimer > 0f) portalBannerTimer -= Time.deltaTime;
        if (portalLostTimer > 0f) portalLostTimer -= Time.deltaTime;
        if (bossBannerTimer > 0f) bossBannerTimer -= Time.deltaTime;

        // 포탈에서 너무 멀어지면 닫혀버린다 (#46) — 무한 맵에서 밑도 끝도 없이 멀어지는 것 방지
        if (activePortal != null && player != null &&
            Vector2.Distance(player.position, activePortal.transform.position) > portalMaxRange)
        {
            activePortal.Close();
            activePortal = null;
            portalBannerTimer = 0f;
            portalLostTimer = 2.5f;
        }

        if (resting)
        {
            restTimer -= Time.deltaTime;
            if (restTimer <= 0f) StartNextWave();
        }
        else if (waveTimer > 0f)
        {
            // 웨이브는 시간으로 밀려온다 — 다 못 잡아도 다음 무리가 겹쳐서 온다
            waveTimer -= Time.deltaTime;
            if (waveTimer <= 0f) OnWaveEnd();
        }
    }

    void StartNextWave()
    {
        resting = false;
        waveTimer = waveDuration;
        CurrentWave++;

        // 라운드 시작의 주인은 흐름 제어(마을·정산 허브)다. 여기서 시작하는 건 사냥 씬만
        // 단독 재생하는 개발 상황 하나뿐 — 시작해 줄 사람이 아무도 없을 때다. (#78)
        // 달 공개·행동 선택은 마을(MoonRevealUI, #103)에서 끝내고 오므로 여긴 연출 없이 스폰만
        if (GameManager.Instance != null && RoundFlowRule.NeedsRoundStart(GameManager.Instance.RoundNumber))
        {
            GameManager.Instance.StartNextRound();
            GameManager.Instance.SetPhase(RoundPhase.Hunt); // 단독 재생도 사냥 라운드로 취급
        }

        float countMult = CurrentMoon != null ? CurrentMoon.enemyCountMultiplier : 1f;
        int count = Mathf.Max(1, Mathf.RoundToInt(
            (baseEnemyCount + enemyCountGrowth * (CurrentWave - 1)) * countMult));

        TrySpawnPortal();
        TrySpawnBoss();
        StartCoroutine(SpawnWave(count));
    }

    /// <summary>보스 웨이브(5, 10, 15...)면 중간보스 소환 (#122).
    /// 이전 보스가 살아있어도 겹쳐 나온다 — 빨리 못 잡은 대가는 유저의 몫</summary>
    void TrySpawnBoss()
    {
        if (bossPrefab == null || player == null || bossEveryWaves <= 0) return;
        if (CurrentWave % bossEveryWaves != 0) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 pos = (Vector2)player.position
            + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;

        GameObject go = Instantiate(bossPrefab, pos, Quaternion.identity);
        AliveCount++;

        // 체력은 웨이브에 비례해 자란다 (기본 100 × (1 + 웨이브×0.1))
        EnemyHealth hp = go.GetComponent<EnemyHealth>();
        if (hp != null) hp.ApplyHpMultiplier(1f + CurrentWave * 0.1f);

        bossBannerTimer = 3f;
        SoundManager.Instance?.PlayMoonReveal(false); // 등장 효과음 (임시 — 전용 사운드는 추후)
    }

    IEnumerator SpawnWave(int count)
    {
        // 무리 단위 습격: 2~4마리가 같은 방향에서 한꺼번에 밀려온다.
        // 웨이브 정원을 무리 수로 나눠 시간 전체에 분산 — 총량은 같지만 "팍팍" 온다
        spawning = true;
        const float avgPack = 3f;
        float interval = waveDuration / Mathf.Max(1f, count / avgPack);

        int remaining = count;
        while (remaining > 0)
        {
            int pack = Mathf.Min(remaining, Random.Range(2, 5));
            float packAngle = Random.Range(0f, Mathf.PI * 2f); // 이 무리가 몰려오는 방향

            for (int i = 0; i < pack; i++)
                SpawnOne(packAngle + Random.Range(-0.35f, 0.35f)); // 같은 방향에서 살짝 흩어져

            remaining -= pack;
            yield return new WaitForSeconds(interval);
        }
        spawning = false;
    }

    void SpawnOne(float angle)
    {
        if (player == null) return;

        GameObject prefab = PickEnemyPrefab();
        if (prefab == null) return;

        // 장애물 위에 스폰되면 갇혀버리므로, 무리 방향 근처에서 자리를 몇 번 다시 뽑는다
        Vector2 pos = (Vector2)player.position
            + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            float a = angle + Random.Range(-0.5f, 0.5f) * attempt; // 재시도할수록 조금씩 옆으로
            pos = (Vector2)player.position
                + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (spawnRadius + Random.Range(0f, 1.5f));

            bool blocked = false;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, 0.6f))
                if (c is TilemapCollider2D || c is CompositeCollider2D
                    || c.GetComponent<ObstacleProp>() != null) { blocked = true; break; }

            if (!blocked) break;
        }

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        AliveCount++;

        // 달 배율 적용 (#7) — 데미지 배율·변신 제한·특화 효과는 TODO
        MoonData moon = CurrentMoon;
        if (moon != null)
        {
            EnemyChase chase = go.GetComponent<EnemyChase>();
            if (chase != null) chase.moveSpeed *= moon.enemySpeedMultiplier;

            EnemyHealth hp = go.GetComponent<EnemyHealth>();
            if (hp != null) hp.ApplyHpMultiplier(moon.enemyHpMultiplier);
        }
    }

    /// <summary>현재 웨이브에 등장 가능한 타입 중 가중치 추첨</summary>
    GameObject PickEnemyPrefab()
    {
        float total = 0f;
        foreach (EnemyOption e in enemyTypes)
            if (e.prefab != null && CurrentWave >= e.minWave) total += e.weight;

        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        foreach (EnemyOption e in enemyTypes)
        {
            if (e.prefab == null || CurrentWave < e.minWave) continue;
            roll -= e.weight;
            if (roll <= 0f) return e.prefab;
        }
        return null;
    }

    /// <summary>EnemyHealth가 사망 시 호출. 웨이브는 시간으로만 넘어간다 — 전멸해도 조기 종료 없음</summary>
    public void NotifyEnemyDied()
    {
        KillCount++;
        AliveCount = Mathf.Max(0, AliveCount - 1);

        // 처치 = 먹기 (#117) — 굶주림이 내려가고, 굶주릴수록 한 입이 크다.
        // 처치가 지나는 깔때기가 여기 하나뿐이라 미니보스도 같은 길로 들어온다.
        int bite = WildAxisManager.Instance.NotifyKill();
        if (bite > 0 && playerHealth != null) playerHealth.Heal(bite);
    }

    /// <summary>웨이브 시작 시 확률 판정 — 성공하면 플레이어 주변에 탈출 포탈이 열린다 (#46).
    /// 포탈 수명은 EscapePortal이 스스로 관리 (기본 20초, 웨이브와 무관)</summary>
    void TrySpawnPortal()
    {
        if (portalPrefab == null || player == null) return;
        if (activePortal != null) return; // 이미 열려 있으면 중복 생성 안 함
        if (CurrentWave < portalMinWave) return;
        if (Random.value >= portalChance) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(portalDistanceMin, portalDistanceMax);
        Vector2 pos = (Vector2)player.position
            + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

        GameObject go = Instantiate(portalPrefab, pos, Quaternion.identity);
        activePortal = go.GetComponent<EscapePortal>();
        portalBannerTimer = 3f;
    }

    /// <summary>웨이브 종료 (시간 만료) — 남은 적은 다음 웨이브로 그대로 이어진다</summary>
    void OnWaveEnd()
    {
        resting = true;
        restTimer = timeBetweenWaves;
        waveTimer = 0f;
        SoundManager.Instance?.PlayWaveClear();

        // 포탈은 웨이브가 끝나도 닫지 않는다 — 수명(20초)은 EscapePortal이 스스로 관리

        // 스킬 3택 (#10) — 선택하는 동안 시간 정지, 휴식 타이머는 그 후 진행
        if (SkillSystem.Instance != null) SkillSystem.Instance.OfferChoices();

        // TODO(#7 머지 후): 다음 웨이브 규모·적 스탯에 MoonData 배율 적용
    }

    // 임시 UI — 달 공개·행동 선택은 마을(MoonRevealUI #103)로 이동, 여긴 전투 HUD만
    void OnGUI()
    {
        if (CurrentWave == 0) return;

        GUIStyle style = new GUIStyle
        {
            fontSize = 26,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = Color.white }
        };

        {
            string text = resting
                ? $"WAVE {CurrentWave} 종료 — 다음 무리가 몰려온다!"
                : $"WAVE {CurrentWave}   ⏱ {Mathf.CeilToInt(waveTimer)}초   처치 {KillCount}";
            GUI.Label(new Rect(0, 16, Screen.width, 40), text, style);

            // 웨이브 사이 카운트다운 — 화면 가운데 큰 숫자 3, 2, 1이 천천히 가라앉으며 사라진다
            if (resting && restTimer > 0f && restTimer <= 3f)
            {
                int sec = Mathf.CeilToInt(restTimer);
                float frac = restTimer - (sec - 1); // 이 숫자의 남은 비율: 1 → 0

                float alpha = 0.2f + 0.8f * Mathf.SmoothStep(0f, 1f, frac); // 서서히 흐려짐

                GUIStyle countStyle = new GUIStyle
                {
                    fontSize = 170,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 1f, 1f, alpha) }
                };
                GUI.Label(new Rect(0, Screen.height * 0.5f - 110, Screen.width, 220),
                    sec.ToString(), countStyle);
            }
        }

        // 중간보스 등장 안내 (#122)
        if (bossBannerTimer > 0f)
        {
            float blink = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 7f);
            GUIStyle bossStyle = new GUIStyle
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(1f, 0.32f, 0.28f, blink) }
            };
            GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 40),
                "우두머리가 나타났다!", bossStyle);
        }

        // 탈출 포탈 안내 (#46) — 열린 직후 3초는 큰 깜빡임, 이후엔 작은 상시 표시
        {
            if (portalBannerTimer > 0f)
            {
                float blink = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f);
                GUIStyle portalStyle = new GUIStyle
                {
                    fontSize = 26,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = new Color(0.8f, 0.6f, 1f, blink) }
                };
                GUI.Label(new Rect(0, 84, Screen.width, 36),
                    "탈출 포탈이 열렸다!  이번 웨이브 동안만 유지된다", portalStyle);
            }
            else if (activePortal != null)
            {
                GUIStyle portalSmall = new GUIStyle
                {
                    fontSize = 18,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = new Color(0.8f, 0.6f, 1f, 0.85f) }
                };
                GUI.Label(new Rect(0, 84, Screen.width, 30), "◈ 탈출 포탈 열림", portalSmall);
            }

            // 포탈이 화면 밖에 있으면 가장자리에 방향 화살표 + 거리 표시 (#46)
            // — 무한 맵이라 랜드마크가 없어서, 이게 없으면 포탈을 놓치면 못 찾는다
            if (activePortal != null && Camera.main != null && player != null)
            {
                Vector3 vp = Camera.main.WorldToViewportPoint(activePortal.transform.position);
                bool offscreen = vp.x < 0.02f || vp.x > 0.98f || vp.y < 0.02f || vp.y > 0.98f;

                if (offscreen)
                {
                    // 뷰포트(아래가 0) → GUI 화면 좌표(위가 0)로 변환하며 가장자리에 고정
                    Vector2 sp = new Vector2(
                        Mathf.Clamp(vp.x, 0.05f, 0.95f) * Screen.width,
                        (1f - Mathf.Clamp(vp.y, 0.08f, 0.92f)) * Screen.height);

                    Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    float ang = Mathf.Atan2(sp.y - center.y, sp.x - center.x) * Mathf.Rad2Deg;

                    // 한계 거리(portalMaxRange)에 가까워질수록 보라 → 빨강으로 경고
                    float dist = Vector2.Distance(player.position, activePortal.transform.position);
                    float danger = Mathf.InverseLerp(portalMaxRange * 0.65f, portalMaxRange, dist);
                    Color portalPurple = new Color(0.8f, 0.6f, 1f);
                    Color arrowColor = Color.Lerp(portalPurple, new Color(1f, 0.3f, 0.25f), danger);
                    if (danger > 0.5f) // 한계 직전엔 깜빡임까지
                        arrowColor.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 10f);

                    GUIStyle arrowStyle = new GUIStyle
                    {
                        fontSize = 30,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = arrowColor }
                    };

                    Matrix4x4 saved = GUI.matrix;
                    GUIUtility.RotateAroundPivot(ang, sp);
                    GUI.Label(new Rect(sp.x - 20, sp.y - 20, 40, 40), "➤", arrowStyle);
                    GUI.matrix = saved;

                    // 거리 숫자는 화살표보다 화면 중앙 쪽에 (회전 없이)
                    Vector2 inward = (center - sp).normalized * 36f;
                    GUIStyle distStyle = new GUIStyle
                    {
                        fontSize = 15,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = arrowColor }
                    };
                    GUI.Label(new Rect(sp.x + inward.x - 30, sp.y + inward.y - 12, 60, 24),
                        $"{dist:0}m", distStyle);
                }
            }

            // 너무 멀어져서 포탈이 닫혔을 때 안내 (#46)
            if (portalLostTimer > 0f)
            {
                GUIStyle lostStyle = new GUIStyle
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = new Color(1f, 0.4f, 0.35f, Mathf.Clamp01(portalLostTimer)) }
                };
                GUI.Label(new Rect(0, 84, Screen.width, 34),
                    "너무 멀어져서 탈출 포탈이 닫혀버렸다...", lostStyle);
            }
        }

        // 재화 HUD (#8)
        GoldPanelUI.Draw();

        // 야성·굶주림 축 게이지 (#117)
        WildAxisGaugeUI.Draw();

        // 현재 달 표시 — 우측 상단 골드 패널 아래에 아이콘 + 이름
        MoonData moon = CurrentMoon;
        if (moon != null && !resting)
        {
            float mw = 190f, mh = 46f;
            float mx = Screen.width - mw - 16f, my = 92f; // 골드 패널(y12, 높이72) 바로 아래

            Color rc = MoonRevealUI.RarityColor(moon.rarity);

            // 등급색 테두리 + 어두운 배경 (골드 패널과 같은 스타일)
            GUI.color = new Color(rc.r, rc.g, rc.b, 0.75f);
            GUI.DrawTexture(new Rect(mx - 2, my - 2, mw + 4, mh + 4), Texture2D.whiteTexture);
            GUI.color = new Color(0.06f, 0.05f, 0.12f, 0.9f);
            GUI.DrawTexture(new Rect(mx, my, mw, mh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 달 아이콘 (작게)
            float isz = 34f;
            if (moon.icon != null)
                GUI.DrawTexture(new Rect(mx + 8, my + (mh - isz) * 0.5f, isz, isz),
                    moon.icon.texture, ScaleMode.ScaleToFit, true);

            // 달 이름 (등급색)
            GUIStyle moonStyle = new GUIStyle
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                normal = { textColor = rc }
            };
            GUI.Label(new Rect(mx + 8 + isz + 8, my + 2, mw - isz - 26, mh - 4),
                moon.moonName, moonStyle);
        }

    }
}
