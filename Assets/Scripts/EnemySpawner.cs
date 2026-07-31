using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("생성할 적 프리팹")]
    public GameObject enemyPrefab;

    [Tooltip("게임 시작 시 생성 간격(초)")]
    public float startInterval = 1.5f;

    [Tooltip("최대로 빨라졌을 때의 생성 간격(초)")]
    public float minInterval = 0.25f;

    [Tooltip("몇 초에 걸쳐 최대 속도까지 빨라질지")]
    public float rampDuration = 120f;

    [Tooltip("한 번에 몇 마리씩 생성할지")]
    public int spawnCount = 1;

    [Tooltip("플레이어로부터 이 거리만큼 떨어진 곳(화면 밖)에서 생성")]
    public float spawnRadius = 12f;

    Transform player;
    float timer;
    float elapsed; // 게임 시작 후 흐른 시간

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null || enemyPrefab == null) return;

        elapsed += Time.deltaTime;
        timer += Time.deltaTime;

        // 시간이 지날수록 startInterval → minInterval로 점점 빨라짐
        float currentInterval = Mathf.Lerp(startInterval, minInterval, elapsed / rampDuration);

        if (timer >= currentInterval)
        {
            timer = 0f;
            for (int i = 0; i < spawnCount; i++) SpawnOne();
        }
    }

    void SpawnOne()
    {
        // 플레이어 주변 원 위의 임의 지점(화면 밖)에서 생성
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
        Vector2 pos = (Vector2)player.position + offset;

        Instantiate(enemyPrefab, pos, Quaternion.identity);
    }
}
