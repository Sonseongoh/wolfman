using System.Collections;
using UnityEngine;

/// <summary>
/// 사냥 모드의 웨이브 진행 관리 (#9).
/// 웨이브 시작 → 적 스폰 → 전멸 시 클리어 → 휴식 → 다음 웨이브(규모 증가).
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("적 구성")]
    public GameObject enemyPrefab;

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

    Transform player;
    bool spawning;   // 이번 웨이브 스폰이 아직 진행 중인가
    bool resting;    // 웨이브 사이 휴식 중인가
    float restTimer;

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
        int count = baseEnemyCount + enemyCountGrowth * (CurrentWave - 1);
        StartCoroutine(SpawnWave(count));
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
        if (player == null || enemyPrefab == null) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
        Instantiate(enemyPrefab, (Vector2)player.position + offset, Quaternion.identity);
        AliveCount++;
    }

    /// <summary>EnemyHealth가 사망 시 호출</summary>
    public void NotifyEnemyDied()
    {
        AliveCount--;
        if (AliveCount <= 0 && !spawning) OnWaveCleared();
    }

    void OnWaveCleared()
    {
        resting = true;
        restTimer = timeBetweenWaves;

        // 스킬 3택 (#10) — 선택하는 동안 시간 정지, 휴식 타이머는 그 후 진행
        if (SkillSystem.Instance != null) SkillSystem.Instance.OfferChoices();

        // TODO(#7 머지 후): 다음 웨이브 규모·적 스탯에 MoonData 배율 적용
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
    }
}
