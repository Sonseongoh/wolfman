using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Tooltip("플레이어로부터 이 거리(화면 밖)에서 스폰")]
    public float spawnRadius = 12f;

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

    [Header("달 슬롯 연출")]
    [Tooltip("달 이름이 슬롯머신처럼 돌아가는 시간(초)")]
    public float moonSpinDuration = 1.8f;

    Transform player;
    bool spawning;   // 이번 웨이브 스폰이 아직 진행 중인가
    bool resting;    // 웨이브 사이 휴식 중인가
    float restTimer;
    float moonBannerTimer;  // 달 확정 후 배너 표시 시간
    bool moonSpinning;      // 슬롯 연출 중인가
    string spinDisplayName; // 슬롯이 돌면서 보여주는 이름
    Sprite spinDisplayIcon; // 슬롯이 돌면서 보여주는 아이콘 (있을 때만)

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
        if (moonBannerTimer > 0f) moonBannerTimer -= Time.deltaTime;

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

        // 웨이브 시작 = 라운드 시작: 달 추첨 (#7)
        if (GameManager.Instance != null)
            GameManager.Instance.StartNextRound();

        float countMult = CurrentMoon != null ? CurrentMoon.enemyCountMultiplier : 1f;
        int count = Mathf.Max(1, Mathf.RoundToInt(
            (baseEnemyCount + enemyCountGrowth * (CurrentWave - 1)) * countMult));

        StartCoroutine(MoonRevealThenSpawn(count));
    }

    /// <summary>달 슬롯머신 연출 → 확정 배너 → 스폰 시작</summary>
    IEnumerator MoonRevealThenSpawn(int count)
    {
        MoonTable table = GameManager.Instance != null ? GameManager.Instance.moonTable : null;

        if (table != null && table.moons != null && table.moons.Length > 1 && CurrentMoon != null)
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
            moonBannerTimer = 1.6f; // 확정된 달 보여주기
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
        AliveCount = Mathf.Max(0, AliveCount - 1);
        if (AliveCount <= 0 && !spawning && !resting) OnWaveCleared();
    }

    void OnWaveCleared()
    {
        resting = true;
        restTimer = timeBetweenWaves;

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

        string text = resting
            ? $"WAVE {CurrentWave} 클리어!  다음 웨이브까지 {Mathf.CeilToInt(restTimer)}초"
            : $"WAVE {CurrentWave}   남은 적: {AliveCount}";

        GUI.Label(new Rect(0, 16, Screen.width, 40), text, style);

        // 현재 달 표시 (웨이브 텍스트 아래) — 슬롯 도는 동안엔 스포일러 방지로 숨김
        MoonData moon = CurrentMoon;
        if (moon != null && !resting && !moonSpinning)
        {
            GUIStyle moonStyle = new GUIStyle
            {
                fontSize = 20,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = RarityColor(moon.rarity) }
            };
            GUI.Label(new Rect(0, 48, Screen.width, 30), $"{moon.moonName}  [{moon.rarity}]", moonStyle);
        }

        // 달 슬롯머신 연출: 이름(+아이콘)이 빠르게 돌다가 감속
        if (moonSpinning && spinDisplayName != null)
        {
            GUIStyle spinTitle = new GUIStyle
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            GUI.Label(new Rect(0, Screen.height * 0.16f, Screen.width, 30), "오늘 밤의 달은...", spinTitle);

            DrawMoonIcon(spinDisplayIcon, Screen.height * 0.21f);

            GUIStyle spin = new GUIStyle
            {
                fontSize = 44,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(0, Screen.height * 0.33f, Screen.width, 60), spinDisplayName, spin);
        }

        // 확정된 달 배너
        if (moon != null && moonBannerTimer > 0f && !moonSpinning)
        {
            DrawMoonIcon(moon.icon, Screen.height * 0.21f);

            GUIStyle banner = new GUIStyle
            {
                fontSize = 48,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = RarityColor(moon.rarity) }
            };
            GUI.Label(new Rect(0, Screen.height * 0.33f, Screen.width, 60),
                $"{moon.moonName}이 떠올랐다!", banner);
        }
    }

    /// <summary>달 아이콘이 연결돼 있으면 화면 가운데에 그린다 (없으면 아무것도 안 함)</summary>
    static void DrawMoonIcon(Sprite icon, float y)
    {
        if (icon == null) return;

        float size = Mathf.Min(96f, Screen.height * 0.12f);
        GUI.DrawTexture(
            new Rect((Screen.width - size) * 0.5f, y, size, size),
            icon.texture, ScaleMode.ScaleToFit, true);
    }
}
