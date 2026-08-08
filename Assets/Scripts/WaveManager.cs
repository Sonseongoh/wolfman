using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public int baseEnemyCount = 6;

    [Tooltip("웨이브마다 적이 몇 마리씩 늘어날지")]
    public int enemyCountGrowth = 3;

    [Header("타이밍")]
    [Tooltip("웨이브 클리어 후 휴식 시간(초)")]
    public float timeBetweenWaves = 4f;

    [Tooltip("웨이브 내에서 적 하나하나가 나오는 간격(초)")]
    public float spawnInterval = 0.25f;

    public int CurrentWave { get; private set; }
    public int AliveCount { get; private set; }
    public int KillCount { get; private set; }

    [Header("달 슬롯 연출")]
    [Tooltip("달 이름이 슬롯머신처럼 돌아가는 시간(초)")]
    public float moonSpinDuration = 1.8f;

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

    Transform player;
    bool spawning;   // 이번 웨이브 스폰이 아직 진행 중인가
    bool resting;    // 웨이브 사이 휴식 중인가
    float restTimer;
    float moonBannerTimer;  // 달 확정 후 배너 표시 시간
    bool moonSpinning;      // 슬롯 연출 중인가
    string spinDisplayName; // 슬롯이 돌면서 보여주는 이름
    Sprite spinDisplayIcon; // 슬롯이 돌면서 보여주는 아이콘 (있을 때만)
    bool moonPromoting;     // 등급 승급 연출 중인가 (전설 전용)
    int promoTier;          // 승급 연출에서 현재 보여주는 등급 (0=Common)
    float promoStepStart;   // 현재 승급 단계가 시작된 시각 (펀치·플래시용)
    string promoDecoyName;  // 가짜 공개용 미끼 달 이름 (초승달/보름달)
    Sprite promoDecoyIcon;  // 미끼 달 아이콘

    bool waitingForAction;  // 첫 웨이브 달 공개 후 사냥/마을 선택 대기 (#6)
    public bool IsWaitingForAction => waitingForAction;
    /// <summary>달 연출(슬롯·승급·배너·행동선택) 중이거나 첫 웨이브 전인지 — 이 때는 일시정지 버튼 숨김</summary>
    public bool IsInMoonReveal => CurrentWave == 0 || moonSpinning || moonPromoting || moonBannerTimer > 0f || waitingForAction;

    EscapePortal activePortal;  // 이번 웨이브에 열린 탈출 포탈 (#46)
    float portalBannerTimer;    // "포탈이 열렸다" 안내 표시 시간
    float portalLostTimer;      // "멀어져서 닫혔다" 안내 표시 시간

    /// <summary>GameManager가 씬에 있으면 현재 달, 없으면 null (달 없이도 동작)</summary>
    MoonData CurrentMoon => GameManager.Instance != null ? GameManager.Instance.CurrentMoon : null;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        resting = true;
        restTimer = 2f; // 게임 시작 후 첫 웨이브까지 잠깐 여유
    }

    void Update()
    {
        if (moonBannerTimer > 0f)
        {
            moonBannerTimer -= Time.deltaTime;
            if (waitingForAction && moonBannerTimer < 0.01f) moonBannerTimer = 0.01f;
        }

        if (portalBannerTimer > 0f) portalBannerTimer -= Time.deltaTime;
        if (portalLostTimer > 0f) portalLostTimer -= Time.deltaTime;

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
    }

    void StartNextWave()
    {
        resting = false;
        CurrentWave++;

        // RoundController(#6)가 없을 때만 직접 라운드 시작 (임시 구조)
        // #6 완성 후엔 OnPhaseChanged(Hunt) 받아서 시작하는 걸로 교체 예정
        if (GameManager.Instance != null && GameManager.Instance.Phase != RoundPhase.Hunt)
            GameManager.Instance.StartNextRound();

        float countMult = CurrentMoon != null ? CurrentMoon.enemyCountMultiplier : 1f;
        int count = Mathf.Max(1, Mathf.RoundToInt(
            (baseEnemyCount + enemyCountGrowth * (CurrentWave - 1)) * countMult));

        TrySpawnPortal();
        StartCoroutine(MoonRevealThenSpawn(count));
    }

    /// <summary>달 슬롯머신 연출 → 확정 배너 → 스폰 시작</summary>
    IEnumerator MoonRevealThenSpawn(int count)
    {
        MoonTable table = GameManager.Instance != null ? GameManager.Instance.moonTable : null;

        if (CurrentWave == 1 && table != null && table.moons != null && table.moons.Length > 1 && CurrentMoon != null)
        {
            moonSpinning = true;
            float elapsed = 0f;
            float nextFlipAt = 0f;
            int idx = Random.Range(0, table.moons.Length);

            while (elapsed < moonSpinDuration)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= nextFlipAt)
                {
                    idx = (idx + 1) % table.moons.Length;
                    spinDisplayName = table.moons[idx].moonName;
                    spinDisplayIcon = table.moons[idx].icon;
                    // 처음엔 빠르게(0.05초), 끝으로 갈수록 느리게(0.3초) — 슬롯 감속
                    nextFlipAt = elapsed + Mathf.Lerp(0.05f, 0.3f, elapsed / moonSpinDuration);
                }
                yield return null;
            }
            moonSpinning = false;

            // 등급 승급 연출 — 슈퍼 블루 블러드문(전설) 전용 의식:
            // 일반 달(초승달/보름달)이 뜬 것처럼 가짜 공개 → 부들부들 → 땅! 땅! 땅! → 금색 대공개
            if (CurrentMoon != null && CurrentMoon.rarity == MoonRarity.Legendary)
            {
                int finalTier = (int)CurrentMoon.rarity;

                moonPromoting = true;

                // 1단계: Common 달로 진짜 공개인 척 2초 — 완전히 방심시킨다
                promoTier = 0;
                SetPromoDisplay(table, MoonRarity.Common);
                promoStepStart = Time.unscaledTime;
                yield return new WaitForSeconds(2.0f);

                // 이후: 등급이 오를 때마다 그 등급의 달로 변모하며 땅땅땅
                for (int tier = 1; tier < finalTier; tier++)
                {
                    promoTier = tier;
                    SetPromoDisplay(table, (MoonRarity)tier);
                    promoStepStart = Time.unscaledTime;
                    yield return new WaitForSeconds(0.4f);
                }
                moonPromoting = false;
            }

            moonBannerTimer = 1.6f; // 확정된 달 보여주기 (최종 공개가 마지막 땅!)
        }

        // 첫 웨이브에만 사냥/마을 선택 (#6) — 달 카드 뜨자마자 버튼 표시, 선택할 때까지 카드 유지
        if (CurrentWave == 1)
        {
            waitingForAction = true;
            yield return new WaitUntil(() => !waitingForAction);
            moonBannerTimer = 0f;
        }

        yield return StartCoroutine(SpawnWave(count));
    }

    IEnumerator SpawnWave(int count)
    {
        spawning = true;
        for (int i = 0; i < count; i++)
        {
            SpawnOne();
            yield return new WaitForSeconds(spawnInterval);
        }
        spawning = false;

        // 스폰이 끝나기 전에 플레이어가 전부 잡아버린 경우
        if (AliveCount <= 0) OnWaveCleared();
    }

    void SpawnOne()
    {
        if (player == null) return;

        GameObject prefab = PickEnemyPrefab();
        if (prefab == null) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
        GameObject go = Instantiate(prefab, (Vector2)player.position + offset, Quaternion.identity);
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

    /// <summary>승급 연출: 해당 등급의 달 중 하나를 골라 카드에 표시</summary>
    void SetPromoDisplay(MoonTable table, MoonRarity rarity)
    {
        var candidates = new List<MoonData>();
        foreach (MoonData m in table.moons)
            if (m != null && m.rarity == rarity) candidates.Add(m);

        if (candidates.Count > 0)
        {
            MoonData pick = candidates[Random.Range(0, candidates.Count)];
            promoDecoyName = pick.moonName;
            promoDecoyIcon = pick.icon;
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

    /// <summary>EnemyHealth가 사망 시 호출</summary>
    public void NotifyEnemyDied()
    {
        KillCount++;
        AliveCount = Mathf.Max(0, AliveCount - 1);
        if (AliveCount <= 0 && !spawning && !resting) OnWaveCleared();
    }

    /// <summary>웨이브 시작 시 확률 판정 — 성공하면 플레이어 주변에 탈출 포탈이 열린다 (#46)</summary>
    void TrySpawnPortal()
    {
        if (portalPrefab == null || player == null) return;
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

    void OnWaveCleared()
    {
        resting = true;
        restTimer = timeBetweenWaves;

        // 탈출 포탈은 그 웨이브 동안만 유지 — 클리어하면 닫힌다 (#46)
        if (activePortal != null)
        {
            activePortal.Close();
            activePortal = null;
        }

        // 스킬 3택 (#10) — 선택하는 동안 시간 정지, 휴식 타이머는 그 후 진행
        if (SkillSystem.Instance != null) SkillSystem.Instance.OfferChoices();

        // TODO(#7 머지 후): 다음 웨이브 규모·적 스탯에 MoonData 배율 적용
    }

    static Color RarityColor(MoonRarity r)
    {
        switch (r)
        {
            case MoonRarity.Uncommon: return new Color(0.4f, 1f, 0.4f);
            case MoonRarity.Rare: return new Color(0.4f, 0.7f, 1f);
            case MoonRarity.Epic: return new Color(0.8f, 0.4f, 1f);
            case MoonRarity.Legendary: return new Color(1f, 0.7f, 0.1f);
            default: return new Color(0.85f, 0.85f, 0.85f);
        }
    }

    // 임시 UI
    void OnGUI()
    {
        if (CurrentWave == 0) return;

        GUIStyle style = new GUIStyle
        {
            fontSize = 26,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = Color.white }
        };



        // 사냥/마을 선택 UI (첫 웨이브 달 공개 후)
        if (waitingForAction)
        {
            float btnW = 200f, btnH = 55f;
            float btnY = Screen.height * 0.82f;

            if (GUI.Button(new Rect(Screen.width * 0.5f - btnW - 20, btnY, btnW, btnH), "사냥 나가기"))
                waitingForAction = false;

            if (GUI.Button(new Rect(Screen.width * 0.5f + 20, btnY, btnW, btnH), "마을 남기"))
            {
                waitingForAction = false;
                if (GameManager.Instance != null) GameManager.Instance.SetPhase(RoundPhase.Village);
                SceneManager.LoadScene("VillageScene");
            }
        }

        if (!waitingForAction && !moonSpinning && !moonPromoting && moonBannerTimer <= 0f)
        {
            string text = resting
                ? $"WAVE {CurrentWave} 클리어!"
                : $"WAVE {CurrentWave}   남은 적: {AliveCount}";
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

        // 탈출 포탈 안내 (#46) — 열린 직후 3초는 큰 깜빡임, 이후엔 작은 상시 표시
        if (!waitingForAction && !moonSpinning && !moonPromoting)
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

        // 재화 HUD (#8) — 달 연출·행동 선택 중에는 화면을 비운다
        if (!moonSpinning && moonBannerTimer <= 0f && !waitingForAction)
            GoldPanelUI.Draw();

        // 현재 달 표시 — 우측 상단 골드 패널 아래에 아이콘 + 이름 (슬롯·연출 중엔 스포일러 방지로 숨김)
        MoonData moon = CurrentMoon;
        if (moon != null && !moonSpinning && !moonPromoting && moonBannerTimer <= 0f && !waitingForAction)
        {
            float mw = 190f, mh = 46f;
            float mx = Screen.width - mw - 16f, my = 92f; // 골드 패널(y12, 높이72) 바로 아래

            Color rc = RarityColor(moon.rarity);

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

        // 달 슬롯머신 연출: 카드 안에서 달이 돌아가고, 카드가 반짝인다
        if (moonSpinning && spinDisplayName != null)
        {
            float pulse = 0.55f + 0.15f * Mathf.Sin(Time.unscaledTime * 8f);
            Color spinBorder = new Color(pulse, pulse, pulse * 0.9f);
            DrawMoonCard("오늘 밤의 달은...", spinDisplayName, spinDisplayIcon, spinBorder, 1f, Vector2.zero);
        }

        // 등급 승급 연출: 미끼 달이 공개된 척하다가 떨리며 단계별로 땅! 땅! 승급
        if (moonPromoting)
        {
            float ts = Time.unscaledTime - promoStepStart;
            Color pc = RarityColor((MoonRarity)promoTier);

            // 승급 순간 작은 플래시
            if (ts < 0.12f)
            {
                GUI.color = new Color(pc.r, pc.g, pc.b, 0.15f * (1f - ts / 0.12f));
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // 승급 순간 카드 펀치. 미끼 단계(0)는 떨지 않고, 승급이 시작되면 점점 격해짐
            float punch = 1f + 0.18f * Mathf.Pow(1f - Mathf.Clamp01(ts / 0.15f), 2f);
            float amp = promoTier == 0 ? 0f : 2f + promoTier * 3f;
            Vector2 shake = new Vector2(
                Mathf.Sin(Time.unscaledTime * 67f),
                Mathf.Cos(Time.unscaledTime * 53f) * 0.6f) * amp;

            DrawMoonCard($"{promoDecoyName}이 떠올랐다!", promoDecoyName, promoDecoyIcon, pc, punch, shake);
        }

        // 확정된 달 카드 — 플래시 → 카드 쿵 착지 → 광선 회전 + 카드 반짝임
        if (moon != null && moonBannerTimer > 0f && !moonSpinning && !moonPromoting)
        {
            const float bannerDuration = 1.6f;
            float t = bannerDuration - moonBannerTimer; // 공개 후 경과 시간
            Color rc = RarityColor(moon.rarity);

            // 1) 공개 순간 등급색 화면 플래시 (0.35초간 사라짐)
            if (t < 0.35f)
            {
                GUI.color = new Color(rc.r, rc.g, rc.b, 0.4f * (1f - t / 0.35f));
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // 2) 카드 뒤에서 천천히 도는 등급색 광선 8줄기
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.42f);
            float rayLen = Screen.height * 0.34f;
            Matrix4x4 saved = GUI.matrix;
            for (int i = 0; i < 8; i++)
            {
                GUI.matrix = saved;
                GUIUtility.RotateAroundPivot(i * 45f + Time.unscaledTime * 25f, center);
                GUI.color = new Color(rc.r, rc.g, rc.b, 0.1f);
                GUI.DrawTexture(
                    new Rect(center.x - rayLen * 0.035f, center.y - rayLen, rayLen * 0.07f, rayLen),
                    Texture2D.whiteTexture);
            }
            GUI.matrix = saved;
            GUI.color = Color.white;

            // 3) 카드 쿵 착지 (1.35배 → 제자리) + 등급색 테두리
            float punch = 1f + 0.35f * Mathf.Pow(1f - Mathf.Clamp01(t / 0.25f), 2f);
            DrawMoonCard($"{moon.moonName}이 떠올랐다!", moon.moonName, moon.icon, rc, punch, Vector2.zero);
        }
    }

    /// <summary>
    /// 달 카드 그리기: 테두리 + 어두운 카드 안에 아이콘·이름, 표면을 스치는 반짝임(샤인).
    /// scale은 등장 펀치용 (1 = 기본 크기).
    /// </summary>
    static void DrawMoonCard(string title, string moonName, Sprite icon, Color borderColor, float scale, Vector2 shakeOffset)
    {
        float cardW = Mathf.Min(360f, Screen.width * 0.34f) * scale;
        float cardH = cardW * 1.3f;
        Rect card = new Rect(
            (Screen.width - cardW) * 0.5f + shakeOffset.x,
            Screen.height * 0.42f - cardH * 0.5f + shakeOffset.y,
            cardW, cardH);

        // 테두리
        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(card.x - 4, card.y - 4, card.width + 8, card.height + 8), Texture2D.whiteTexture);

        // 카드 배경
        GUI.color = new Color(0.1f, 0.09f, 0.15f);
        GUI.DrawTexture(card, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 제목 (카드 위 바깥)
        GUIStyle titleStyle = new GUIStyle
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = borderColor }
        };
        GUI.Label(new Rect(0, card.y - 44, Screen.width, 34), title, titleStyle);

        // 아이콘 (카드 안 상단)
        if (icon != null)
        {
            float isz = cardW * 0.62f;
            GUI.DrawTexture(
                new Rect(card.x + (cardW - isz) * 0.5f, card.y + cardH * 0.12f, isz, isz),
                icon.texture, ScaleMode.ScaleToFit, true);
        }

        // 달 이름 (카드 안 하단)
        GUIStyle nameStyle = new GUIStyle
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
        GUI.Label(new Rect(card.x + 8, card.y + cardH * 0.72f, cardW - 16, cardH * 0.24f), moonName, nameStyle);

        // 반짝임: 카드 표면을 왼쪽→오른쪽으로 스치는 빛 밴드 (카드 영역에만 그려짐)
        GUI.BeginGroup(card);
        float sweep = (Time.unscaledTime * cardW * 0.9f) % (cardW * 1.8f) - cardW * 0.4f;
        GUI.color = new Color(1f, 1f, 1f, 0.10f);
        GUI.DrawTexture(new Rect(sweep, 0, cardW * 0.22f, cardH), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, 0.18f);
        GUI.DrawTexture(new Rect(sweep + cardW * 0.07f, 0, cardW * 0.07f, cardH), Texture2D.whiteTexture);
        GUI.EndGroup();
        GUI.color = Color.white;
    }
}
